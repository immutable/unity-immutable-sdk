#nullable enable

using System;
using System.IO;
using NUnit.Framework;
using Immutable.Audience.Unity.Mobile;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class InstallReferrerBridgeTests
    {
        private string _testDir = null!;
        private Func<string, string?> _originalReadCachedImpl = null!;
        private Action<string> _originalStartFetchImpl = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);

            _originalReadCachedImpl = InstallReferrerBridge.ReadCachedImpl;
            _originalStartFetchImpl = InstallReferrerBridge.StartFetchImpl;
            InstallReferrerBridge.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            InstallReferrerBridge.ReadCachedImpl = _originalReadCachedImpl;
            InstallReferrerBridge.StartFetchImpl = _originalStartFetchImpl;
            InstallReferrerBridge.ResetForTesting();

            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // -----------------------------------------------------------------
        // GetCachedInstallReferrer
        // -----------------------------------------------------------------

        [Test]
        public void GetCachedInstallReferrer_NoFile_ReturnsNull()
        {
            Assert.IsNull(InstallReferrerBridge.GetCachedInstallReferrer(_testDir));
        }

        [Test]
        public void GetCachedInstallReferrer_NonEmptyFile_ReturnsContent()
        {
            const string referrer = "utm_source=google-play&utm_medium=organic";
            InstallReferrerBridge.WriteCacheEntry(_testDir, referrer);

            Assert.AreEqual(referrer, InstallReferrerBridge.GetCachedInstallReferrer(_testDir));
        }

        [Test]
        public void GetCachedInstallReferrer_EmptyFile_ReturnsNull()
        {
            // Empty file marks "fetched, no referrer". Caller treats it as
            // "nothing to emit" but EnsureFetchStarted treats it as terminal.
            InstallReferrerBridge.WriteCacheEntry(_testDir, string.Empty);

            Assert.IsNull(InstallReferrerBridge.GetCachedInstallReferrer(_testDir));
        }

        [Test]
        public void GetCachedInstallReferrer_NullPath_ReturnsNull()
        {
            Assert.IsNull(InstallReferrerBridge.GetCachedInstallReferrer(null!));
            Assert.IsNull(InstallReferrerBridge.GetCachedInstallReferrer(string.Empty));
        }

        // -----------------------------------------------------------------
        // EnsureFetchStarted
        // -----------------------------------------------------------------

        [Test]
        public void EnsureFetchStarted_NoCacheFile_InvokesFetch()
        {
            var fetchCalls = 0;
            InstallReferrerBridge.StartFetchImpl = _ => fetchCalls++;

            InstallReferrerBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, fetchCalls);
        }

        [Test]
        public void EnsureFetchStarted_CalledTwiceInSameProcess_FetchesOnce()
        {
            var fetchCalls = 0;
            InstallReferrerBridge.StartFetchImpl = _ => fetchCalls++;

            InstallReferrerBridge.EnsureFetchStarted(_testDir);
            InstallReferrerBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, fetchCalls,
                "Per-process gate must prevent double-fetch in one session");
        }

        [Test]
        public void EnsureFetchStarted_TerminalCacheExists_SkipsFetch()
        {
            // Simulate a previous launch that wrote a cache entry. The fetch
            // must NOT run again. Google's referrer is stable per install.
            InstallReferrerBridge.WriteCacheEntry(_testDir, "utm_source=test");

            var fetchCalls = 0;
            InstallReferrerBridge.StartFetchImpl = _ => fetchCalls++;

            InstallReferrerBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(0, fetchCalls);
        }

        [Test]
        public void EnsureFetchStarted_EmptyCacheExists_SkipsFetch()
        {
            // Empty cache = "fetched, no referrer": terminal state, no retry.
            InstallReferrerBridge.WriteCacheEntry(_testDir, string.Empty);

            var fetchCalls = 0;
            InstallReferrerBridge.StartFetchImpl = _ => fetchCalls++;

            InstallReferrerBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(0, fetchCalls);
        }

        [Test]
        public void EnsureFetchStarted_NullPath_NoOp()
        {
            var fetchCalls = 0;
            InstallReferrerBridge.StartFetchImpl = _ => fetchCalls++;

            InstallReferrerBridge.EnsureFetchStarted(null!);
            InstallReferrerBridge.EnsureFetchStarted(string.Empty);

            Assert.AreEqual(0, fetchCalls);
        }

        // -----------------------------------------------------------------
        // WriteCacheEntry
        // -----------------------------------------------------------------

        [Test]
        public void WriteCacheEntry_CreatesAudienceDirIfMissing()
        {
            // Disk persistence routes through AudiencePaths so the file lives
            // under imtbl_audience/. WriteCacheEntry must create the dir on
            // first attribution write since previous launches may not have
            // touched it (consent None never creates the audience dir).
            InstallReferrerBridge.WriteCacheEntry(_testDir, "ref");

            Assert.IsNotNull(InstallReferrerBridge.GetCachedInstallReferrer(_testDir));
        }

        [Test]
        public void WriteCacheEntry_OverwritesExistingFile()
        {
            InstallReferrerBridge.WriteCacheEntry(_testDir, "old=value");
            InstallReferrerBridge.WriteCacheEntry(_testDir, "new=value");

            Assert.AreEqual("new=value", InstallReferrerBridge.GetCachedInstallReferrer(_testDir));
        }
    }
}
