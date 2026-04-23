using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class SessionTests
    {
        private List<(string name, Dictionary<string, object> props)> _events;

        [SetUp]
        public void SetUp()
        {
            _events = new List<(string, Dictionary<string, object>)>();
        }

        private void MockTrack(string name, Dictionary<string, object> props)
        {
            _events.Add((name, props));
        }

        // -----------------------------------------------------------------
        // Start / End
        // -----------------------------------------------------------------

        [Test]
        public void Start_FiresSessionStart_WithSessionId()
        {
            using var session = new Session(MockTrack);
            session.Start();

            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual("session_start", _events[0].name);
            Assert.IsTrue(_events[0].props.ContainsKey("sessionId"));
            Assert.IsNotEmpty((string)_events[0].props["sessionId"]);
        }

        [Test]
        public void Start_GeneratesUniqueSessionId()
        {
            using var session = new Session(MockTrack);
            session.Start();
            var id1 = session.SessionId;

            session.End();
            session.Start();
            var id2 = session.SessionId;

            Assert.AreNotEqual(id1, id2);
        }

        [Test]
        public void End_FiresSessionEnd_WithDuration()
        {
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            using var session = new Session(MockTrack, getUtcNow: () => now);
            session.Start();
            now = now.AddSeconds(2);
            session.End();

            var endEvent = _events.FirstOrDefault(e => e.name == "session_end");
            Assert.IsNotNull(endEvent.props);
            Assert.IsTrue(endEvent.props.ContainsKey("sessionId"));
            Assert.IsTrue(endEvent.props.ContainsKey("durationSec"));
            Assert.AreEqual(2L, (long)endEvent.props["durationSec"]);
        }

        [Test]
        public void Dispose_FiresSessionEnd()
        {
            var session = new Session(MockTrack);
            session.Start();
            session.Dispose();

            Assert.IsTrue(_events.Any(e => e.name == "session_end"));
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotFireTwice()
        {
            var session = new Session(MockTrack);
            session.Start();
            session.Dispose();
            var count = _events.Count(e => e.name == "session_end");
            session.Dispose();
            Assert.AreEqual(count, _events.Count(e => e.name == "session_end"));
        }

        // -----------------------------------------------------------------
        // Heartbeat
        // -----------------------------------------------------------------

        [Test]
        public void Heartbeat_FiresAfterInterval()
        {
            // Timer-driven heartbeat path. Uses the heartbeatIntervalMs
            // constructor override so we can observe the timer without waiting
            // the production 60-second cadence, and a ManualResetEvent to
            // rendezvous on the thread-pool callback.
            using var heartbeatFired = new ManualResetEvent(false);
            var events = new List<(string name, Dictionary<string, object> props)>();
            var gate = new object();

            void Track(string name, Dictionary<string, object> props)
            {
                lock (gate) events.Add((name, props));
                if (name == "session_heartbeat") heartbeatFired.Set();
            }

            using var session = new Session(Track, heartbeatIntervalMs: 50);
            session.Start();

            Assert.IsTrue(heartbeatFired.WaitOne(TimeSpan.FromSeconds(5)),
                "timer-driven heartbeat should fire within 5s of a 50ms interval");

            lock (gate)
            {
                var beat = events.FirstOrDefault(e => e.name == "session_heartbeat");
                Assert.IsNotNull(beat.props, "heartbeat event should carry a properties dictionary");
                Assert.IsTrue(beat.props.ContainsKey("sessionId"));
                Assert.IsTrue(beat.props.ContainsKey("durationSec"));
            }
        }

        // -----------------------------------------------------------------
        // Pause / Resume
        // -----------------------------------------------------------------

        [Test]
        public void Pause_ThenResume_ShortPause_ContinuesSession()
        {
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            using var session = new Session(MockTrack, getUtcNow: () => now);
            session.Start();
            var originalId = session.SessionId;

            session.Pause();
            now = now.AddSeconds(5); // well under the 30 s threshold
            session.Resume();

            Assert.AreEqual(originalId, session.SessionId, "short pause should not change session");
            Assert.IsFalse(_events.Any(e => e.name == "session_end"),
                "short pause should not fire session_end");
        }

        [Test]
        public void Pause_ThenResume_LongPause_StartsNewSession()
        {
            // Uses the injected clock to jump past the 30-second threshold
            // without sleeping.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            using var session = new Session(MockTrack, getUtcNow: () => now);
            session.Start();
            var id1 = session.SessionId;

            session.Pause();
            now = now.AddSeconds(31); // > 30 second PauseTimeoutMs
            session.Resume();

            Assert.AreNotEqual(id1, session.SessionId,
                "pause longer than PauseTimeoutMs should end the old session and start a new one");
            Assert.IsTrue(_events.Any(e => e.name == "session_end"),
                "old session should have fired session_end");
            Assert.AreEqual(2, _events.Count(e => e.name == "session_start"),
                "a new session_start should fire after the long pause");
        }

        [Test]
        public void Pause_CalledTwice_SecondCallIsNoOp()
        {
            // Double-Pause without an intervening Resume must not advance
            // _pausedAt. The live-pause window stays anchored to the first
            // Pause so engagement arithmetic covers the full backgrounded
            // interval. Sabotage: removing the already-paused guard makes
            // _pausedAt jump to the second Pause and this test reports a
            // larger duration (over-crediting engagement by the double-pause
            // gap).
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(5);
            session.Pause(); // first Pause anchors _pausedAt at T=5

            now = now.AddSeconds(3);
            session.Pause(); // second Pause at T=8 — should be ignored

            now = now.AddSeconds(2); // paused for another 2s
            session.Resume(); // resume at T=10, pause window spans T=5 to T=10

            now = now.AddSeconds(3); // 3s more engagement
            session.End();

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            // Wall-clock Start→End = 13s, paused from T=5 to T=10 = 5s, engaged = 8s.
            Assert.AreEqual(8L, duration,
                "double Pause must preserve the first Pause timestamp so engagement arithmetic covers the full pause window");
        }

        [Test]
        public void Resume_WithoutPause_IsNoOp()
        {
            using var session = new Session(MockTrack);
            session.Start();
            var eventsBefore = _events.Count;

            session.Resume();

            Assert.AreEqual(eventsBefore, _events.Count, "resume without pause should not fire events");
        }

        // -----------------------------------------------------------------
        // Pause-adjusted duration
        // -----------------------------------------------------------------

        [Test]
        public void Resume_NegativePauseDuration_ClampsAccumulatorToZero()
        {
            // Wall-clock can rewind during a pause: NTP correction, manual
            // clock change, or a debugger that froze the process. Without
            // the clamp at Resume, a negative pauseDuration would shrink
            // _accumulatedPause and hand the next session_end an
            // artificial engagement credit. Sabotage: removing the clamp
            // would let this test report a duration that exceeds the
            // wall-clock window, which the assertion below pins.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(10); // 10 s engaged
            session.Pause();

            now = now.AddSeconds(-5); // clock rewinds 5 s during the pause
            session.Resume();

            now = now.AddSeconds(2); // 2 s further engagement after resume
            session.End();

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            // Wall-clock from Start to End is 10 + (-5) + 2 = 7 s. The
            // pause duration was clamped to 0, so engaged seconds = 7 - 0 = 7.
            // Without the clamp, _accumulatedPause would be -5, the
            // subtraction would over-credit, and engaged seconds would
            // be 12 — well outside the wall-clock window.
            Assert.AreEqual(7L, duration,
                "negative pauseDuration must clamp to zero so the accumulator does not over-credit engagement");
        }

        [Test]
        public void End_ClockRewindsSinceStart_ClampsDurationToZero()
        {
            // Wall-clock can rewind between Start and End with no pause
            // in between (server-side NTP correction, headless build with
            // a manual clock set). Without the clamp in
            // ComputeEngagedSecondsLocked, End would ship a negative
            // duration. The Resume-side clamp does not cover this path
            // because it only fires when _pausedAt was set. Sabotage:
            // removing `if (engagedSeconds < 0) return 0;` would let this
            // test report -3 instead of 0.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(-3); // clock rewinds after Start, no pause
            session.End();

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            Assert.AreEqual(0L, duration,
                "negative engaged time from a wall-clock rewind must clamp to zero");
        }

        [Test]
        public void End_ClockRewindsWhilePaused_DoesNotInflateDuration()
        {
            // Wall-clock rewinds while the session is still paused (e.g. the
            // app is backgrounded and NTP corrects backwards before Shutdown
            // fires End). Without the livePause ≥ 0 clamp in
            // ComputeEngagedSecondsLocked, livePause goes negative and,
            // being subtracted, inflates duration past the wall-clock window
            // — the final engagedSeconds ≥ 0 clamp only catches negatives,
            // not over-credit. Sabotage: removing the livePause clamp lets
            // this test report 15s instead of the ≤ 5s wall-clock window.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(10);
            session.Pause();

            now = now.AddSeconds(-5); // clock rewinds 5s while paused
            session.End();

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            Assert.LessOrEqual(duration, 5L,
                "clock rewind while paused must not over-credit engagement past the wall-clock window");
        }

        [Test]
        public void End_AfterShortPause_ReportsDurationMinusPause()
        {
            // 10 seconds session, 3 seconds paused inside it → 7 seconds engaged.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(4);
            session.Pause();

            now = now.AddSeconds(3); // 3 s paused
            session.Resume();

            now = now.AddSeconds(3);
            session.End();

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            Assert.AreEqual(7L, duration,
                "session_end duration should exclude the 3s paused interval");
        }

        [Test]
        public void End_WhilePaused_ExcludesInFlightPauseFromDuration()
        {
            // Session running 5s, then paused for 2s and ended without resuming.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(5);
            session.Pause();

            now = now.AddSeconds(2);
            session.End(); // ends while paused

            var sessionEnd = _events.Last(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            Assert.AreEqual(5L, duration,
                "session_end fired while paused should count only pre-pause engaged time");
        }

        [Test]
        public void End_AfterExtendedPauseRollover_ReportsPrePauseDuration()
        {
            // Extended pause (>30 s) on Resume runs End → Start. The
            // session_end event for the old session must report only the
            // engaged time before the pause, not wall-clock from start to
            // resume (which would include the pause). Regression guard
            // for the extended-pause rollover path: a naive duration that
            // forgot to credit the in-flight pause before End fires would
            // ship wall-clock seconds and break engagement dashboards.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(10); // 10 s engaged before pause
            session.Pause();

            now = now.AddSeconds(40); // 40 s paused — extended (>30 s threshold)
            session.Resume();

            var sessionEnd = _events.First(e => e.name == "session_end");
            var duration = (long)sessionEnd.props["durationSec"];
            Assert.AreEqual(10L, duration,
                "session_end on extended-pause rollover should report pre-pause engaged time, not wall-clock");
        }

        [Test]
        public void Heartbeat_AfterShortPause_ReportsPauseAdjustedDuration()
        {
            // Engaged 6s, paused 2s, resumed, then heartbeat → 6 s engaged.
            var now = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
            DateTime Clock() => now;

            using var session = new Session(MockTrack, getUtcNow: Clock);
            session.Start();

            now = now.AddSeconds(4);
            session.Pause();

            now = now.AddSeconds(2);
            session.Resume();

            now = now.AddSeconds(2);
            session.OnHeartbeat();

            var heartbeat = _events.Last(e => e.name == "session_heartbeat");
            var duration = (long)heartbeat.props["durationSec"];
            Assert.AreEqual(6L, duration,
                "heartbeat duration should exclude the 2s paused interval");
        }

        // -----------------------------------------------------------------
        // Double-Start safety
        // -----------------------------------------------------------------

        [Test]
        public void Start_WithoutPriorEnd_DoesNotStrandTheOldTimer()
        {
            // Call Start twice in a row. The implementation should stop the
            // first timer cleanly; the second heartbeat scheduling should
            // succeed and the second session id takes over.
            using var session = new Session(MockTrack);
            session.Start();
            var firstId = session.SessionId;

            session.Start();
            var secondId = session.SessionId;

            Assert.AreNotEqual(firstId, secondId,
                "second Start should generate a fresh sessionId");

            Assert.AreEqual(2, _events.Count(e => e.name == "session_start"),
                "both session_start events should fire");
        }

        // -----------------------------------------------------------------
        // Heartbeat-during-pause quiescence
        // -----------------------------------------------------------------

        [Test]
        public void Heartbeat_WhilePaused_DoesNotFire()
        {
            // The class contract is session_heartbeat every 60s while
            // foregrounded. A paused session is backgrounded by definition,
            // so OnHeartbeat must not emit until Resume clears the pause.
            // Without this guard a backgrounded alt-tab would dribble
            // stable-duration heartbeats for the entire pause window.
            using var session = new Session(MockTrack);
            session.Start();
            session.Pause();

            session.OnHeartbeat();

            Assert.IsFalse(_events.Any(e => e.name == "session_heartbeat"),
                "OnHeartbeat should not emit while the session is paused");
        }

        [Test]
        public void End_HeartbeatExceedsDrainBudget_LogsWarningAndContinues()
        {
            // DrainHeartbeatTimer waits up to 1 second for an in-flight
            // heartbeat callback before End emits session_end. If the
            // callback exceeds the budget the implementation logs a
            // warning and continues, accepting the risk of a trailing
            // heartbeat racing the next lifecycle event. Sabotage paths
            // that would skip the warning: raising the budget past the
            // 1.5 s callback sleep (WaitOne returns true before timeout);
            // removing the `if (!waited.WaitOne(...))` guard. WaitOne
            // (TimeSpan.Zero) on an unsignaled handle returns false, so
            // lowering the budget does not skip the warning. This test
            // pins the budget-exceeded path so future drain-budget edits
            // cannot silently drop the warning.
            var warnings = new List<string>();
            var prevWriter = Log.Writer;
            Log.Writer = line => { lock (warnings) warnings.Add(line); };
            try
            {
                using var beatStarted = new ManualResetEvent(false);
                void Track(string name, Dictionary<string, object> props)
                {
                    if (name == "session_heartbeat")
                    {
                        beatStarted.Set();
                        // Block past the 1 s drain budget so DrainHeartbeatTimer
                        // times out. Self-releases after 1.5 s so the callback
                        // does eventually finish.
                        Thread.Sleep(1500);
                    }
                }

                using var session = new Session(Track, heartbeatIntervalMs: 50);
                session.Start();
                Assert.IsTrue(beatStarted.WaitOne(TimeSpan.FromSeconds(2)),
                    "heartbeat callback must enter before End is invoked");

                session.End();

                lock (warnings)
                {
                    Assert.IsTrue(warnings.Any(w => w.Contains("heartbeat callback did not complete")),
                        "End must log the drain-timeout warning when an in-flight heartbeat exceeds 1 s");
                }
            }
            finally
            {
                Log.Writer = prevWriter;
            }
        }

        [Test]
        public void Heartbeat_AfterResume_Fires()
        {
            // Pair for Heartbeat_WhilePaused_DoesNotFire: once Resume
            // clears _pausedAt, heartbeats must flow again.
            using var session = new Session(MockTrack);
            session.Start();
            session.Pause();
            session.Resume();

            session.OnHeartbeat();

            Assert.IsTrue(_events.Any(e => e.name == "session_heartbeat"),
                "OnHeartbeat should emit again once the session is resumed");
        }

        // -----------------------------------------------------------------
        // Exception containment
        // -----------------------------------------------------------------

        [Test]
        public void OnHeartbeat_TrackCallbackThrows_DoesNotEscape()
        {
            // OnHeartbeat runs on a thread-pool timer callback; an unhandled
            // exception there can terminate the process on .NET 5+. The
            // SafeTrack wrapper catches and logs instead. Sabotage: removing
            // the try/catch in SafeTrack would let the exception propagate
            // up to the caller (which here is the test thread, so this
            // assertion would fail).
            var warnings = new List<string>();
            var prevWriter = Log.Writer;
            Log.Writer = line => { lock (warnings) warnings.Add(line); };
            try
            {
                void ThrowingTrack(string name, Dictionary<string, object> props)
                {
                    if (name == "session_heartbeat")
                        throw new InvalidOperationException("track explode");
                }

                using var session = new Session(ThrowingTrack);
                session.Start();

                Assert.DoesNotThrow(() => session.OnHeartbeat(),
                    "a throwing track callback on the heartbeat path must not escape Session");

                lock (warnings)
                {
                    Assert.IsTrue(warnings.Any(w => w.Contains("session_heartbeat track callback threw")),
                        "SafeTrack must log a warning when the callback throws");
                }
            }
            finally { Log.Writer = prevWriter; }
        }

        [Test]
        public void Start_TrackCallbackThrows_DoesNotEscape()
        {
            // Start fires session_start via _track. A throwing implementation
            // would otherwise propagate into Init / SetConsent on the caller's
            // thread. SafeTrack swallows and logs. Sabotage: removing
            // SafeTrack's try/catch fails Assert.DoesNotThrow; a regression
            // that swallows silently without logging fails the warning check.
            var warnings = new List<string>();
            var prevWriter = Log.Writer;
            Log.Writer = line => { lock (warnings) warnings.Add(line); };
            try
            {
                void ThrowingTrack(string name, Dictionary<string, object> props)
                {
                    if (name == "session_start")
                        throw new InvalidOperationException("track explode");
                }

                using var session = new Session(ThrowingTrack);

                Assert.DoesNotThrow(() => session.Start(),
                    "a throwing track callback on session_start must not escape Start");

                lock (warnings)
                {
                    Assert.IsTrue(warnings.Any(w => w.Contains("session_start track callback threw")),
                        "SafeTrack must log a warning when the Start callback throws");
                }
            }
            finally { Log.Writer = prevWriter; }
        }

        [Test]
        public void End_TrackCallbackThrows_DoesNotEscape()
        {
            // End fires session_end via _track. Dispose wraps End, so a
            // throwing implementation would otherwise propagate into
            // Shutdown / SetConsent on the caller's thread. Sabotage:
            // removing SafeTrack's try/catch fails Assert.DoesNotThrow; a
            // regression that swallows silently without logging fails the
            // warning check.
            var warnings = new List<string>();
            var prevWriter = Log.Writer;
            Log.Writer = line => { lock (warnings) warnings.Add(line); };
            try
            {
                void ThrowingTrack(string name, Dictionary<string, object> props)
                {
                    if (name == "session_end")
                        throw new InvalidOperationException("track explode");
                }

                using var session = new Session(ThrowingTrack);
                session.Start();

                Assert.DoesNotThrow(() => session.End(),
                    "a throwing track callback on session_end must not escape End");

                lock (warnings)
                {
                    Assert.IsTrue(warnings.Any(w => w.Contains("session_end track callback threw")),
                        "SafeTrack must log a warning when the End callback throws");
                }
            }
            finally { Log.Writer = prevWriter; }
        }

        [Test]
        public void SafeTrack_LogWriterThrows_DoesNotEscape()
        {
            // SafeTrack logs to Log.Warn when the _track delegate throws. If
            // the Log.Writer itself throws, the log call would escape
            // SafeTrack's catch and propagate up to the Timer thread (process
            // kill on .NET 5+) or the caller thread (Init / Shutdown /
            // SetConsent). Log.Emit's internal try/catch must swallow the
            // Writer failure. Sabotage: removing Log.Emit's try/catch would
            // fail Assert.DoesNotThrow below.
            var prevWriter = Log.Writer;
            Log.Writer = _ => throw new InvalidOperationException("log explode");
            try
            {
                void ThrowingTrack(string name, Dictionary<string, object> props)
                {
                    if (name == "session_heartbeat")
                        throw new InvalidOperationException("track explode");
                }

                using var session = new Session(ThrowingTrack);
                session.Start();

                Assert.DoesNotThrow(() => session.OnHeartbeat(),
                    "Log.Writer throwing inside SafeTrack's catch must not escape Session");
            }
            finally { Log.Writer = prevWriter; }
        }
    }

    // -----------------------------------------------------------------
    // Session integration through ImmutableAudience
    // -----------------------------------------------------------------

    [TestFixture]
    internal class SessionIntegrationTests
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
                FlushIntervalSeconds = 600,
                FlushSize = 1000,
                HttpHandler = new KeepOnDiskHandler()
            };
        }

        // Reads every queued event file. Returns an empty array when the
        // queue directory has not been created yet (SetConsent(None) purges
        // it, Init with ConsentLevel.None never writes one) so tests can
        // assert "no such event" without a DirectoryNotFoundException crash
        // masking the real signal.
        private string[] ReadQueueFiles()
        {
            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            if (!Directory.Exists(queueDir)) return Array.Empty<string>();
            return Directory.GetFiles(queueDir, "*.json").Select(File.ReadAllText).ToArray();
        }

        [Test]
        public void Init_FiresSessionStart()
        {
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            Assert.IsTrue(ReadQueueFiles().Any(c => c.Contains("\"session_start\"")));
        }

        [Test]
        public void Shutdown_FiresSessionEnd()
        {
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Shutdown();

            Assert.IsTrue(ReadQueueFiles().Any(c => c.Contains("\"session_end\"")));
        }

        [Test]
        public void Init_ConsentNone_DoesNotStartSession()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));
            ImmutableAudience.Shutdown();

            Assert.IsFalse(ReadQueueFiles().Any(c => c.Contains("\"session_start\"")));
        }

        [Test]
        public void SetConsent_NoneToAnonymous_StartsSession()
        {
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));
            ImmutableAudience.SetConsent(ConsentLevel.Anonymous);
            ImmutableAudience.Shutdown();

            Assert.IsTrue(ReadQueueFiles().Any(c => c.Contains("\"session_start\"")),
                "upgrading from None should start a session");
        }

        [Test]
        public void OnPauseAndOnResume_RouteThroughActiveSession()
        {
            // Pin that OnPause / OnResume actually reach the live Session,
            // not just that they compile. A heartbeat fired while the
            // session is paused must not emit; after OnResume the next
            // heartbeat must emit. Sabotage: emptying either wrapper body
            // fails one of the two assertions below.
            //
            // Signature-pin (rename / parameter change) also still holds
            // via the direct ImmutableAudience.OnPause() / OnResume() call
            // sites — a change there breaks compilation here.
            ImmutableAudience.Init(MakeConfig());

            ImmutableAudience.OnPause();
            ImmutableAudience.InvokeSessionHeartbeatForTesting();
            ImmutableAudience.FlushQueueToDiskForTesting();

            Assert.IsFalse(
                ReadQueueFiles().Any(c => c.Contains("\"session_heartbeat\"")),
                "OnPause must route to Session.Pause — paused sessions quiesce heartbeats");

            ImmutableAudience.OnResume();
            ImmutableAudience.InvokeSessionHeartbeatForTesting();
            ImmutableAudience.FlushQueueToDiskForTesting();

            Assert.IsTrue(
                ReadQueueFiles().Any(c => c.Contains("\"session_heartbeat\"")),
                "OnResume must route to Session.Resume — resumed sessions emit heartbeats again");

            ImmutableAudience.Shutdown();
        }

        [Test]
        public void OnPauseAndOnResume_BeforeInit_AreNoOps()
        {
            // Both wrappers gate on _initialized and return early. A
            // lifecycle bridge that fires before Init (rare, but possible
            // on subsystem-registration order quirks) must not throw.
            Assert.DoesNotThrow(() => ImmutableAudience.OnPause());
            Assert.DoesNotThrow(() => ImmutableAudience.OnResume());
        }

        [Test]
        public void Reset_StartsNewSession_DoesNotEmitSessionEnd()
        {
            // Reset must end the old session and start a new one so subsequent
            // Track events carry a fresh sessionId alongside the fresh
            // anonymousId — matches Web SDK reset() semantics. The old
            // session's session_end is enqueued by Session.Dispose but wiped
            // by the PurgeAll in Reset, so the wire sees only a session_start
            // for the new session.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));

            // Drain game_launch + session_start for the initial session so we
            // only see post-Reset events.
            ImmutableAudience.FlushQueueToDiskForTesting();
            var queueDir = Path.Combine(_testDir, "imtbl_audience", "queue");
            foreach (var f in Directory.GetFiles(queueDir, "*.json")) File.Delete(f);

            var firstAnonymousId = Identity.Get(_testDir);
            Assert.IsNotNull(firstAnonymousId, "first session should have minted an anonymousId");

            ImmutableAudience.Reset();
            ImmutableAudience.FlushQueueToDiskForTesting();

            var files = ReadQueueFiles();
            Assert.IsTrue(files.Any(c => c.Contains("\"session_start\"")),
                "Reset must fire session_start for the new session");
            Assert.IsFalse(files.Any(c => c.Contains("\"session_end\"")),
                "Reset must not leak session_end for the old session (Web SDK parity)");

            var secondAnonymousId = Identity.Get(_testDir);
            Assert.IsNotNull(secondAnonymousId, "Reset should have minted a fresh anonymousId");
            Assert.AreNotEqual(firstAnonymousId, secondAnonymousId,
                "Reset must mint a new anonymousId");

            ImmutableAudience.Shutdown();
        }

        [Test]
        public void Reset_ConsentNone_DoesNotStartSession()
        {
            // At consent=None there is no session running; Reset must not
            // spin one up (Web SDK reset() guards on !isTrackingDisabled()).
            ImmutableAudience.Init(MakeConfig(ConsentLevel.None));

            ImmutableAudience.Reset();
            ImmutableAudience.Shutdown();

            Assert.IsFalse(ReadQueueFiles().Any(c => c.Contains("\"session_start\"")),
                "Reset at consent=None must not fire session_start");
        }

        [Test]
        public void SetConsent_AnonymousToNone_DoesNotEmitSessionEnd()
        {
            // Consent revocation purges the queue and disposes the session.
            // Session.Dispose fires End → Track("session_end"), but by the
            // time End runs the consent level has already been flipped to
            // None, so CanTrack gates the track call. No session_end event
            // should appear on disk. Regression guard: a future reorder of
            // "flip consent" vs "dispose session" would silently leak a
            // session_end that the consent-revocation promise forbids.
            ImmutableAudience.Init(MakeConfig(ConsentLevel.Anonymous));
            ImmutableAudience.SetConsent(ConsentLevel.None);
            ImmutableAudience.Shutdown();

            Assert.IsFalse(
                ReadQueueFiles().Any(c => c.Contains("\"session_end\"")),
                "SetConsent(None) must not leak a session_end event past the queue purge");
        }

        private class KeepOnDiskHandler : System.Net.Http.HttpMessageHandler
        {
            protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(
                System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken ct)
            {
                return System.Threading.Tasks.Task.FromResult(
                    new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
            }
        }
    }
}