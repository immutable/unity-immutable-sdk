using System.IO;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class IdentityTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // -----------------------------------------------------------------
        // GetOrCreate: anonymous ID
        // -----------------------------------------------------------------

        [Test]
        public void NewDirectory_GeneratesNonEmptyId_AndWritesFile()
        {
            var id = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(id);
            Assert.IsNotEmpty(id);

            var filePath = AudiencePaths.IdentityFile(_testDir);
            Assert.IsTrue(File.Exists(filePath), "identity file should exist on disk");
        }

        [Test]
        public void ExistingFile_NewFormat_ReturnsPreviousAnonId()
        {
            var expectedAnonId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            var expectedDeviceId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.IdentityFile(_testDir),
                $"{{\"anonymousId\":\"{expectedAnonId}\",\"deviceId\":\"{expectedDeviceId}\"}}");

            var result = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(expectedAnonId, result);
        }

        [Test]
        public void ExistingFile_LegacyPlainString_MigratesAnonId_AndGeneratesDeviceId()
        {
            var legacyAnonId = "pre-existing-id-from-last-launch";
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.IdentityFile(_testDir), legacyAnonId);

            var result = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Assert.AreEqual(legacyAnonId, result, "legacy anon_id should be preserved after migration");

            // File must be rewritten as JSON; verify by re-reading via a cold cache.
            Identity.ClearCache();
            var anonIdAfterReload = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var deviceIdAfterReload = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(legacyAnonId, anonIdAfterReload, "migrated anon_id must survive a cache-clear + reload");
            Assert.IsNotNull(deviceIdAfterReload, "device_id should be generated during migration and persist");
            Assert.IsNotEmpty(deviceIdAfterReload);

            // File must now be valid JSON, not a plain string.
            var raw = File.ReadAllText(AudiencePaths.IdentityFile(_testDir)).Trim();
            Assert.IsTrue(raw.StartsWith("{"), "identity file must be rewritten to JSON format after migration");
        }

        [Test]
        public void ExistingFile_LegacyPlainString_MigrationWriteFails_PreservesReadAnonId()
        {
            // The anon_id here was already legitimately persisted and successfully
            // read; only the migration write (adding device_id) fails. That must not
            // discard the real anon_id for a fresh random one.
            var legacyAnonId = "already-persisted-legacy-id";
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            var identityFile = AudiencePaths.IdentityFile(_testDir);
            File.WriteAllText(identityFile, legacyAnonId);

            // Force WriteFile's tmp-then-replace to fail by blocking its .tmp path
            // with a directory: File.WriteAllText(tmpPath, ...) throws
            // UnauthorizedAccessException when tmpPath is an existing directory.
            Directory.CreateDirectory(identityFile + ".tmp");

            var result = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(legacyAnonId, result,
                "a failed migration write must not discard an anon_id that was already legitimately persisted");
        }

        [Test]
        public void SecondCall_ReturnsSameAnonId()
        {
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void ConsentNone_ReturnsNull_AndNoFileWritten()
        {
            var id = Identity.GetOrCreate(_testDir, ConsentLevel.None);

            Assert.IsNull(id);

            var filePath = AudiencePaths.IdentityFile(_testDir);
            Assert.IsFalse(File.Exists(filePath), "identity file must not be written when consent is None");
        }

        [Test]
        public void PersistenceFails_FallsBackToInMemoryId()
        {
            // Force Directory.CreateDirectory(AudienceDir) to throw IOException by
            // pre-creating a *file* at the exact path the identity directory needs.
            File.WriteAllText(AudiencePaths.AudienceDir(_testDir), "blocking file");

            var id = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(id, "anonymousId must still be populated even when persistence fails");
            Assert.IsNotEmpty(id);
        }

        [Test]
        public void PersistenceFails_InMemoryIdStableForRestOfSession()
        {
            File.WriteAllText(AudiencePaths.AudienceDir(_testDir), "blocking file");

            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(id1, id2, "the in-memory fallback id must not change between calls in the same session");
        }

        [Test]
        public void ReadFails_FallsBackToInMemoryIds()
        {
            // Distinct from the persistence-failure tests above: here the *read*
            // itself throws (exercising LoadOrGenerate's first catch block), not
            // just the later write, so there's nothing pre-existing to reuse at all.
            var identityFile = AudiencePaths.IdentityFile(_testDir);
            Directory.CreateDirectory(AudiencePaths.AudienceDir(_testDir));
            File.WriteAllText(identityFile, "{\"anonymousId\":\"existing-anon\",\"deviceId\":\"existing-device\"}");
            using var exclusiveLock = new FileStream(identityFile, FileMode.Open, FileAccess.Read, FileShare.None);

            var anonId = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var deviceId = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(anonId, "anonymousId must still be populated even when the read itself fails");
            Assert.IsNotEmpty(anonId);
            Assert.IsNotNull(deviceId);
            Assert.IsNotEmpty(deviceId);
        }

        // -----------------------------------------------------------------
        // GetOrCreateDeviceId
        // -----------------------------------------------------------------

        [Test]
        public void GetOrCreateDeviceId_Anonymous_ReturnsNonEmptyId()
        {
            var deviceId = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(deviceId);
            Assert.IsNotEmpty(deviceId);
        }

        [Test]
        public void GetOrCreateDeviceId_None_ReturnsNull_AndNoFileWritten()
        {
            var deviceId = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.None);

            Assert.IsNull(deviceId);
            Assert.IsFalse(File.Exists(AudiencePaths.IdentityFile(_testDir)));
        }

        [Test]
        public void GetOrCreateDeviceId_SameAcrossMultipleCalls()
        {
            var id1 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);
            var id2 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void GetOrCreateDeviceId_PersistedAcrossSimulatedRestart()
        {
            var deviceId1 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);
            Identity.ClearCache();
            var deviceId2 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(deviceId1, deviceId2, "device_id must survive a simulated restart (cache clear)");
        }

        [Test]
        public void GetOrCreateDeviceId_DifferentFromAnonId()
        {
            var anonId = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            var deviceId = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreNotEqual(anonId, deviceId, "device_id and anon_id must be distinct UUIDs");
        }

        // -----------------------------------------------------------------
        // RotateAnonymousId (logout)
        // -----------------------------------------------------------------

        [Test]
        public void RotateAnonymousId_ChangesAnonId()
        {
            var anonId1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Identity.RotateAnonymousId(_testDir);
            var anonId2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(anonId2);
            Assert.AreNotEqual(anonId1, anonId2, "anon_id must rotate on logout");
        }

        [Test]
        public void RotateAnonymousId_PreservesDeviceId()
        {
            var deviceId1 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);
            Identity.RotateAnonymousId(_testDir);
            var deviceId2 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(deviceId1, deviceId2, "device_id must survive logout (RotateAnonymousId)");
        }

        [Test]
        public void RotateAnonymousId_DeviceIdPersistedAcrossRestart()
        {
            var deviceId1 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);
            Identity.RotateAnonymousId(_testDir);
            Identity.ClearCache();
            var deviceId2 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreEqual(deviceId1, deviceId2, "device_id must persist through logout + simulated restart");
        }

        // -----------------------------------------------------------------
        // Reset (opt-out, full wipe)
        // -----------------------------------------------------------------

        [Test]
        public void Reset_WipesAnonId()
        {
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Identity.Reset(_testDir);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.IsNotNull(id2);
            Assert.AreNotEqual(id1, id2, "Reset must wipe anon_id");
        }

        [Test]
        public void Reset_WipesDeviceId()
        {
            var deviceId1 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);
            Identity.Reset(_testDir);
            var deviceId2 = Identity.GetOrCreateDeviceId(_testDir, ConsentLevel.Anonymous);

            Assert.AreNotEqual(deviceId1, deviceId2, "Reset (opt-out) must wipe device_id");
        }

        [Test]
        public void Reset_DeletesFile()
        {
            Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Assert.IsTrue(File.Exists(AudiencePaths.IdentityFile(_testDir)));

            Identity.Reset(_testDir);
            Assert.IsFalse(File.Exists(AudiencePaths.IdentityFile(_testDir)),
                "Reset must delete the identity file");
        }

        // -----------------------------------------------------------------
        // Get (non-creating read)
        // -----------------------------------------------------------------

        [Test]
        public void Get_NoExistingFile_ReturnsNull_AndDoesNotCreate()
        {
            var id = Identity.Get(_testDir);

            Assert.IsNull(id);
            var filePath = AudiencePaths.IdentityFile(_testDir);
            Assert.IsFalse(File.Exists(filePath), "Get must not create the identity file");
        }

        [Test]
        public void Get_ExistingNewFormatFile_ReturnsAnonId()
        {
            var expectedAnonId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.IdentityFile(_testDir),
                $"{{\"anonymousId\":\"{expectedAnonId}\",\"deviceId\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"}}");

            Assert.AreEqual(expectedAnonId, Identity.Get(_testDir));
        }

        [Test]
        public void Get_ExistingLegacyFile_ReturnsAnonId()
        {
            var expectedId = "pre-existing-id";
            var dir = AudiencePaths.AudienceDir(_testDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(AudiencePaths.IdentityFile(_testDir), expectedId);

            Assert.AreEqual(expectedId, Identity.Get(_testDir));
        }
    }
}
