using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ThreadSafetyStressTests
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
            ImmutableAudience.ResetState();
            ImmutableAudience.LaunchContextProvider = null;
            ImmutableAudience.ContextProvider = null;
            ImmutableAudience.DefaultPersistentDataPathProvider = null;
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // 503 keeps every event on disk so the queue-count assertion is exact.
        private class KeepOnDiskHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        private AudienceConfig MakeConfig(ConsentLevel consent = ConsentLevel.Full) =>
            new AudienceConfig
            {
                PublishableKey = "pk_imapik-test-stress",
                Consent = consent,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = TestDefaults.FlushIntervalSeconds,
                FlushSize = TestDefaults.FlushSize,
                HttpHandler = new KeepOnDiskHandler()
            };

        // -----------------------------------------------------------------
        // Track: sustained throughput across N threads
        // -----------------------------------------------------------------

        [Test]
        public void Track_4Threads_SustainedLoad_NoExceptions_QueueCountMatches() =>
            RunSustainedTrackLoad(threadCount: 4, durationSeconds: 5);

        [Test]
        public void Track_16Threads_SustainedLoad_NoExceptions_QueueCountMatches() =>
            RunSustainedTrackLoad(threadCount: 16, durationSeconds: 5);

        private void RunSustainedTrackLoad(int threadCount, int durationSeconds)
        {
            ImmutableAudience.Init(MakeConfig());

            // Drain Init's session_start + game_launch so the on-disk count
            // measures only what our threads enqueue.
            ImmutableAudience.FlushQueueToDiskForTesting();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);

            var threads = new Thread[threadCount];
            var firedPerThread = new int[threadCount];
            var exceptions = new ConcurrentBag<Exception>();
            var barrier = new Barrier(threadCount + 1);
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(durationSeconds);

            for (int t = 0; t < threadCount; t++)
            {
                int idx = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        int count = 0;
                        while (DateTime.UtcNow < deadline)
                        {
                            ImmutableAudience.Track("stress_track");
                            count++;
                        }
                        firedPerThread[idx] = count;
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
                threads[t].Start();
            }

            barrier.SignalAndWait();
            var sw = Stopwatch.StartNew();
            foreach (var th in threads) th.Join();
            sw.Stop();

            CollectionAssert.IsEmpty(exceptions,
                $"sustained Track load must not throw; observed {exceptions.Count} exception(s)");

            int totalFired = firedPerThread.Sum();
            double perThreadRate = totalFired / (double)threadCount / sw.Elapsed.TotalSeconds;

            TestContext.WriteLine(
                $"Track threads={threadCount} fired={totalFired} elapsed={sw.Elapsed.TotalSeconds:F2}s rate={perThreadRate:F0}/sec/thread");

            Assert.GreaterOrEqual(perThreadRate, 200,
                $"sustained Track throughput collapsed below floor; observed {perThreadRate:F0}/sec/thread");

            ImmutableAudience.FlushQueueToDiskForTesting();

            int onDisk = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length;
            Assert.AreEqual(totalFired, onDisk,
                $"every Track should land on disk; fired={totalFired} onDisk={onDisk} delta={totalFired - onDisk}");
        }

        // -----------------------------------------------------------------
        // Concurrent SetConsent + Identify + Track
        // -----------------------------------------------------------------

        [Test]
        public void TrackIdentifySetConsent_ConcurrentLoad_NoRaceExceptions()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            const int durationSeconds = 5;
            const int trackerCount = 8;
            const int identifierCount = 4;
            const int consentCount = 4;
            const int totalThreads = trackerCount + identifierCount + consentCount;

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(durationSeconds);
            var exceptions = new ConcurrentBag<Exception>();
            var barrier = new Barrier(totalThreads);
            var threads = new Thread[totalThreads];
            int t = 0;

            for (int i = 0; i < trackerCount; i++)
            {
                threads[t++] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        while (DateTime.UtcNow < deadline)
                            ImmutableAudience.Track("mixed_load_track");
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
            }

            for (int i = 0; i < identifierCount; i++)
            {
                int seed = i;
                threads[t++] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        int n = 0;
                        while (DateTime.UtcNow < deadline)
                            ImmutableAudience.Identify($"player_{seed}_{n++}", IdentityType.Custom);
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
            }

            for (int i = 0; i < consentCount; i++)
            {
                bool toFull = i % 2 == 0;
                threads[t++] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        while (DateTime.UtcNow < deadline)
                            ImmutableAudience.SetConsent(toFull ? ConsentLevel.Full : ConsentLevel.Anonymous);
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
            }

            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();

            CollectionAssert.IsEmpty(exceptions,
                $"concurrent Track / Identify / SetConsent must not throw; observed {exceptions.Count} exception(s)");
            Assert.IsTrue(ImmutableAudience.Initialized,
                "SDK should remain initialised after the mixed-workload run");

            var finalConsent = ImmutableAudience.CurrentConsent;
            Assert.That(finalConsent,
                Is.EqualTo(ConsentLevel.Full).Or.EqualTo(ConsentLevel.Anonymous),
                $"final consent must be one of the values we set, got {finalConsent}");
        }

        // -----------------------------------------------------------------
        // Allocation profile: bounded growth + no Gen 2 churn
        // -----------------------------------------------------------------

        [Test]
        public void Track_SteadyState_BoundedMainThreadAllocation()
        {
            ImmutableAudience.Init(MakeConfig());

            // Warm up so JIT and one-time allocations are out of the measured window.
            for (int i = 0; i < 200; i++) ImmutableAudience.Track("warmup");
            ImmutableAudience.FlushQueueToDiskForTesting();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocBefore = GC.GetAllocatedBytesForCurrentThread();

            const int iterations = 10_000;
            for (int i = 0; i < iterations; i++)
                ImmutableAudience.Track("steady_state");

            long allocDelta = GC.GetAllocatedBytesForCurrentThread() - allocBefore;
            double bytesPerCall = (double)allocDelta / iterations;

            TestContext.WriteLine(
                $"Track x{iterations}: main-thread alloc={allocDelta:N0}B ({bytesPerCall:F0}B/call)");

            // Empirical baseline ~860 B/call (one MessageBuilder dict, sized
            // for ~10 message-envelope entries). 2.5 KB/call would indicate
            // a regression like retaining state across calls or boxing in
            // the hot path; normal evolution that adds an envelope entry
            // (~80 B) doesn't trip it.
            Assert.Less(bytesPerCall, 2500,
                $"main-thread allocation per Track call ({bytesPerCall:F0}B) exceeded budget");
        }
    }
}
