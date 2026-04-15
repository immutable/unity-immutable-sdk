using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class DiskStoreTests
    {
        private string _testDir;
        private DiskStore _store;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
            _store = new DiskStore(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [Test]
        public void Write_CreatesJsonFile_InQueueDirectory()
        {
            _store.Write("{\"event\":\"test\"}");

            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            var files = Directory.GetFiles(queueDir, "*.json");
            Assert.AreEqual(1, files.Length, "should have written exactly one event file");
        }

        [Test]
        public void Write_FileContents_MatchInputJson()
        {
            const string json = "{\"event\":\"pageview\",\"userId\":\"u1\"}";
            _store.Write(json);

            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            var file = Directory.GetFiles(queueDir, "*.json").Single();
            Assert.AreEqual(json, File.ReadAllText(file));
        }

        [Test]
        public void ReadBatch_ReturnsOldestFirst()
        {
            // Write events with a small delay to guarantee different ticks in filenames
            _store.Write("{\"seq\":1}");
            Thread.Sleep(10);
            _store.Write("{\"seq\":2}");
            Thread.Sleep(10);
            _store.Write("{\"seq\":3}");

            var batch = _store.ReadBatch(10);

            Assert.AreEqual(3, batch.Count);
            // Filenames are {ticks}_{uuid}.json — lexicographic sort == oldest first
            var names = batch.Select(Path.GetFileName).ToList();
            Assert.That(names, Is.Ordered.Ascending);
        }

        [Test]
        public void ReadBatch_RespectsMaxSize()
        {
            for (var i = 0; i < 5; i++)
            {
                _store.Write($"{{\"i\":{i}}}");
                Thread.Sleep(5);
            }

            var batch = _store.ReadBatch(3);
            Assert.AreEqual(3, batch.Count);
        }

        [Test]
        public void ReadBatch_ClampsToMaxBatchSize()
        {
            for (var i = 0; i < Constants.MaxBatchSize + 10; i++)
                _store.Write($"{{\"i\":{i}}}");

            var batch = _store.ReadBatch(Constants.MaxBatchSize + 10);
            Assert.LessOrEqual(batch.Count, Constants.MaxBatchSize);
        }

        [Test]
        public void ReadBatch_ExcludesAndDeletesStaleFiles()
        {
            _store.Write("{\"fresh\":true}");

            // Manually plant a stale file (ticks from 31 days ago)
            var staleTime = DateTime.UtcNow.AddDays(-(Constants.StaleEventDays + 1));
            var staleName = $"{staleTime.Ticks}_{Guid.NewGuid():N}.json";
            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            File.WriteAllText(Path.Combine(queueDir, staleName), "{\"stale\":true}");

            var batch = _store.ReadBatch(10);

            Assert.AreEqual(1, batch.Count, "stale file should be excluded from batch");
            Assert.IsFalse(File.Exists(Path.Combine(queueDir, staleName)), "stale file should be deleted");
        }

        [Test]
        public void Delete_RemovesSpecifiedFiles()
        {
            _store.Write("{\"a\":1}");
            _store.Write("{\"b\":2}");

            var batch = _store.ReadBatch(10);
            Assert.AreEqual(2, batch.Count);

            _store.Delete(batch);

            Assert.AreEqual(0, _store.Count());
        }

        [Test]
        public void Count_ReflectsNumberOfFilesOnDisk()
        {
            Assert.AreEqual(0, _store.Count());

            _store.Write("{\"x\":1}");
            _store.Write("{\"x\":2}");

            Assert.AreEqual(2, _store.Count());
        }

        [Test]
        public void ReadBatch_EmptyQueue_ReturnsEmpty()
        {
            var batch = _store.ReadBatch(10);
            Assert.AreEqual(0, batch.Count);
        }

        [Test]
        public void ReadBatch_ZeroMaxSize_ReturnsEmpty()
        {
            _store.Write("{\"x\":1}");
            var batch = _store.ReadBatch(0);
            Assert.AreEqual(0, batch.Count);
        }

        [Test]
        public void CrashRecovery_PicksUpFilesFromPreviousRun()
        {
            // Simulate a previous run by writing a file directly
            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            var survivingName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}.json";
            File.WriteAllText(Path.Combine(queueDir, survivingName), "{\"survived\":true}");

            // Create a new DiskStore instance pointing at the same path (simulates restart)
            var store2 = new DiskStore(_testDir);
            var batch = store2.ReadBatch(10);

            Assert.AreEqual(1, batch.Count, "crash-surviving file should be picked up on next init");
        }
    }
}
