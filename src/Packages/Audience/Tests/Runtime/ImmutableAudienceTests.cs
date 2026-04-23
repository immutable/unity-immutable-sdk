using System;
using System.Collections.Generic;
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
    internal class ImmutableAudienceTests
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
            ImmutableAudience.DefaultPersistentDataPathProvider = null;
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        private AudienceConfig MakeConfig(ConsentLevel consent = ConsentLevel.Anonymous)
        {
            return new AudienceConfig
            {
                PublishableKey = "pk_imapik-test-key1",
                Consent = consent,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = 600, // large — we flush manually in tests
                FlushSize = 1000,
                HttpHandler = new KeepOnDiskHandler()
            };
        }

        /// <summary>
        /// Returns 503 so the transport keeps files on disk for inspection.
        /// Tests verify queuing behavior, not sending behavior.
        /// </summary>
        private class KeepOnDiskHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
        }

        // -----------------------------------------------------------------
        // Init
        // -----------------------------------------------------------------

        [Test]
        public void Init_NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableAudience.Init(null));
        }

        [Test]
        public void Init_MissingPublishableKey_Throws()
        {
            var config = MakeConfig();
            config.PublishableKey = null;
            Assert.Throws<ArgumentException>(() => ImmutableAudience.Init(config));
        }

        [Test]
        public void Init_MissingPersistentDataPath_Throws()
        {
            var config = MakeConfig();
            config.PersistentDataPath = null;
            Assert.Throws<ArgumentException>(() => ImmutableAudience.Init(config));
        }

        [Test]
        public void Init_CalledTwice_IgnoresSecondCall()
        {
            ImmutableAudience.Init(MakeConfig());
            Assert.DoesNotThrow(() => ImmutableAudience.Init(MakeConfig()));
        }

        [Test]
        public void Track_NullEvent_DoesNotThrow_AndLogsWarning()
        {
            ImmutableAudience.Init(MakeConfig());

            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                Assert.DoesNotThrow(() => ImmutableAudience.Track((IEvent)null));
                Assert.That(lines, Has.Some.Contains("null event"));
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Track_IEventMissingRequiredField_DropsWithWarn()
        {
            ImmutableAudience.Init(MakeConfig());

            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                // Purchase with no Value set — ToProperties throws; Track must
                // catch, warn, and drop rather than ship an incomplete event.
                Assert.DoesNotThrow(() => ImmutableAudience.Track(new Purchase { Currency = "USD" }));
                Assert.That(lines, Has.Some.Contains("Purchase"));
                Assert.That(lines, Has.Some.Contains("Dropping"));
            }
            finally { Log.Writer = null; }

            ImmutableAudience.Shutdown();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains("\"purchase\"")),
                "purchase event with missing required Value must be dropped, not enqueued");
        }

        [Test]
        public void Track_NullOrEmptyEventName_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig());

            Assert.DoesNotThrow(() => ImmutableAudience.Track((string)null));
            Assert.DoesNotThrow(() => ImmutableAudience.Track(""));

            ImmutableAudience.Shutdown();

            // Assert the invariant directly: no enqueued message carries a
            // null or empty eventName. Earlier versions counted files
            // before/after the Track calls, which raced with the async
            // disk drain — Init enqueues session_start + game_launch, and
            // Shutdown adds session_end, so the file count after Shutdown
            // is deterministic but the before-count is not. Counting is
            // the wrong axis: what the test actually wants to pin is
            // "no empty-name event ever hit the queue", regardless of
            // what else was enqueued alongside it.
            //
            // Deserialize each message and inspect the eventName field
            // directly. A raw substring scan would false-positive on an
            // event whose property value happened to be the literal
            // string "eventName":"" (unlikely but possible) and would
            // silently break on any future JSON encoding change (whitespace,
            // key ordering, escape style).
            var queueDir = AudiencePaths.QueueDir(_testDir);
            if (!Directory.Exists(queueDir)) return;
            foreach (var file in Directory.GetFiles(queueDir, "*.json"))
            {
                var msg = JsonReader.DeserializeObject(File.ReadAllText(file));
                if ((string)msg["type"] != "track") continue;

                if (!msg.TryGetValue("eventName", out var eventNameObj))
                    Assert.Fail($"track message {Path.GetFileName(file)} missing eventName field");

                Assert.IsNotNull(eventNameObj,
                    $"queue file {Path.GetFileName(file)} has a null eventName");
                Assert.IsNotEmpty((string)eventNameObj,
                    $"queue file {Path.GetFileName(file)} has an empty eventName");
            }
        }

        [Test]
        public void Identify_NullUserId_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            Assert.DoesNotThrow(() => ImmutableAudience.Identify(null, IdentityType.Passport));
            Assert.DoesNotThrow(() => ImmutableAudience.Identify("", IdentityType.Passport));
        }

        [Test]
        public void Identify_InvalidIdentityTypeCast_Throws()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            var invalid = (IdentityType)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ImmutableAudience.Identify("user1", invalid),
                "invalid enum cast must throw so a broken call fails loud rather than " +
                "shipping an identify event that cannot be matched for deletion");
        }

        [Test]
        public void Alias_InvalidIdentityTypeCast_Throws()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            var invalid = (IdentityType)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ImmutableAudience.Alias("fromId", invalid, "toId", IdentityType.Steam),
                "invalid enum cast must throw so a broken alias call fails loud rather " +
                "than shipping an event that cannot be matched for deletion");
        }

        [Test]
        public void Alias_NullIds_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            Assert.DoesNotThrow(() => ImmutableAudience.Alias(null, IdentityType.Passport, "to", IdentityType.Steam));
            Assert.DoesNotThrow(() => ImmutableAudience.Alias("from", IdentityType.Passport, "", IdentityType.Steam));
        }

        [Test]
        public void Init_CalledTwice_LogsWarning()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig());
                ImmutableAudience.Init(MakeConfig());

                Assert.That(lines, Has.Some.Contains("Init called more than once"),
                    "second Init must surface a warning so a developer notices the silent no-op");
            }
            finally
            {
                Log.Writer = null;
            }
        }

        [Test]
        public void Init_ConcurrentCalls_OnlyOneSucceeds_OthersWarn()
        {
            // Spin up N threads that all race to call Init. With the lock in place,
            // exactly one initialises; the rest hit the duplicate-call warning branch.
            // Without the lock, all of them would pass the _initialized check and
            // double-allocate Timer + HttpClient + EventQueue, leaking the first set.
            const int threadCount = 16;
            var lines = new System.Collections.Concurrent.ConcurrentBag<string>();
            Log.Writer = msg => lines.Add(msg);

            try
            {
                var barrier = new System.Threading.Barrier(threadCount);
                var threads = new Thread[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.Init(MakeConfig());
                    });
                    threads[i].Start();
                }

                foreach (var t in threads) t.Join(TimeSpan.FromSeconds(5));

                var warningCount = lines.Count(l => l.Contains("Init called more than once"));
                Assert.AreEqual(threadCount - 1, warningCount,
                    "exactly one thread should initialise; the other (threadCount - 1) should hit the duplicate-call warning branch");
            }
            finally
            {
                Log.Writer = null;
            }
        }

        // -----------------------------------------------------------------
        // Track — custom events
        // -----------------------------------------------------------------

        [Test]
        public void Track_CustomEvent_WritesEventToDisk()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track("crafting_started", new Dictionary<string, object>
            {
                { "recipe_id", "iron_sword" }
            });

            // Flush memory → disk
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var files = Directory.GetFiles(queueDir, "*.json");
            // game_launch + crafting_started
            Assert.GreaterOrEqual(files.Length, 2);

            var contents = files.Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"crafting_started\"")),
                "should contain the custom event");
        }

        [Test]
        public void Track_NoProperties_WritesEvent()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track("main_menu_opened");
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"main_menu_opened\"")));
        }

        [Test]
        public void Track_ConsentNone_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));

            ImmutableAudience.Track("should_not_appear");
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            if (!Directory.Exists(queueDir))
            {
                Assert.Pass("queue directory not created — no events");
                return;
            }

            Assert.AreEqual(0, Directory.GetFiles(queueDir, "*.json").Length);
        }

        // -----------------------------------------------------------------
        // Track — typed events
        // -----------------------------------------------------------------

        private class NullNameEvent : IEvent
        {
            public string EventName => null;
            public Dictionary<string, object> ToProperties() => new Dictionary<string, object>();
        }

        private class EmptyNameEvent : IEvent
        {
            public string EventName => "";
            public Dictionary<string, object> ToProperties() => new Dictionary<string, object>();
        }

        [Test]
        public void Track_TypedEvent_NullEventName_IsDropped()
        {
            ImmutableAudience.Init(MakeConfig());

            // Sanity: game_launch is already on disk; drain it first so the
            // assertion counts only our test event.
            ImmutableAudience.FlushQueueToDiskForTesting();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            foreach (var f in Directory.GetFiles(queueDir, "*.json")) File.Delete(f);

            Assert.DoesNotThrow(() => ImmutableAudience.Track(new NullNameEvent()));
            Assert.DoesNotThrow(() => ImmutableAudience.Track(new EmptyNameEvent()));

            ImmutableAudience.FlushQueueToDiskForTesting();
            Assert.AreEqual(0, Directory.GetFiles(queueDir, "*.json").Length,
                "IEvent with null/empty EventName must be dropped, not enqueued");
        }

        [Test]
        public void Track_TypedProgression_WritesCorrectEventName()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(new Progression
            {
                Status = ProgressionStatus.Complete,
                World = "tutorial",
                Level = "1"
            });
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains("\"progression\"") && c.Contains("\"complete\"")));
        }

        [Test]
        public void Track_TypedPurchase_WritesCorrectEventName()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(new Purchase
            {
                Currency = "USD",
                Value = 9.99m
            });
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"purchase\"")));
        }

        // -----------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------

        [Test]
        public void Identify_FullConsent_WritesIdentifyEvent()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            ImmutableAudience.Identify("76561198012345", "steam");
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains("\"identify\"") && c.Contains("\"76561198012345\"")));
        }

        [Test]
        public void Identify_AnonymousConsent_IsIgnored()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            ImmutableAudience.Identify("user1", "steam");
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains("\"identify\"")),
                "identify should be discarded at Anonymous consent");
        }

        [Test]
        public void Alias_FullConsent_WritesAliasEvent()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            ImmutableAudience.Alias("steam123", "steam", "user_456", "passport");
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains("\"alias\"") && c.Contains("\"steam123\"")));
        }

        // -----------------------------------------------------------------
        // Reset
        // -----------------------------------------------------------------

        [Test]
        public void Reset_GeneratesNewAnonymousId()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track("before_reset");
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            ImmutableAudience.Reset();

            ImmutableAudience.Track("after_reset");
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreNotEqual(id1, id2, "Reset should generate a new anonymousId");
        }

        [Test]
        public void Reset_DiscardsQueuedEventsOnDisk()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track("before_reset");
            ImmutableAudience.FlushQueueToDiskForTesting();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            Assert.Greater(Directory.GetFiles(queueDir, "*.json").Length, 0,
                "precondition: queued event should be on disk before reset");

            ImmutableAudience.Reset();

            Assert.AreEqual(0, Directory.GetFiles(queueDir, "*.json").Length,
                "Reset must discard queued events on disk to match the Web SDK");
        }

        // -----------------------------------------------------------------
        // SetConsent — purge + persistence
        // -----------------------------------------------------------------

        [Test]
        public void SetConsent_DowngradeToNone_PurgesQueueOnDiskAndInMemory()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track("event_under_old_consent");

            var queueDir = AudiencePaths.QueueDir(_testDir);
            // Force memory → disk so we can verify the purge wipes both layers.
            ImmutableAudience.FlushQueueToDiskForTesting();
            Assert.Greater(Directory.GetFiles(queueDir, "*.json").Length, 0,
                "precondition: events queued before downgrade exist on disk");

            ImmutableAudience.SetConsent(ConsentLevel.None);

            Assert.AreEqual(0, Directory.GetFiles(queueDir, "*.json").Length,
                "downgrade to None must purge queued events from disk so they can't leak after revocation");
        }

        [Test]
        public void SetConsent_DowngradeToNone_DropsInFlightTrack_ThatRacesThePurge()
        {
            // Reproduces the window where a Track call observed consent=Anonymous,
            // built its message, and is about to enqueue — while a concurrent
            // SetConsent(None) sets consent and purges. Without the re-check inside
            // the drain lock, the enqueue lands after the purge and the event leaks
            // to disk past revocation.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            // Drain the game_launch that Init auto-fires so the assertion below is
            // about our race event only.
            ImmutableAudience.FlushQueueToDiskForTesting();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            foreach (var f in Directory.GetFiles(queueDir, "*.json")) File.Delete(f);

            // Gate the Track thread so it's poised to enqueue at the moment SetConsent
            // completes its purge. We approximate the race by kicking Track off a
            // threadpool thread and racing SetConsent after a tiny stagger — if the
            // re-check is missing, this leaks deterministically under contention over
            // repeated runs.
            var trackStarted = new ManualResetEventSlim(false);
            var trackTask = Task.Run(() =>
            {
                trackStarted.Set();
                ImmutableAudience.Track("racing_event");
            });

            trackStarted.Wait();
            ImmutableAudience.SetConsent(ConsentLevel.None);
            trackTask.Wait(TimeSpan.FromSeconds(5));

            // Flush any residue that a faulty Enqueue may have pushed to memory.
            ImmutableAudience.FlushQueueToDiskForTesting();

            var leaked = Directory.Exists(queueDir)
                ? Directory.GetFiles(queueDir, "*.json").Select(File.ReadAllText)
                    .Count(c => c.Contains("\"racing_event\""))
                : 0;

            Assert.AreEqual(0, leaked,
                "Track that raced SetConsent(None) must not leak past the purge");
        }

        [Test]
        public void SetConsent_DowngradeToNone_StressTest_NoLeak()
        {
            // The single-shot race test above can pass trivially if Track finishes
            // before SetConsent starts on a fast machine. This stress variant runs
            // the race many times with many concurrent Track threads so at least
            // some iterations are guaranteed to land the enqueue inside the
            // _consent=None/PurgeAll window.
            //
            // Without the EnqueueChecked re-check, this test leaks events
            // reproducibly. With the fix, zero leaks across all iterations.
            const int iterations = 200;
            const int trackersPerIteration = 4;

            for (int iter = 0; iter < iterations; iter++)
            {
                ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

                // Clear game_launch so only race events can leak.
                ImmutableAudience.FlushQueueToDiskForTesting();
                var queueDir = AudiencePaths.QueueDir(_testDir);
                if (Directory.Exists(queueDir))
                    foreach (var f in Directory.GetFiles(queueDir, "*.json")) File.Delete(f);

                // All trackers spin up and block on the barrier so they all release
                // simultaneously. The main thread joins the barrier too and fires
                // SetConsent immediately after release — maximising contention.
                var barrier = new Barrier(trackersPerIteration + 1);
                var trackers = new Task[trackersPerIteration];
                for (int t = 0; t < trackersPerIteration; t++)
                {
                    trackers[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.Track("race_stress");
                    });
                }

                barrier.SignalAndWait();
                ImmutableAudience.SetConsent(ConsentLevel.None);
                Task.WaitAll(trackers, TimeSpan.FromSeconds(5));

                // Anything the drain loop hasn't picked up yet → force it.
                ImmutableAudience.FlushQueueToDiskForTesting();

                int leaked = 0;
                if (Directory.Exists(queueDir))
                {
                    leaked = Directory.GetFiles(queueDir, "*.json")
                        .Select(File.ReadAllText)
                        .Count(c => c.Contains("\"race_stress\""));
                }

                if (leaked > 0)
                {
                    Assert.Fail(
                        $"iteration {iter}: {leaked} race_stress events leaked past SetConsent(None)");
                }

                ImmutableAudience.ResetState();
                // Clean state for next iteration so consent isn't carried via disk.
                if (Directory.Exists(AudiencePaths.AudienceDir(_testDir)))
                    Directory.Delete(AudiencePaths.AudienceDir(_testDir), recursive: true);
            }
        }

        [Test]
        public void Init_ConcurrentWithSetConsent_LeavesConsistentState()
        {
            // Pre-fix (before 1784ae3f), SetConsent mutated _consent and
            // _session outside any lock. A SetConsent landing between Init's
            // _initialized = true and its _session = new Session(...)
            // observed _session = null, skipped the dispose path, and let
            // Init finish creating a Session whose timer was never disposed.
            //
            // Limitation: the race window is narrow and not deterministically
            // reproducible without a test hook inside Init. This is a
            // probabilistic guard — many iterations of concurrent Init /
            // SetConsent(None) from two threads, asserting only that the
            // final state is consistent (consent is whichever the last lock
            // holder set, no exceptions escape, Init did not silently ignore
            // the race). Regressions that fully remove SetConsent's lock
            // would still show up here via ConsentLevel mismatches or
            // exceptions on a majority of iterations.
            const int iterations = 50;
            for (int iter = 0; iter < iterations; iter++)
            {
                Exception initEx = null;
                Exception setConsentEx = null;

                var setConsentTask = Task.Run(() =>
                {
                    try
                    {
                        Thread.Yield();
                        ImmutableAudience.SetConsent(ConsentLevel.None);
                    }
                    catch (Exception ex) { setConsentEx = ex; }
                });

                var initTask = Task.Run(() =>
                {
                    try { ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous)); }
                    catch (Exception ex) { initEx = ex; }
                });

                Assert.IsTrue(Task.WaitAll(new[] { initTask, setConsentTask }, TimeSpan.FromSeconds(5)),
                    $"iteration {iter}: Init / SetConsent must complete within 5s");
                Assert.IsNull(initEx, $"iteration {iter}: Init threw {initEx}");
                Assert.IsNull(setConsentEx, $"iteration {iter}: SetConsent threw {setConsentEx}");

                // Either order is valid:
                //   - SetConsent runs first: _initialized is false, SetConsent
                //     early-returns, Init then initialises with Anonymous.
                //   - Init runs first: Init sets Anonymous, SetConsent flips
                //     to None under the lock, consent ends at None.
                var finalConsent = ImmutableAudience.CurrentConsentForTesting;
                Assert.That(finalConsent,
                    Is.EqualTo(ConsentLevel.None).Or.EqualTo(ConsentLevel.Anonymous),
                    $"iteration {iter}: unexpected final consent {finalConsent}");

                ImmutableAudience.ResetState();
                if (Directory.Exists(AudiencePaths.AudienceDir(_testDir)))
                    Directory.Delete(AudiencePaths.AudienceDir(_testDir), recursive: true);
            }
        }

        [Test]
        public void SetConsent_ConcurrentUpgradeFromNone_StartsOneSession_StressTest()
        {
            // Starting from ConsentLevel.None, N threads race to
            // SetConsent(Anonymous). Without the _initLock in SetConsent,
            // multiple threads observe previous == None, each take the
            // upgrade branch, each build a fresh Session, each Start() it.
            // The last _session = new Session(...) wins; the earlier
            // instances keep their heartbeat timers running on the
            // thread pool forever (heartbeats land as dropped-by-CanTrack
            // no-ops but the Timer allocations leak unbounded).
            //
            // Wire-visible symptom: multiple session_start events hit the
            // queue per iteration. With the lock, exactly one thread
            // flips _consent, the rest observe previous == Anonymous and
            // return without touching _session.
            //
            // Sabotage: removing the lock (or widening the else-if to skip
            // the previous-consent check) fails this test reliably within
            // a handful of iterations.
            const int iterations = 100;
            const int callersPerIteration = 4;

            for (int iter = 0; iter < iterations; iter++)
            {
                ImmutableAudience.Init(MakeConfig(ConsentLevel.None));

                var barrier = new Barrier(callersPerIteration);
                var callers = new Task[callersPerIteration];
                for (int c = 0; c < callersPerIteration; c++)
                {
                    callers[c] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.SetConsent(ConsentLevel.Anonymous);
                    });
                }

                Task.WaitAll(callers, TimeSpan.FromSeconds(5));
                ImmutableAudience.FlushQueueToDiskForTesting();

                var queueDir = AudiencePaths.QueueDir(_testDir);
                var sessionStarts = Directory.Exists(queueDir)
                    ? Directory.GetFiles(queueDir, "*.json")
                        .Select(File.ReadAllText)
                        .Count(c => c.Contains("\"session_start\""))
                    : 0;

                if (sessionStarts != 1)
                {
                    Assert.Fail(
                        $"iteration {iter}: expected exactly one session_start from concurrent SetConsent upgrade, got {sessionStarts}");
                }

                ImmutableAudience.ResetState();
                if (Directory.Exists(AudiencePaths.AudienceDir(_testDir)))
                    Directory.Delete(AudiencePaths.AudienceDir(_testDir), recursive: true);
            }
        }

        [Test]
        public void SetConsent_DowngradeToAnonymous_StressTest_NoUserIdLeak()
        {
            // Full → Anonymous race: Track reads _state with userId still set,
            // then SetConsent flips _state to Anonymous and calls
            // ApplyAnonymousDowngrade (one-shot rewrite). If Track's enqueue
            // lands after the rewrite, the msg with userId is not stripped.
            //
            // With the ConsentState + EnqueueChecked transform in place, Track's
            // transform runs under _drainLock and strips userId when current state
            // is not Full. Zero leaks across all iterations.
            //
            // Sabotage: remove the `m.Remove(MessageFields.UserId)` in
            // EnqueueTrack and this test leaks reproducibly.
            const int iterations = 200;
            const int trackersPerIteration = 4;
            const string testUserId = "user_race_stress";

            for (int iter = 0; iter < iterations; iter++)
            {
                ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
                ImmutableAudience.Identify(testUserId, "steam");

                // Clear Init events so only race events can leak.
                ImmutableAudience.FlushQueueToDiskForTesting();
                var queueDir = AudiencePaths.QueueDir(_testDir);
                if (Directory.Exists(queueDir))
                    foreach (var f in Directory.GetFiles(queueDir, "*.json")) File.Delete(f);

                var barrier = new Barrier(trackersPerIteration + 1);
                var trackers = new Task[trackersPerIteration];
                for (int t = 0; t < trackersPerIteration; t++)
                {
                    trackers[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.Track("race_stress");
                    });
                }

                barrier.SignalAndWait();
                ImmutableAudience.SetConsent(ConsentLevel.Anonymous);
                Task.WaitAll(trackers, TimeSpan.FromSeconds(5));

                ImmutableAudience.FlushQueueToDiskForTesting();

                int userIdLeaks = 0;
                if (Directory.Exists(queueDir))
                {
                    userIdLeaks = Directory.GetFiles(queueDir, "*.json")
                        .Select(File.ReadAllText)
                        .Count(c => c.Contains($"\"{testUserId}\""));
                }

                if (userIdLeaks > 0)
                {
                    Assert.Fail(
                        $"iteration {iter}: {userIdLeaks} track events retained userId past SetConsent(Anonymous)");
                }

                ImmutableAudience.ResetState();
                if (Directory.Exists(AudiencePaths.AudienceDir(_testDir)))
                    Directory.Delete(AudiencePaths.AudienceDir(_testDir), recursive: true);
            }
        }

        [Test]
        public void ResetState_ClearsIdentityCache_AcrossInitWithDifferentPath()
        {
            // First init: mints and caches an anonymousId under _testDir.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            var firstId = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            ImmutableAudience.Shutdown();

            // Second init with a different persistentDataPath. If Identity's
            // static cache survives Shutdown, GetOrCreate returns firstId
            // even though the new path has its own (yet-to-be-written) file.
            var otherDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(otherDir);
            try
            {
                var config2 = MakeConfig(ConsentLevel.Anonymous);
                config2.PersistentDataPath = otherDir;
                ImmutableAudience.Init(config2);

                var secondId = Identity.GetOrCreate(otherDir, ConsentLevel.Anonymous);
                Assert.AreNotEqual(firstId, secondId,
                    "ResetState must drop Identity's in-memory cache so the new path mints its own id");
            }
            finally
            {
                Identity.Reset(otherDir);
                if (Directory.Exists(otherDir))
                    Directory.Delete(otherDir, recursive: true);
            }
        }

        [Test]
        public void SetConsent_PersistFailure_SurfacesOnError()
        {
            // Pre-create a directory where ConsentStore.Save wants to place
            // the consent file; File.Move then fails without disturbing
            // Init's DiskStore or Identity paths.
            var consentFile = AudiencePaths.ConsentFile(_testDir);
            Directory.CreateDirectory(consentFile);

            // Bag rather than single capture: ConsentPersistFailed fires
            // synchronously on the caller thread, SyncConsentToBackend's
            // Task.Run may also fire ConsentSyncFailed concurrently. Assert
            // presence of the one under test rather than the last seen.
            var errors = new System.Collections.Concurrent.ConcurrentBag<AudienceError>();
            var config = MakeConfig(ConsentLevel.Anonymous);
            config.OnError = err => errors.Add(err);

            ImmutableAudience.Init(config);
            ImmutableAudience.SetConsent(ConsentLevel.Full);

            Assert.That(errors.Any(e => e.Code == AudienceErrorCode.ConsentPersistFailed),
                Is.True, "OnError should receive ConsentPersistFailed for consent persist failure");
        }

        [Test]
        public void SetConsent_PersistsAcrossInit()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            ImmutableAudience.SetConsent(ConsentLevel.Full);
            ImmutableAudience.Shutdown();

            // Re-init with the *original* (Anonymous) config — persisted Full should win.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            Assert.AreEqual(ConsentLevel.Full, ImmutableAudience.CurrentConsentForTesting,
                "persisted consent must override the config default after restart");
        }

        // -----------------------------------------------------------------
        // game_launch auto-fire
        // -----------------------------------------------------------------

        [Test]
        public void Init_FiresGameLaunch_Automatically()
        {
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"game_launch\"")),
                "Init should auto-fire game_launch");
        }

        [Test]
        public void Init_GameLaunch_IncludesDistributionPlatform()
        {
            var config = MakeConfig();
            config.DistributionPlatform = DistributionPlatforms.Steam;
            ImmutableAudience.Init(config);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains("\"game_launch\"") && c.Contains("\"steam\"")));
        }

        [Test]
        public void Init_ConsentNone_DoesNotFireGameLaunch()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            if (!Directory.Exists(queueDir))
            {
                Assert.Pass();
                return;
            }

            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains("\"game_launch\"")));
        }

        [Test]
        public void Init_GameLaunch_IncludesLaunchContextProviderFields()
        {
            ImmutableAudience.LaunchContextProvider = () => new Dictionary<string, object>
            {
                ["platform"] = "WindowsPlayer",
                ["version"] = "1.2.3",
                ["buildGuid"] = "a1b2c3d4e5f6",
                ["unityVersion"] = "2022.3.20f1",
            };

            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var launchFile = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText)
                .FirstOrDefault(c => c.Contains("\"game_launch\""));
            Assert.IsNotNull(launchFile, "game_launch should have been enqueued");
            StringAssert.Contains("\"platform\":\"WindowsPlayer\"", launchFile);
            StringAssert.Contains("\"version\":\"1.2.3\"", launchFile);
            StringAssert.Contains("\"buildGuid\":\"a1b2c3d4e5f6\"", launchFile);
            StringAssert.Contains("\"unityVersion\":\"2022.3.20f1\"", launchFile);
        }

        [Test]
        public void Init_GameLaunch_ConfigDistributionPlatformOverridesProvider()
        {
            ImmutableAudience.LaunchContextProvider = () => new Dictionary<string, object>
            {
                ["distributionPlatform"] = "provider_value",
            };

            var config = MakeConfig();
            config.DistributionPlatform = DistributionPlatforms.Steam;
            ImmutableAudience.Init(config);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var launchFile = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText)
                .First(c => c.Contains("\"game_launch\""));
            StringAssert.Contains("\"distributionPlatform\":\"steam\"", launchFile);
            Assert.IsFalse(launchFile.Contains("provider_value"),
                "config.DistributionPlatform should win over the provider's value");
        }

        [Test]
        public void Init_GameLaunch_ProviderThrows_StillFiresEvent()
        {
            ImmutableAudience.LaunchContextProvider = () =>
                throw new InvalidOperationException("provider exploded");

            Assert.DoesNotThrow(() => ImmutableAudience.Init(MakeConfig()));
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, "*.json")
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"game_launch\"")),
                "game_launch must still ship when the context provider throws");
        }

        // -----------------------------------------------------------------
        // Shutdown
        // -----------------------------------------------------------------

        [Test]
        public void Shutdown_CalledTwice_DoesNotThrow()
        {
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();
            Assert.DoesNotThrow(() => ImmutableAudience.Shutdown());
        }

        [Test]
        public void Track_AfterShutdown_IsIgnored()
        {
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            Assert.DoesNotThrow(() => ImmutableAudience.Track("should_not_crash"));
        }

        [Test]
        public void Shutdown_ReleasesInitLock_BeforeBlockingTeardown()
        {
            // Hanging handler: the final flush inside Shutdown's Phase 2 will
            // block in transport.SendBatchAsync().Wait(timeoutMs). Pre-refactor,
            // _initLock was held across that wait — SetConsent / Reset on another
            // thread would be stranded for the full ShutdownFlushTimeoutMs.
            var handler = new BlockingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;
            config.ShutdownFlushTimeoutMs = 10_000;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track("ensure_nonempty_queue");
            ImmutableAudience.FlushQueueToDiskForTesting();

            // Phase 1 flips _initialized and releases the lock; Phase 2 enters
            // the hanging final flush.
            var shutdown = Task.Run(() => ImmutableAudience.Shutdown());

            Assert.IsTrue(handler.EnteredSendAsync.Wait(TimeSpan.FromSeconds(5)),
                "Shutdown should reach the hanging final flush inside Phase 2");

            // Reset unconditionally acquires _initLock. If Phase 2 held the lock
            // this would block for ~10s; post-refactor the lock is free, Reset
            // sees !_initialized and returns in microseconds.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ImmutableAudience.Reset();
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 500,
                $"Reset must not block on Shutdown's Phase 2; took {sw.ElapsedMilliseconds}ms");

            handler.Release.Set();
            Assert.IsTrue(shutdown.Wait(TimeSpan.FromSeconds(15)),
                "Shutdown should finish after handler release");
        }

        // -----------------------------------------------------------------
        // Full -> Anonymous consent downgrade
        // -----------------------------------------------------------------

        [Test]
        public void FullToAnonymous_StripsUserIdFromQueuedTrackAndDropsIdentifyAlias()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            ImmutableAudience.Identify("player_steam", IdentityType.Steam);
            ImmutableAudience.Alias("player_steam", IdentityType.Steam, "player_passport", IdentityType.Passport);
            ImmutableAudience.Track("tracked_before_downgrade");

            ImmutableAudience.FlushQueueToDiskForTesting();

            ImmutableAudience.SetConsent(ConsentLevel.Anonymous);

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var files = Directory.GetFiles(queueDir, "*.json");

            foreach (var f in files)
            {
                var msg = JsonReader.DeserializeObject(File.ReadAllText(f));
                var type = (string)msg["type"];
                Assert.AreNotEqual("identify", type, "identify must be purged on Full -> Anonymous");
                Assert.AreNotEqual("alias", type, "alias must be purged on Full -> Anonymous");
                if (type == "track")
                    Assert.IsFalse(msg.ContainsKey("userId"), "userId must be stripped from queued track on Full -> Anonymous");
            }
        }

        [Test]
        public void FullToAnonymous_FutureTracksOmitUserId()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
            ImmutableAudience.Identify("player_steam", IdentityType.Steam);
            ImmutableAudience.SetConsent(ConsentLevel.Anonymous);

            ImmutableAudience.Track("tracked_after_downgrade");
            ImmutableAudience.FlushQueueToDiskForTesting();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var trackFiles = Directory.GetFiles(queueDir, "*.json")
                .Select(f => JsonReader.DeserializeObject(File.ReadAllText(f)))
                .Where(m => (string)m["type"] == "track"
                            && m.ContainsKey("eventName")
                            && (string)m["eventName"] == "tracked_after_downgrade")
                .ToList();

            Assert.AreEqual(1, trackFiles.Count);
            Assert.IsFalse(trackFiles[0].ContainsKey("userId"),
                "Track under Anonymous consent must not carry userId");
        }

        // -----------------------------------------------------------------
        // SendBatch — overlapping timer tick guard
        // -----------------------------------------------------------------

        [Test]
        public void SendBatch_ConcurrentTicks_OnlyOneReachesTransport()
        {
            var handler = new BlockingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track("event_to_send");
            ImmutableAudience.FlushQueueToDiskForTesting();

            // Kick off one SendBatch on a worker — it will block inside the
            // handler until we signal, holding _sendInFlight = 1.
            var blocked = Task.Run(() => ImmutableAudience.SendBatchForTesting());

            // Give the worker enough time to enter the handler's SendAsync.
            Assert.IsTrue(handler.EnteredSendAsync.Wait(TimeSpan.FromSeconds(2)),
                "first SendBatch should have reached the HTTP handler");

            // Second tick while the first is still in flight — must return
            // immediately without issuing another request.
            ImmutableAudience.SendBatchForTesting();

            // Release the blocked send and let it finish.
            handler.Release.Set();
            Assert.IsTrue(blocked.Wait(TimeSpan.FromSeconds(5)),
                "blocked SendBatch should finish after release");

            Assert.AreEqual(1, handler.RequestCount,
                "overlapping tick must not issue a second HTTP request");
        }

        private class BlockingHandler : HttpMessageHandler
        {
            public readonly ManualResetEventSlim EnteredSendAsync = new ManualResetEventSlim(false);
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim(false);
            public int RequestCount;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Interlocked.Increment(ref RequestCount);
                EnteredSendAsync.Set();
                await Task.Run(() => Release.Wait(ct), ct).ConfigureAwait(false);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}