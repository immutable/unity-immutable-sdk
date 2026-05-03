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

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var files = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob);
            Assert.AreEqual(1, files.Length, "should have written exactly one event file");
        }

        [Test]
        public void Write_FileContents_MatchInputJson()
        {
            const string json = "{\"event\":\"pageview\",\"userId\":\"u1\"}";
            _store.Write(json);

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var file = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Single();
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
            // Filenames are {ticks}_{uuid}.json: lexicographic sort == oldest first
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
            var staleName = $"{staleTime.Ticks}_{Guid.NewGuid():N}{AudiencePaths.QueueFileExtension}";
            var queueDir = AudiencePaths.QueueDir(_testDir);
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
            var queueDir = AudiencePaths.QueueDir(_testDir);
            var survivingName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{AudiencePaths.QueueFileExtension}";
            File.WriteAllText(Path.Combine(queueDir, survivingName), "{\"survived\":true}");

            // Create a new DiskStore instance pointing at the same path (simulates restart)
            var store2 = new DiskStore(_testDir);
            var batch = store2.ReadBatch(10);

            Assert.AreEqual(1, batch.Count, "crash-surviving file should be picked up on next init");
        }

        [Test]
        public void ApplyAnonymousDowngrade_DeletesIdentifyAndAlias_StripsUserIdFromTrack()
        {
            _store.Write(WireFixture.Identify(
                (MessageFields.AnonymousId, "a"),
                (MessageFields.UserId, "u")));
            _store.Write(WireFixture.Alias(
                (MessageFields.FromId, "a"),
                (MessageFields.ToId, "u")));
            _store.Write(WireFixture.Track(
                (MessageFields.EventName, "x"),
                (MessageFields.AnonymousId, "a"),
                (MessageFields.UserId, "u")));
            _store.Write(WireFixture.Track(
                (MessageFields.EventName, "y"),
                (MessageFields.AnonymousId, "a")));

            _store.ApplyAnonymousDowngrade();

            var remaining = _store.ReadBatch(100);
            Assert.AreEqual(2, remaining.Count, "identify and alias files should be deleted");

            foreach (var path in remaining)
            {
                var json = File.ReadAllText(path);
                var msg = JsonReader.DeserializeObject(json);
                Assert.AreEqual(MessageTypes.Track, msg[MessageFields.Type]);
                Assert.IsFalse(msg.ContainsKey(MessageFields.UserId), "userId must be stripped from queued track messages");
            }
        }

        [Test]
        public void ApplyAnonymousDowngrade_PurchaseValue_RoundsTripsExactlyForRealisticPrices()
        {
            // Pinning test: the JsonReader -> Json.Serialize rewrite path
            // turns decimals into doubles. Assert that the realistic range of
            // Purchase.Value amounts (typical two-decimal prices, free item,
            // AAA-tier bundle) survives the rewrite without drift.
            string[] realisticAmounts = { "0.99", "4.99", "9.99", "19.99", "49.99", "99.99", "149.99" };

            foreach (var amount in realisticAmounts)
            {
                // Fresh store per iteration to keep assertions clean.
                TearDown();
                SetUp();

                var json = "{\"type\":\"track\",\"eventName\":\"purchase\",\"anonymousId\":\"a\",\"userId\":\"u\","
                    + "\"properties\":{\"currency\":\"USD\",\"value\":" + amount + "}}";
                _store.Write(json);

                _store.ApplyAnonymousDowngrade();

                var rewritten = _store.ReadBatch(10);
                Assert.AreEqual(1, rewritten.Count);
                var rewrittenJson = File.ReadAllText(rewritten[0]);
                StringAssert.Contains("\"value\":" + amount, rewrittenJson,
                    $"Purchase.Value {amount} must round-trip exactly through the downgrade rewrite");
            }
        }

        [Test]
        public void ApplyAnonymousDowngrade_DeletesMalformedFiles()
        {
            // Seed the queue directory with a file that is not valid JSON so the
            // downgrade cannot leave it to potentially leak identified data.
            var queueDir = AudiencePaths.QueueDir(_testDir);
            var badName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{AudiencePaths.QueueFileExtension}";
            File.WriteAllText(Path.Combine(queueDir, badName), "{not valid json");

            _store.ApplyAnonymousDowngrade();

            Assert.AreEqual(0, _store.ReadBatch(10).Count, "malformed file must not survive downgrade");
        }
    }
}
