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
            ImmutableAudience.ContextProvider = null;
            ImmutableAudience.DefaultPersistentDataPathProvider = null;
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        private AudienceConfig MakeConfig(ConsentLevel consent = ConsentLevel.Anonymous)
        {
            return new AudienceConfig
            {
                PublishableKey = TestDefaults.PublishableKey,
                Consent = consent,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = 600, // large; we flush manually in tests
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
        // Diagnostic getters (Initialized / CurrentConsent / UserId /
        // AnonymousId / SessionId / QueueSize)
        // -----------------------------------------------------------------

        [Test]
        public void Initialized_FlipsAroundInitAndShutdown()
        {
            Assert.IsFalse(ImmutableAudience.Initialized,
                "Initialized should be false before Init");

            ImmutableAudience.Init(MakeConfig());
            Assert.IsTrue(ImmutableAudience.Initialized,
                "Initialized should flip true after Init");

            ImmutableAudience.Shutdown();
            Assert.IsFalse(ImmutableAudience.Initialized,
                "Initialized should flip back to false after Shutdown");
        }

        [Test]
        public void CurrentConsent_ReflectsLatestSetConsent()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            Assert.AreEqual(ConsentLevel.Anonymous, ImmutableAudience.CurrentConsent);

            ImmutableAudience.SetConsent(ConsentLevel.Full);
            Assert.AreEqual(ConsentLevel.Full, ImmutableAudience.CurrentConsent);

            ImmutableAudience.SetConsent(ConsentLevel.None);
            Assert.AreEqual(ConsentLevel.None, ImmutableAudience.CurrentConsent);
        }

        [Test]
        public void UserId_Uninitialised_ReturnsNull()
        {
            Assert.IsNull(ImmutableAudience.UserId);
        }

        [Test]
        public void UserId_AfterIdentifyAndReset_TracksState()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
            Assert.IsNull(ImmutableAudience.UserId,
                "UserId should be null until Identify is called");

            ImmutableAudience.Identify(TestFixtures.PlayerCustomId, IdentityType.Custom);
            Assert.AreEqual(TestFixtures.PlayerCustomId, ImmutableAudience.UserId,
                "UserId must reflect the most recent Identify call");

            ImmutableAudience.Reset();
            Assert.IsNull(ImmutableAudience.UserId,
                "Reset must clear UserId so the next player is not attributed to the previous one");
        }

        [Test]
        public void AnonymousId_ConsentNone_ReturnsNull()
        {
            // Anonymous identifier is consent-gated: below tracking consent,
            // no stable id should leak through the getter.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));

            Assert.IsNull(ImmutableAudience.AnonymousId);
        }

        [Test]
        public void AnonymousId_ConsentAnonymous_ReturnsPersistedId()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            // Track once so Identity.GetOrCreate runs and writes the id file.
            ImmutableAudience.Track(TestEventNames.WarmupEvent);

            var id = ImmutableAudience.AnonymousId;
            Assert.IsFalse(string.IsNullOrEmpty(id),
                "AnonymousId should return the persisted id once tracking has created one");
        }

        [Test]
        public void SessionId_MirrorsSessionLifecycle()
        {
            Assert.IsNull(ImmutableAudience.SessionId,
                "SessionId should be null before Init");

            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            Assert.IsFalse(string.IsNullOrEmpty(ImmutableAudience.SessionId),
                "SessionId should be non-null once Init creates a session");

            ImmutableAudience.Shutdown();
            Assert.IsNull(ImmutableAudience.SessionId,
                "SessionId should be null after Shutdown disposes the session");
        }

        [Test]
        public void QueueSize_ZeroBeforeInit_GrowsWithEnqueue()
        {
            Assert.AreEqual(0, ImmutableAudience.QueueSize,
                "QueueSize should be 0 before Init");

            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            // Init enqueues session_start + game_launch; those stay
            // in-memory until a flush. QueueSize sums memory + disk so the
            // pre-flush snapshot must be > 0.
            var afterInit = ImmutableAudience.QueueSize;
            Assert.Greater(afterInit, 0,
                "QueueSize should include session_start and game_launch after Init");

            ImmutableAudience.Track(TestEventNames.ExplicitTrackEvent);
            Assert.Greater(ImmutableAudience.QueueSize, afterInit,
                "QueueSize should grow when a new event is enqueued");
        }

        // -----------------------------------------------------------------
        // Unity context provider
        // -----------------------------------------------------------------

        [Test]
        public void ContextProvider_Set_MergesFieldsIntoEveryMessageContext()
        {
            ImmutableAudience.ContextProvider = () => new Dictionary<string, object>
            {
                [ContextKeys.UserAgent] = "TestOS 1.0",
                [ContextKeys.Locale] = "en-GB",
                [ContextKeys.Timezone] = "Europe/London",
                [ContextKeys.Screen] = "1920x1080",
            };

            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Track(TestEventNames.UnitTestEvent);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var blobs = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Select(File.ReadAllText).ToList();

            Assert.IsTrue(blobs.Any(b =>
                b.Contains($"\"{ContextKeys.UserAgent}\":\"TestOS 1.0\"") &&
                b.Contains($"\"{ContextKeys.Locale}\":\"en-GB\"") &&
                b.Contains($"\"{ContextKeys.Timezone}\":\"Europe/London\"") &&
                b.Contains($"\"{ContextKeys.Screen}\":\"1920x1080\"") &&
                b.Contains($"\"{MessageFields.Library}\":")),
                "Enqueue should merge ContextProvider fields into msg.context alongside library/libraryVersion");
        }

        [Test]
        public void ContextProvider_Set_MergesOnIdentifyPath()
        {
            // EnqueueIdentity must merge ContextProvider fields the same way
            // EnqueueTrack does. Otherwise Identify events ship without the
            // userAgent / locale / timezone / screen context every other
            // event carries.
            ImmutableAudience.ContextProvider = () => new Dictionary<string, object>
            {
                [ContextKeys.UserAgent] = "TestOS 1.0",
                [ContextKeys.Locale] = "en-GB",
            };

            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
            ImmutableAudience.Identify(TestFixtures.PlayerCustomId, IdentityType.Custom);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var blobs = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Select(File.ReadAllText).ToList();

            Assert.IsTrue(blobs.Any(b =>
                b.Contains($"\"{MessageFields.Type}\":\"{MessageTypes.Identify}\"") &&
                b.Contains($"\"{ContextKeys.UserAgent}\":\"TestOS 1.0\"") &&
                b.Contains($"\"{ContextKeys.Locale}\":\"en-GB\"")),
                "Identify message must carry ContextProvider fields in msg.context");
        }

        [Test]
        public void ContextProvider_ThrowingDelegate_SwallowsAndShipsBaseContext()
        {
            ImmutableAudience.ContextProvider = () => throw new InvalidOperationException(TestFixtures.ContextProviderBoomMessage);

            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Track(TestEventNames.UnitTestEvent);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var blobs = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Select(File.ReadAllText).ToList();

            Assert.IsTrue(blobs.Any(b => b.Contains("\"unit_test_event\"") && b.Contains($"\"{MessageFields.Library}\":")),
                "event should still ship with base context when ContextProvider throws");
        }

        [Test]
        public void ContextProvider_ReturnsNull_ShipsBaseContext()
        {
            ImmutableAudience.ContextProvider = () => null;

            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Track(TestEventNames.UnitTestEvent);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var blobs = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Select(File.ReadAllText).ToList();

            Assert.IsTrue(blobs.Any(b => b.Contains("\"unit_test_event\"") && b.Contains($"\"{MessageFields.Library}\":")),
                "event should still ship with base context when ContextProvider returns null");
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
                Assert.That(lines, Has.Some.Contains(AudienceLogs.TrackIEventNull));
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
                // Purchase with no Value set: ToProperties throws; Track must
                // catch, warn, and drop rather than ship an incomplete event.
                Assert.DoesNotThrow(() => ImmutableAudience.Track(new Purchase { Currency = TestFixtures.UsdCurrency }));
                // Assert the stable parts (event-type name and trailing "Dropping")
                // so the test survives any change to the exception type or message.
                Assert.That(lines, Has.Some.Contains(nameof(Purchase)));
                Assert.That(lines, Has.Some.Contains(AudienceLogs.DroppingMarker));
            }
            finally { Log.Writer = null; }

            ImmutableAudience.Shutdown();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains($"\"{EventNames.Purchase}\"")),
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
            // disk drain: Init enqueues session_start + game_launch, and
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
            foreach (var file in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob))
            {
                var msg = JsonReader.DeserializeObject(File.ReadAllText(file));
                if ((string)msg[MessageFields.Type] != MessageTypes.Track) continue;

                if (!msg.TryGetValue(MessageFields.EventName, out var eventNameObj))
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
                () => ImmutableAudience.Alias(TestFixtures.GenericAliasFromId, invalid, TestFixtures.GenericAliasToId, IdentityType.Steam),
                "invalid enum cast must throw so a broken alias call fails loud rather " +
                "than shipping an event that cannot be matched for deletion");
        }

        [Test]
        public void Alias_NullIds_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            Assert.DoesNotThrow(() => ImmutableAudience.Alias(null, IdentityType.Passport, "to", IdentityType.Steam));
            Assert.DoesNotThrow(() => ImmutableAudience.Alias(TestFixtures.GenericAliasFromShort, IdentityType.Passport, "", IdentityType.Steam));
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

                Assert.That(lines, Has.Some.Contains(AudienceLogs.InitCalledTwice),
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

                var warningCount = lines.Count(l => l.Contains(AudienceLogs.InitCalledTwice));
                Assert.AreEqual(threadCount - 1, warningCount,
                    "exactly one thread should initialise; the other (threadCount - 1) should hit the duplicate-call warning branch");
            }
            finally
            {
                Log.Writer = null;
            }
        }

        // -----------------------------------------------------------------
        // Track: custom events
        // -----------------------------------------------------------------

        [Test]
        public void Track_CustomEvent_WritesEventToDisk()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(TestEventNames.CraftingStarted, new Dictionary<string, object>
            {
                { TestFixtures.CustomPropKeyRecipeId, TestFixtures.CraftingRecipeIronSword }
            });

            // Flush memory → disk
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var files = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob);
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

            ImmutableAudience.Track(TestEventNames.MainMenuOpened);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains("\"main_menu_opened\"")));
        }

        [Test]
        public void Track_ConsentNone_DoesNotEnqueue()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));

            ImmutableAudience.Track(TestEventNames.ShouldNotAppear);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            if (!Directory.Exists(queueDir))
            {
                Assert.Pass("queue directory not created; no events");
                return;
            }

            Assert.AreEqual(0, Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length);
        }

        // -----------------------------------------------------------------
        // Track: typed events
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
            foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);

            Assert.DoesNotThrow(() => ImmutableAudience.Track(new NullNameEvent()));
            Assert.DoesNotThrow(() => ImmutableAudience.Track(new EmptyNameEvent()));

            ImmutableAudience.FlushQueueToDiskForTesting();
            Assert.AreEqual(0, Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length,
                "IEvent with null/empty EventName must be dropped, not enqueued");
        }

        [Test]
        public void Track_TypedProgression_WritesCorrectEventName()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(new Progression
            {
                Status = ProgressionStatus.Complete,
                World = TestFixtures.ProgressionWorldTutorial,
                Level = "1"
            });
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
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
                Currency = TestFixtures.UsdCurrency,
                Value = 9.99m
            });
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains($"\"{EventNames.Purchase}\"")));
        }

        // -----------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------

        [Test]
        public void Identify_FullConsent_WritesIdentifyEvent()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            ImmutableAudience.Identify(TestFixtures.SteamId64, IdentityType.Steam);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains($"\"{MessageTypes.Identify}\"") && c.Contains($"\"{TestFixtures.SteamId64}\"")));
        }

        [Test]
        public void Identify_AnonymousConsent_IsIgnored()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            ImmutableAudience.Identify(TestFixtures.GenericUserSingleId, IdentityType.Steam);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains($"\"{MessageTypes.Identify}\"")),
                "identify should be discarded at Anonymous consent");
        }

        [Test]
        public void Alias_FullConsent_WritesAliasEvent()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));

            ImmutableAudience.Alias(TestFixtures.SteamId, IdentityType.Steam, TestFixtures.PassportId, IdentityType.Passport);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains($"\"{MessageTypes.Alias}\"") && c.Contains($"\"{TestFixtures.SteamId}\"")));
        }

        // -----------------------------------------------------------------
        // Reset
        // -----------------------------------------------------------------

        [Test]
        public void Reset_GeneratesNewAnonymousId()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(TestEventNames.BeforeReset);
            var id1 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            ImmutableAudience.Reset();

            ImmutableAudience.Track(TestEventNames.AfterReset);
            var id2 = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);

            Assert.AreNotEqual(id1, id2, "Reset should generate a new anonymousId");
        }

        [Test]
        public void Reset_DiscardsQueuedEventsOnDisk()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(TestEventNames.BeforeReset);
            ImmutableAudience.FlushQueueToDiskForTesting();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            Assert.Greater(Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length, 0,
                "precondition: queued event should be on disk before reset");

            ImmutableAudience.Reset();

            Assert.AreEqual(0, Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length,
                "Reset must discard queued events on disk to match the Web SDK");
        }

        // -----------------------------------------------------------------
        // SetConsent: purge + persistence
        // -----------------------------------------------------------------

        [Test]
        public void SetConsent_DowngradeToNone_PurgesQueueOnDiskAndInMemory()
        {
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.Track(TestEventNames.EventUnderOldConsent);

            var queueDir = AudiencePaths.QueueDir(_testDir);
            // Force memory → disk so we can verify the purge wipes both layers.
            ImmutableAudience.FlushQueueToDiskForTesting();
            Assert.Greater(Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length, 0,
                "precondition: events queued before downgrade exist on disk");

            ImmutableAudience.SetConsent(ConsentLevel.None);

            Assert.AreEqual(0, Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Length,
                "downgrade to None must purge queued events from disk so they can't leak after revocation");
        }

        [Test]
        public void SetConsent_DowngradeToNone_DropsInFlightTrack_ThatRacesThePurge()
        {
            // Reproduces the window where a Track call observed consent=Anonymous,
            // built its message, and is about to enqueue, while a concurrent
            // SetConsent(None) sets consent and purges. Without the re-check inside
            // the drain lock, the enqueue lands after the purge and the event leaks
            // to disk past revocation.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            // Drain the game_launch that Init auto-fires so the assertion below is
            // about our race event only.
            ImmutableAudience.FlushQueueToDiskForTesting();
            var queueDir = AudiencePaths.QueueDir(_testDir);
            foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);

            // Gate the Track thread so it's poised to enqueue at the moment SetConsent
            // completes its purge. We approximate the race by kicking Track off a
            // threadpool thread and racing SetConsent after a tiny stagger; if the
            // re-check is missing, this leaks deterministically under contention over
            // repeated runs.
            var trackStarted = new ManualResetEventSlim(false);
            var trackTask = Task.Run(() =>
            {
                trackStarted.Set();
                ImmutableAudience.Track(TestEventNames.RacingEvent);
            });

            trackStarted.Wait();
            ImmutableAudience.SetConsent(ConsentLevel.None);
            trackTask.Wait(TimeSpan.FromSeconds(5));

            // Flush any residue that a faulty Enqueue may have pushed to memory.
            ImmutableAudience.FlushQueueToDiskForTesting();

            var leaked = Directory.Exists(queueDir)
                ? Directory.GetFiles(queueDir, AudiencePaths.QueueGlob).Select(File.ReadAllText)
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
            // _state=None/PurgeAll window.
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
                    foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);

                // All trackers spin up and block on the barrier so they all release
                // simultaneously. The main thread joins the barrier too and fires
                // SetConsent immediately after release, maximising contention.
                var barrier = new Barrier(trackersPerIteration + 1);
                var trackers = new Task[trackersPerIteration];
                for (int t = 0; t < trackersPerIteration; t++)
                {
                    trackers[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.Track(TestEventNames.RaceStress);
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
                    leaked = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
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
            // probabilistic guard: many iterations of concurrent Init /
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
                var finalConsent = ImmutableAudience.CurrentConsent;
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
                    ? Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
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
            const string testUserId = TestFixtures.UserRaceStress;

            for (int iter = 0; iter < iterations; iter++)
            {
                ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
                ImmutableAudience.Identify(testUserId, IdentityType.Steam);

                // Clear Init events so only race events can leak.
                ImmutableAudience.FlushQueueToDiskForTesting();
                var queueDir = AudiencePaths.QueueDir(_testDir);
                if (Directory.Exists(queueDir))
                    foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);

                var barrier = new Barrier(trackersPerIteration + 1);
                var trackers = new Task[trackersPerIteration];
                for (int t = 0; t < trackersPerIteration; t++)
                {
                    trackers[t] = Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        ImmutableAudience.Track(TestEventNames.RaceStress);
                    });
                }

                barrier.SignalAndWait();
                ImmutableAudience.SetConsent(ConsentLevel.Anonymous);
                Task.WaitAll(trackers, TimeSpan.FromSeconds(5));

                ImmutableAudience.FlushQueueToDiskForTesting();

                int userIdLeaks = 0;
                if (Directory.Exists(queueDir))
                {
                    userIdLeaks = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
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

            // Re-init with the *original* (Anonymous) config. Persisted Full should win.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            Assert.AreEqual(ConsentLevel.Full, ImmutableAudience.CurrentConsent,
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
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains($"\"{EventNames.GameLaunch}\"")),
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
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c =>
                c.Contains($"\"{EventNames.GameLaunch}\"")
                && c.Contains($"\"{GameLaunchPropertyKeys.DistributionPlatform}\":\"{DistributionPlatforms.Steam}\"")));
        }

        [Test]
        public void Init_LowercasesDistributionPlatform_WhenCallerPassesMixedCase()
        {
            var config = MakeConfig();
            config.DistributionPlatform = TestFixtures.DistributionPlatformSteamCased;
            ImmutableAudience.Init(config);

            Assert.AreEqual(DistributionPlatforms.Steam, config.DistributionPlatform,
                "Init should lowercase mixed-case DistributionPlatform so dashboards aggregate consistently.");
        }

        [Test]
        public void Init_LowercasesDistributionPlatform_WhenCallerPassesAllUpperCase()
        {
            var config = MakeConfig();
            config.DistributionPlatform = TestFixtures.DistributionPlatformSteamUppercase;
            ImmutableAudience.Init(config);

            Assert.AreEqual(DistributionPlatforms.Steam, config.DistributionPlatform);
        }

        [Test]
        public void Init_LeavesDistributionPlatformUnchanged_WhenAlreadyLowercase()
        {
            var config = MakeConfig();
            config.DistributionPlatform = DistributionPlatforms.Steam;
            ImmutableAudience.Init(config);

            Assert.AreEqual(DistributionPlatforms.Steam, config.DistributionPlatform);
        }

        // Lowercase normalisation must apply to every DistributionPlatforms value.
        [TestCase(DistributionPlatforms.Steam)]
        [TestCase(DistributionPlatforms.Epic)]
        [TestCase(DistributionPlatforms.GOG)]
        [TestCase(DistributionPlatforms.Itch)]
        [TestCase(DistributionPlatforms.Standalone)]
        public void Init_LowercasesDistributionPlatform_AcrossAllPublicValues(string canonical)
        {
            var config = MakeConfig();
            config.DistributionPlatform = canonical.ToUpperInvariant();
            ImmutableAudience.Init(config);

            Assert.AreEqual(canonical, config.DistributionPlatform);
        }

        [Test]
        public void Init_LeavesDistributionPlatformNull_WhenNotSet()
        {
            var config = MakeConfig();
            Assert.IsNull(config.DistributionPlatform);
            ImmutableAudience.Init(config);

            Assert.IsNull(config.DistributionPlatform);
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

            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsFalse(contents.Any(c => c.Contains($"\"{EventNames.GameLaunch}\"")));
        }

        [Test]
        public void Init_GameLaunch_IncludesLaunchContextProviderFields()
        {
            ImmutableAudience.LaunchContextProvider = () => new Dictionary<string, object>
            {
                [GameLaunchPropertyKeys.Platform] = TestFixtures.PlatformWindows,
                [GameLaunchPropertyKeys.Version] = "1.2.3",
                [GameLaunchPropertyKeys.BuildGuid] = "a1b2c3d4e5f6",
                [GameLaunchPropertyKeys.UnityVersion] = "2022.3.20f1",
            };

            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var launchFile = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText)
                .FirstOrDefault(c => c.Contains($"\"{EventNames.GameLaunch}\""));
            Assert.IsNotNull(launchFile, "game_launch should have been enqueued");
            StringAssert.Contains($"\"{GameLaunchPropertyKeys.Platform}\":\"WindowsPlayer\"", launchFile);
            StringAssert.Contains($"\"{GameLaunchPropertyKeys.Version}\":\"1.2.3\"", launchFile);
            StringAssert.Contains($"\"{GameLaunchPropertyKeys.BuildGuid}\":\"a1b2c3d4e5f6\"", launchFile);
            StringAssert.Contains($"\"{GameLaunchPropertyKeys.UnityVersion}\":\"2022.3.20f1\"", launchFile);
        }

        [Test]
        public void Init_GameLaunch_ConfigDistributionPlatformOverridesProvider()
        {
            ImmutableAudience.LaunchContextProvider = () => new Dictionary<string, object>
            {
                [GameLaunchPropertyKeys.DistributionPlatform] = TestFixtures.ProviderValue,
            };

            var config = MakeConfig();
            config.DistributionPlatform = DistributionPlatforms.Steam;
            ImmutableAudience.Init(config);
            ImmutableAudience.Shutdown();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var launchFile = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText)
                .First(c => c.Contains($"\"{EventNames.GameLaunch}\""));
            StringAssert.Contains($"\"{GameLaunchPropertyKeys.DistributionPlatform}\":\"{DistributionPlatforms.Steam}\"", launchFile);
            Assert.IsFalse(launchFile.Contains(TestFixtures.ProviderValue),
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
            var contents = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(File.ReadAllText).ToList();
            Assert.IsTrue(contents.Any(c => c.Contains($"\"{EventNames.GameLaunch}\"")),
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

            Assert.DoesNotThrow(() => ImmutableAudience.Track(TestEventNames.ShouldNotCrash));
        }

        [Test]
        public void Shutdown_ReleasesInitLock_BeforeBlockingTeardown()
        {
            // Hanging handler: the final flush inside Shutdown's Phase 2 will
            // block in transport.SendBatchAsync().Wait(timeoutMs). Pre-refactor,
            // _initLock was held across that wait; SetConsent / Reset on another
            // thread would be stranded for the full ShutdownFlushTimeoutMs.
            var handler = new BlockingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;
            config.ShutdownFlushTimeoutMs = 10_000;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track(TestEventNames.EnsureNonemptyQueue);
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

            ImmutableAudience.Identify(TestFixtures.PlayerSteamId, IdentityType.Steam);
            ImmutableAudience.Alias(TestFixtures.PlayerSteamId, IdentityType.Steam, TestFixtures.PlayerPassportId, IdentityType.Passport);
            ImmutableAudience.Track(TestEventNames.TrackedBeforeDowngrade);

            ImmutableAudience.FlushQueueToDiskForTesting();

            ImmutableAudience.SetConsent(ConsentLevel.Anonymous);

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var files = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob);

            foreach (var f in files)
            {
                var msg = JsonReader.DeserializeObject(File.ReadAllText(f));
                var type = (string)msg[MessageFields.Type];
                Assert.AreNotEqual(MessageTypes.Identify, type, "identify must be purged on Full -> Anonymous");
                Assert.AreNotEqual(MessageTypes.Alias, type, "alias must be purged on Full -> Anonymous");
                if (type == MessageTypes.Track)
                    Assert.IsFalse(msg.ContainsKey(MessageFields.UserId), "userId must be stripped from queued track on Full -> Anonymous");
            }
        }

        [Test]
        public void FullToAnonymous_FutureTracksOmitUserId()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Full));
            ImmutableAudience.Identify(TestFixtures.PlayerSteamId, IdentityType.Steam);
            ImmutableAudience.SetConsent(ConsentLevel.Anonymous);

            ImmutableAudience.Track(TestEventNames.TrackedAfterDowngrade);
            ImmutableAudience.FlushQueueToDiskForTesting();

            var queueDir = AudiencePaths.QueueDir(_testDir);
            var trackFiles = Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)
                .Select(f => JsonReader.DeserializeObject(File.ReadAllText(f)))
                .Where(m => (string)m[MessageFields.Type] == MessageTypes.Track
                            && m.ContainsKey(MessageFields.EventName)
                            && (string)m[MessageFields.EventName] == TestEventNames.TrackedAfterDowngrade)
                .ToList();

            Assert.AreEqual(1, trackFiles.Count);
            Assert.IsFalse(trackFiles[0].ContainsKey(MessageFields.UserId),
                "Track under Anonymous consent must not carry userId");
        }

        // -----------------------------------------------------------------
        // SendBatch: overlapping timer tick guard
        // -----------------------------------------------------------------

        [Test]
        public void SendBatch_ConcurrentTicks_OnlyOneReachesTransport()
        {
            var handler = new BlockingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track(TestEventNames.EventToSend);
            ImmutableAudience.FlushQueueToDiskForTesting();

            // Kick off one SendBatch on a worker. It will block inside the
            // handler until we signal, holding _sendInFlight = 1.
            var blocked = Task.Run(() => ImmutableAudience.SendBatchForTesting());

            // Give the worker enough time to enter the handler's SendAsync.
            Assert.IsTrue(handler.EnteredSendAsync.Wait(TimeSpan.FromSeconds(2)),
                "first SendBatch should have reached the HTTP handler");

            // Second tick while the first is still in flight: must return
            // immediately without issuing another request.
            ImmutableAudience.SendBatchForTesting();

            // Release the blocked send and let it finish.
            handler.Release.Set();
            Assert.IsTrue(blocked.Wait(TimeSpan.FromSeconds(5)),
                "blocked SendBatch should finish after release");

            Assert.AreEqual(1, handler.RequestCount,
                "overlapping tick must not issue a second HTTP request");
        }

        [Test]
        public void FlushAsync_ConcurrentCallers_OnlyOneReachesTransport()
        {
            // Two parallel FlushAsync calls must serialise on _sendInFlight so
            // ReadBatch/POST pairs do not double-send. Sabotage: remove the
            // gate in FlushAsync and this test sees RequestCount > 1.
            var handler = new BlockingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track(TestEventNames.EventToSend);
            ImmutableAudience.FlushQueueToDiskForTesting();

            // First caller enters SendAsync and blocks on handler.Release.
            var flush1 = Task.Run(() => ImmutableAudience.FlushAsync());
            Assert.IsTrue(handler.EnteredSendAsync.Wait(TimeSpan.FromSeconds(2)),
                "first FlushAsync should reach the HTTP handler");

            // Second caller starts while the first holds the gate; it must
            // wait, not issue a second request.
            var flush2 = Task.Run(() => ImmutableAudience.FlushAsync());

            // Give the second caller a moment to try (and back off).
            Thread.Sleep(200);
            Assert.AreEqual(1, handler.RequestCount,
                "second FlushAsync must not issue a second HTTP request while the first is in-flight");

            handler.Release.Set();
            Assert.IsTrue(Task.WaitAll(new[] { flush1, flush2 }, TimeSpan.FromSeconds(10)),
                "both FlushAsync calls should complete after release");
        }

        [Test]
        public async Task FlushAsync_CancelledToken_Terminates_DoesNotHotLoop()
        {
            // Regression for PR #701 review (@nattb8): if SendBatchAsync
            // silently swallowed caller cancellation, the inner while-loop
            // here would re-enter on the same cancelled token and spin
            // because the batch is never deleted on that code path. The
            // task below would never complete. After the fix, cancellation
            // propagates and the task faults quickly.
            var handler = new CancellingHandler();
            var config = MakeConfig();
            config.HttpHandler = handler;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track(TestEventNames.EventToSend);
            ImmutableAudience.FlushQueueToDiskForTesting();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var flush = ImmutableAudience.FlushAsync(cts.Token);
            var finishedFirst = await Task.WhenAny(flush, Task.Delay(TimeSpan.FromSeconds(2)));

            Assert.AreSame(flush, finishedFirst,
                "FlushAsync must terminate (not hot-loop) when the token is cancelled");
            Assert.IsTrue(flush.IsCanceled || flush.IsFaulted,
                "FlushAsync must propagate the cancellation, not return normally");
            Assert.LessOrEqual(handler.CallCount, 1,
                "a cancelled token must not drive repeated SendAsync attempts");

            // Gate must be released by the finally block; a follow-up flush
            // on an uncancelled token should proceed, proving _sendInFlight
            // is not stranded at 1.
            handler.AcceptNextAsSuccess = true;
            var followUp = ImmutableAudience.FlushAsync();
            Assert.IsTrue(followUp.Wait(TimeSpan.FromSeconds(2)),
                "_sendInFlight must be released after a cancelled flush");
        }

        private class CancellingHandler : HttpMessageHandler
        {
            public int CallCount;
            public bool AcceptNextAsSuccess;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Interlocked.Increment(ref CallCount);
                ct.ThrowIfCancellationRequested();
                var status = AcceptNextAsSuccess ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
                var body = AcceptNextAsSuccess ? "{\"accepted\":1,\"rejected\":0}" : "";
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                });
            }
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