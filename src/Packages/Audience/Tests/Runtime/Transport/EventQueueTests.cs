using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class EventQueueTests
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

        private static Dictionary<string, object> Msg(string evt) =>
            new Dictionary<string, object> { ["event"] = evt };

        [Test]
        public void Enqueue_ThenFlushSync_PersistesEventToDisk()
        {
            using var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);

            queue.Enqueue(Msg("track"));
            queue.FlushSync();

            Assert.AreEqual(1, _store.Count(), "event should be on disk after FlushSync");
        }

        [Test]
        public void Enqueue_MultipleEvents_AllPersistedAfterFlush()
        {
            using var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);

            for (var i = 0; i < 10; i++)
                queue.Enqueue(new Dictionary<string, object> { ["i"] = i });

            queue.FlushSync();

            Assert.AreEqual(10, _store.Count());
        }

        [Test]
        public void FlushSize_Trigger_DrainsToDiskAutomatically()
        {
            const int flushSize = 5;
            using var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: flushSize);

            for (var i = 0; i < flushSize; i++)
                queue.Enqueue(new Dictionary<string, object> { ["i"] = i });

            // Give the background thread time to drain
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (_store.Count() < flushSize && DateTime.UtcNow < deadline)
                Thread.Sleep(20);

            Assert.AreEqual(flushSize, _store.Count(),
                "reaching FlushSize should trigger automatic drain without explicit FlushSync");
        }

        [Test]
        public void Shutdown_FlushesRemainingEvents()
        {
            var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);

            queue.Enqueue(Msg("a"));
            queue.Enqueue(Msg("b"));

            queue.Shutdown();

            Assert.AreEqual(2, _store.Count(), "Shutdown should flush all remaining in-memory events");
        }

        [Test]
        public void Shutdown_CalledTwice_DoesNotThrow()
        {
            var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);
            queue.Shutdown();
            Assert.DoesNotThrow(() => queue.Shutdown());
        }

        [Test]
        public void Enqueue_AfterShutdown_IsIgnored()
        {
            var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);
            queue.Shutdown();

            queue.Enqueue(Msg("ignored"));

            Assert.AreEqual(0, _store.Count(), "events enqueued after Shutdown should be discarded");
        }

        [Test]
        public void IntervalFlush_DrainsToDiskWithoutExplicitCall()
        {
            // Very short interval to make the test fast
            using var queue = new EventQueue(_store, flushIntervalSeconds: 1, flushSize: 100);

            queue.Enqueue(Msg("interval_flush"));

            // Wait slightly longer than the flush interval
            var deadline = DateTime.UtcNow.AddSeconds(4);
            while (_store.Count() < 1 && DateTime.UtcNow < deadline)
                Thread.Sleep(50);

            Assert.AreEqual(1, _store.Count(), "interval flush should have persisted event to disk");
        }

        [Test]
        public void Dispose_FlushesAndStopsDrainThread()
        {
            using (var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100))
            {
                queue.Enqueue(Msg("dispose_test"));
            } // Dispose called here

            Assert.AreEqual(1, _store.Count(), "Dispose should flush events to disk");
        }

        [Test]
        public void SerialisationHappensOnDrainThread_CallerMutationDoesNotCorrupt()
        {
            using var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);

            // Caller passes a dict, then -- simulating a misuse -- mutates it
            // after enqueue but before FlushSync. Because the queue stores the
            // reference and serialises on drain, the written payload reflects the
            // mutated state. This test pins down that behaviour so callers know
            // they must snapshot (which ImmutableAudience.Track does).
            var shared = new Dictionary<string, object> { ["v"] = 1 };
            queue.Enqueue(shared);
            shared["v"] = 99;

            queue.FlushSync();

            var written = File.ReadAllText(_store.ReadBatch(10)[0]);
            StringAssert.Contains("\"v\":99", written);
        }

        [Test]
        public void PurgeAll_ClearsMemoryAndDisk()
        {
            using var queue = new EventQueue(_store, flushIntervalSeconds: 60, flushSize: 100);

            queue.Enqueue(new Dictionary<string, object> { [MessageFields.Type] = "track", [MessageFields.EventName] = "a" });
            queue.FlushSync();
            queue.Enqueue(new Dictionary<string, object> { [MessageFields.Type] = "track", [MessageFields.EventName] = "b" });

            queue.PurgeAll();

            Assert.AreEqual(0, _store.Count(),
                "PurgeAll should clear both in-memory queue and disk");
        }
    }
}
