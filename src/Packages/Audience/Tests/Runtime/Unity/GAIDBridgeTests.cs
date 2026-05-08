#nullable enable

using System;
using System.IO;
using NUnit.Framework;
using Immutable.Audience.Unity.Mobile;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class GAIDBridgeTests
    {
        private string _testDir = null!;
        private Func<string, GAIDInfo?> _originalReadCachedImpl = null!;
        private Action<string> _originalStartFetchImpl = null!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);

            _originalReadCachedImpl = GAIDBridge.ReadCachedImpl;
            _originalStartFetchImpl = GAIDBridge.StartFetchImpl;
            GAIDBridge.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            GAIDBridge.ReadCachedImpl = _originalReadCachedImpl;
            GAIDBridge.StartFetchImpl = _originalStartFetchImpl;
            GAIDBridge.ResetForTesting();

            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // -----------------------------------------------------------------
        // GetCached
        // -----------------------------------------------------------------

        [Test]
        public void GetCached_NoFile_ReturnsNull()
        {
            Assert.IsNull(GAIDBridge.GetCached(_testDir));
        }

        [Test]
        public void GetCached_NonEmptyFile_ReturnsGaidAndFlag()
        {
            const string gaid = "abcdef01-2345-6789-abcd-ef0123456789";
            GAIDBridge.WriteCacheEntry(_testDir, gaid, limitAdTracking: false);

            var info = GAIDBridge.GetCached(_testDir);

            Assert.IsTrue(info.HasValue);
            Assert.AreEqual(gaid, info!.Value.Gaid);
            Assert.IsFalse(info.Value.LimitAdTracking);
        }

        [Test]
        public void GetCached_LimitAdTrackingTrue_PreservedAcrossWriteRead()
        {
            const string gaid = "00000000-0000-0000-0000-000000000000";
            GAIDBridge.WriteCacheEntry(_testDir, gaid, limitAdTracking: true);

            var info = GAIDBridge.GetCached(_testDir);

            Assert.IsTrue(info.HasValue);
            Assert.IsTrue(info!.Value.LimitAdTracking);
        }

        [Test]
        public void GetCached_OptOutEntry_ReturnsEmptyGaidWithFlag()
        {
            // User opted out → empty gaid string + limitAdTracking=true. The
            // pipeline reads "fetched, opted out" rather than "not fetched".
            GAIDBridge.WriteCacheEntry(_testDir, string.Empty, limitAdTracking: true);

            var info = GAIDBridge.GetCached(_testDir);

            Assert.IsTrue(info.HasValue);
            Assert.AreEqual(string.Empty, info!.Value.Gaid);
            Assert.IsTrue(info.Value.LimitAdTracking);
        }

        [Test]
        public void GetCached_NullPath_ReturnsNull()
        {
            Assert.IsNull(GAIDBridge.GetCached(null!));
            Assert.IsNull(GAIDBridge.GetCached(string.Empty));
        }

        // -----------------------------------------------------------------
        // EnsureFetchStarted
        // -----------------------------------------------------------------

        [Test]
        public void EnsureFetchStarted_FirstCall_InvokesFetch()
        {
            var fetchCalls = 0;
            GAIDBridge.StartFetchImpl = _ => fetchCalls++;

            GAIDBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, fetchCalls);
        }

        [Test]
        public void EnsureFetchStarted_CalledTwiceInSameProcess_FetchesOnce()
        {
            var fetchCalls = 0;
            GAIDBridge.StartFetchImpl = _ => fetchCalls++;

            GAIDBridge.EnsureFetchStarted(_testDir);
            GAIDBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, fetchCalls,
                "Per-process gate must prevent duplicate JNI workers in one session");
        }

        [Test]
        public void EnsureFetchStarted_TerminalCacheExists_StillFetches()
        {
            // Unlike the install referrer (terminal once written), GAID can
            // change (user reset) — we always refresh the cache for the next
            // launch even when a value already exists.
            GAIDBridge.WriteCacheEntry(_testDir, "stale-gaid", limitAdTracking: false);

            var fetchCalls = 0;
            GAIDBridge.StartFetchImpl = _ => fetchCalls++;

            GAIDBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, fetchCalls);
        }

        [Test]
        public void EnsureFetchStarted_StartFetchThrows_RearmsGate()
        {
            // A synchronous failure (e.g. JNI attach) must re-arm the gate
            // so a later call this session can retry.
            GAIDBridge.StartFetchImpl = _ => throw new InvalidOperationException("boom");

            Assert.Throws<InvalidOperationException>(() => GAIDBridge.EnsureFetchStarted(_testDir));

            var retryCalls = 0;
            GAIDBridge.StartFetchImpl = _ => retryCalls++;
            GAIDBridge.EnsureFetchStarted(_testDir);

            Assert.AreEqual(1, retryCalls);
        }

        [Test]
        public void EnsureFetchStarted_NullPath_NoOp()
        {
            var fetchCalls = 0;
            GAIDBridge.StartFetchImpl = _ => fetchCalls++;

            GAIDBridge.EnsureFetchStarted(null!);
            GAIDBridge.EnsureFetchStarted(string.Empty);

            Assert.AreEqual(0, fetchCalls);
        }

        // -----------------------------------------------------------------
        // WriteCacheEntry
        // -----------------------------------------------------------------

        [Test]
        public void WriteCacheEntry_CreatesAudienceDirIfMissing()
        {
            // First-launch attribution write must create the imtbl_audience/
            // directory; consent None never touches disk so the dir may not
            // exist yet.
            GAIDBridge.WriteCacheEntry(_testDir, "abc", limitAdTracking: false);

            Assert.IsNotNull(GAIDBridge.GetCached(_testDir));
        }

        [Test]
        public void WriteCacheEntry_OverwritesExistingFile()
        {
            GAIDBridge.WriteCacheEntry(_testDir, "old-gaid", limitAdTracking: true);
            GAIDBridge.WriteCacheEntry(_testDir, "new-gaid", limitAdTracking: false);

            var info = GAIDBridge.GetCached(_testDir);

            Assert.IsTrue(info.HasValue);
            Assert.AreEqual("new-gaid", info!.Value.Gaid);
            Assert.IsFalse(info.Value.LimitAdTracking);
        }
    }
}
