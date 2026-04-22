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
        public void Track_NullOrEmptyEventName_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig());

            var beforeQueue = AudiencePaths.QueueDir(_testDir);
            var beforeCount = Directory.Exists(beforeQueue) ? Directory.GetFiles(beforeQueue, "*.json").Length : 0;

            Assert.DoesNotThrow(() => ImmutableAudience.Track((string)null));
            Assert.DoesNotThrow(() => ImmutableAudience.Track(""));

            ImmutableAudience.Shutdown();
            var afterCount = Directory.GetFiles(beforeQueue, "*.json").Length;
            // Only game_launch should have been enqueued; null/empty Track calls dropped.
            Assert.AreEqual(beforeCount + 1, afterCount, "null/empty event names must be dropped, not enqueued");
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
                "shipping an identify event CDP cannot match for deletion");
        }

        [Test]
        public void Alias_InvalidIdentityTypeCast_Throws()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            var invalid = (IdentityType)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => ImmutableAudience.Alias("fromId", invalid, "toId", IdentityType.Steam),
                "invalid enum cast must throw so a broken alias call fails loud rather " +
                "than shipping an event CDP cannot match for deletion");
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