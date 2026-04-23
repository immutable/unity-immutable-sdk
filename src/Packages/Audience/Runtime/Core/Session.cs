#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Immutable.Audience
{
    // Session lifecycle. session_start on Init, session_heartbeat every 60s
    // while foregrounded (quiesced during Pause), session_end on Shutdown or
    // pause > 30s evaluated on the next Resume. Shares the Web SDK wire
    // schema (sessionId plus integer "duration" in seconds) but not the
    // duration semantic — see End() for detail. Unity-specific extensions:
    // engagement-aware duration (excludes pause time) and heartbeat-carried
    // performance metrics. Games don't have browser visibility APIs, and
    // long alt-tabs would inflate session time if we counted wall-clock.
    //
    // Thread safety: heartbeat fires on a thread-pool thread; Start / End /
    // Pause / Resume / Dispose run on the caller's thread. A private lock
    // guards state. The _track callback fires outside the lock so re-entrant
    // track implementations can safely take their own locks.
    internal sealed class Session : IDisposable
    {
        internal const int HeartbeatIntervalMs = 60_000;

        // 30 seconds. Intentionally short: a player who alt-tabs out of the
        // game for half a minute has stopped engaging and the next resume
        // should roll the session. Not a port of Web SDK SESSION_MAX_AGE
        // (30 minutes idle on the cookie). Browsers have no foreground
        // pause concept, so the two values measure different things.
        internal const int PauseTimeoutMs = 30_000;

        private readonly Action<string, Dictionary<string, object>> _track;
        private readonly Func<Dictionary<string, object>>? _performanceSnapshot;
        private readonly Func<DateTime> _getUtcNow;
        private readonly int _heartbeatIntervalMs;
        private readonly object _lock = new object();

        private Timer? _heartbeatTimer;
        private string? _sessionId;
        private DateTime _sessionStart;
        private DateTime? _pausedAt;
        // Total pause time accumulated across every Pause/Resume cycle
        // since Start. Subtracted from wall-clock in session_end so
        // "duration" reflects engagement rather than real time. Post-resume
        // session_heartbeat callers read this too, so a resumed session's
        // first tick does not overcount the interval that spanned the pause;
        // heartbeats that fire while _pausedAt is set are quiesced at the
        // top of OnHeartbeat and never touch the accumulator.
        private TimeSpan _accumulatedPause;
        private bool _disposed;

        // Current session ID. Null before Start() is called and after End()/Dispose().
        internal string? SessionId
        {
            get { lock (_lock) return _sessionId; }
        }

        // track fires session_start / _heartbeat / _end via
        // ImmutableAudience.Track (passed as a delegate so tests can drive
        // Session with a mock and without touching the static SDK surface).
        // performanceSnapshot merges fps/memory into each heartbeat; null
        // on non-Unity runtimes. getUtcNow and heartbeatIntervalMs are test
        // seams that production callers leave at their defaults (system
        // clock, 60_000 ms).
        internal Session(
            Action<string, Dictionary<string, object>> track,
            Func<Dictionary<string, object>>? performanceSnapshot = null,
            Func<DateTime>? getUtcNow = null,
            int heartbeatIntervalMs = HeartbeatIntervalMs)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _performanceSnapshot = performanceSnapshot;
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
            _heartbeatIntervalMs = heartbeatIntervalMs;
        }

        // Start a new session. Fires session_start and begins the heartbeat timer.
        internal void Start()
        {
            // Phase 1 — stop the old heartbeat timer (if any) and drain its
            // in-flight callback OUTSIDE the lock before we mutate session
            // state. The callback itself acquires _lock, so waiting on the
            // drain under _lock would deadlock. Leaving _sessionId /
            // _sessionStart / _accumulatedPause at their old values across
            // the drain means a trailing callback emits a heartbeat for the
            // OLD session — wire-ordered before the new session_start —
            // instead of reading the new state with zero-duration noise.
            //
            // Double-Start is a misuse path (supported pattern is End/Dispose
            // then Start); on the normal End→Start rollover _heartbeatTimer
            // is already null from End's drain, so this drain is a no-op.
            Timer? oldTimer;
            lock (_lock)
            {
                if (_disposed) return;
                oldTimer = _heartbeatTimer;
                if (oldTimer != null)
                {
                    oldTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _heartbeatTimer = null;
                }
            }

            if (oldTimer != null)
            {
                using var waited = new ManualResetEvent(false);
                try
                {
                    // 500 ms budget (vs End's 1 s) because double-Start is
                    // unexpected; we do not want a mistake path to block the
                    // caller for a full second. Dispose(wh) returns false on
                    // an already-disposed timer — skip the wait in that case.
                    if (oldTimer.Dispose(waited))
                        waited.WaitOne(TimeSpan.FromMilliseconds(500));
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // Phase 2 — atomically populate new session state and schedule
            // the new timer. _disposed may have flipped during the drain;
            // re-check.
            string sessionId;
            lock (_lock)
            {
                if (_disposed) return;

                _sessionId = Guid.NewGuid().ToString();
                _sessionStart = _getUtcNow();
                _pausedAt = null;
                _accumulatedPause = TimeSpan.Zero;

                sessionId = _sessionId;
                _heartbeatTimer = new Timer(_ => OnHeartbeat(), null, _heartbeatIntervalMs, _heartbeatIntervalMs);
            }

            SafeTrack("session_start", new Dictionary<string, object>
            {
                ["sessionId"] = sessionId
            });
        }

        // Called when the game loses focus (e.g. alt-tab, minimize). Records
        // the pause start; quiesces session_heartbeat so no engagement signal
        // is emitted while the app is backgrounded. The 30s pause threshold
        // is evaluated on the next Resume call — no timer fires on Pause, so
        // a game that never resumes (force-killed while backgrounded) sees
        // session_end only when Shutdown runs. Expected to be called on the
        // Unity main thread by the lifecycle bridge.
        internal void Pause()
        {
            lock (_lock)
            {
                if (_disposed || _sessionId == null) return;
                // Already paused — keep the original _pausedAt so the live
                // pause window stays anchored to the first Pause. Without
                // this guard a double-Pause would advance _pausedAt forward
                // and ComputeEngagedSecondsLocked's (now - _pausedAt) live
                // pause would undercount the window by the delta, over-
                // crediting engagement time.
                if (_pausedAt.HasValue) return;
                _pausedAt = _getUtcNow();
            }
        }

        // Called when the game regains focus. If paused up to 30s the
        // session continues; any longer and the old session ends and a new
        // one starts. Expected to be called on the Unity main thread by the
        // lifecycle bridge.
        internal void Resume()
        {
            bool extended;
            lock (_lock)
            {
                if (_disposed || _sessionId == null || _pausedAt == null) return;

                var pauseDuration = _getUtcNow() - _pausedAt.Value;
                _pausedAt = null;

                // Clamp to zero for wall-clock rewinds (NTP correction, manual
                // clock change). A negative pauseDuration would reduce
                // _accumulatedPause and hand the next session an artificial
                // engagement credit. End already clamps the outgoing duration,
                // but the accumulator needs its own guard.
                if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;

                extended = pauseDuration.TotalMilliseconds > PauseTimeoutMs;

                // Credit the pause in both paths. Short pauses: End will never
                // fire for this session, the credit prevents heartbeats from
                // overcounting. Extended pauses: End fires next and subtracts
                // the credit so session_end reports the pre-pause engaged time,
                // then resets _accumulatedPause to zero before Start runs. No
                // risk of double-count — the reset in End (and again in Start)
                // is what makes the extended-pause path safe to credit here.
                _accumulatedPause += pauseDuration;
            }

            if (extended)
            {
                // Extended pause — end old session, start new one. Both fire
                // their track events outside our lock so reentrant track
                // implementations can safely take their own locks.
                //
                // Invariant between End() and Start(): _sessionId is null
                // (End reset it), _heartbeatTimer is null (drained), and
                // _disposed may flip if Dispose races. Every other public
                // method (Pause, Resume, End, OnHeartbeat, Dispose) guards
                // on one of those fields and early-returns, so a concurrent
                // call landing in this window is a no-op rather than a
                // corruption vector.
                End();
                Start();
            }
        }

        // End the current session. Fires session_end and stops the heartbeat.
        //
        // Drains any in-flight heartbeat callback before emitting session_end
        // so the usual wire ordering (heartbeats -> session_end) holds. On
        // drain timeout (1 s, see DrainHeartbeatTimer), ordering is
        // best-effort: a stuck heartbeat callback whose _track call is still
        // running when the drain gives up can land after session_end on the
        // wire. The drain logs a warning in that case so the anomaly is
        // observable.
        internal void End()
        {
            // Phase 1 — stop the timer and wait for any in-flight callback
            // OUTSIDE the lock. OnHeartbeat itself takes _lock; waiting under
            // the lock would deadlock. See DrainHeartbeatTimer for the wait
            // budget and the logging on timeout.
            DrainHeartbeatTimer();

            // Phase 2 — atomically capture the outgoing session fields and
            // reset state so subsequent Start (on extended-pause rollover) or
            // Dispose (on shutdown) sees a clean slate.
            string sessionId;
            long duration;
            lock (_lock)
            {
                if (_sessionId == null) return;
                sessionId = _sessionId!;

                // ComputeEngagedSecondsLocked folds the in-flight pause in
                // itself, so End on a still-paused session reports engaged
                // time that excludes the final pause window.
                duration = ComputeEngagedSecondsLocked();
                ResetSessionStateLocked();
            }

            // duration is engagement-aware: wall-clock since Start minus
            // every credited pause. Web SDK's session_end emits wall-clock
            // seconds with no pause concept (browsers have no foreground
            // pause). Same wire field, different semantic. Unity always
            // emits duration (minimum 0); Web SDK guards it behind
            // sessionStartTime, which in practice tracks sessionId and
            // is set/cleared together with it. Backend dashboards
            // comparing surfaces should not assume Unity and Web session
            // lengths are directly comparable.
            SafeTrack("session_end", new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["duration"] = duration
            });
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            // End handles the timer drain, the atomic state capture, and the
            // session_end emit. Dispose adds nothing beyond the _disposed
            // latch (which blocks subsequent Start/Pause/Resume calls).
            End();
        }

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        // Fires a single heartbeat. Called by the internal timer every 60s
        // and exposed as internal so tests can exercise the heartbeat path
        // without waiting the full interval. Skips emission while the
        // session is paused so "foregrounded-only" is what actually ships:
        // a backgrounded game must not dribble stable-duration heartbeats
        // into the pipeline for the entire alt-tab.
        internal void OnHeartbeat()
        {
            string sessionId;
            long duration;
            lock (_lock)
            {
                if (_disposed || _sessionId == null) return;
                // Quiesce while paused — session_heartbeat is an engagement
                // signal, and a paused session is by definition disengaged.
                // Resume resumes ticking; the timer keeps firing in the
                // background on thread-pool cadence but this method is the
                // gate that controls what reaches _track.
                if (_pausedAt.HasValue) return;
                // Non-null at this point — the early-return above ensures it.
                sessionId = _sessionId!;

                duration = ComputeEngagedSecondsLocked();
            }

            // Build and emit the heartbeat outside the lock so the performance
            // snapshot delegate and track callback cannot cause contention or
            // reentrant-lock surprises.
            var properties = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["duration"] = duration
            };

            var perf = SafePerformanceSnapshot();
            if (perf != null)
            {
                foreach (var kv in perf)
                {
                    // Core heartbeat fields are owned by Session; a provider
                    // that returns a "sessionId" or "duration" key (buggy or
                    // malicious) must not be able to rewrite them on the wire.
                    // Drop colliding keys rather than overwrite.
                    if (properties.ContainsKey(kv.Key)) continue;
                    properties[kv.Key] = kv.Value;
                }
            }

            SafeTrack("session_heartbeat", properties);
        }

        // Invokes _track with a catch-all so a throwing callback cannot
        // escape. OnHeartbeat runs on a thread-pool timer callback where an
        // unhandled exception is swallowed by the runtime on .NET Framework
        // / Mono but can terminate the process on .NET 5+. Start and End run
        // on the caller's thread (typically Unity main) where an exception
        // from _track would bubble into Init / Shutdown / SetConsent. Same
        // guard covers both paths.
        private void SafeTrack(string eventName, Dictionary<string, object> properties)
        {
            try
            {
                _track(eventName, properties);
            }
            catch (Exception ex)
            {
                Log.Warn($"Session: {eventName} track callback threw {ex.GetType().Name}: {ex.Message}. Event dropped.");
            }
        }

        // Invokes the performance snapshot provider with a catch-all. The
        // provider is studio-supplied (PerformanceSnapshotProvider) and
        // crosses an API boundary; without this guard a throwing provider
        // propagates into the heartbeat timer callback.
        private Dictionary<string, object>? SafePerformanceSnapshot()
        {
            if (_performanceSnapshot == null) return null;
            try
            {
                return _performanceSnapshot();
            }
            catch (Exception ex)
            {
                Log.Warn($"Session: performance snapshot threw {ex.GetType().Name}: {ex.Message}. Heartbeat ships without performance fields.");
                return null;
            }
        }

        // Stops the heartbeat timer and waits for any in-flight callback to
        // complete. Runs OUTSIDE _lock because OnHeartbeat takes _lock itself
        // and waiting under the lock would deadlock. Idempotent: safe to call
        // repeatedly. The wait budget is 1 second because Dispose can run on
        // Application.quitting (Unity main thread) and a frozen quit is worse
        // than a dropped trailing heartbeat. Logs a warning on timeout so
        // stuck-heartbeat anomalies surface when Debug logging is on.
        private void DrainHeartbeatTimer()
        {
            Timer? timer;
            lock (_lock)
            {
                timer = _heartbeatTimer;
                _heartbeatTimer = null;
            }
            if (timer == null) return;

            using var waited = new ManualResetEvent(false);
            try
            {
                // Timer.Dispose(WaitHandle) returns false when the timer was
                // already disposed — in that case the WaitHandle is NOT
                // signaled, so WaitOne would time out and fire a spurious
                // "drain timeout" warning. Skip the wait (and the warning)
                // when the timer reports it is already gone.
                if (!timer.Dispose(waited))
                    return;

                if (!waited.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    Log.Warn("Session: heartbeat callback did not complete within 1s on timer stop. " +
                             "A trailing session_heartbeat may race with the next session lifecycle event.");
                }
            }
            catch (ObjectDisposedException)
            {
                // Timer already disposed — nothing to wait for.
            }
        }

        // Caller must hold _lock. Engagement time in seconds: wall-clock
        // since Start, minus every credited pause (both the committed
        // _accumulatedPause and any in-flight window since _pausedAt),
        // rounded to a whole second to match Web SDK's Math.round(... /
        // 1000). Folding the live pause in here rather than at each call
        // site keeps callers from racing the pause state against their
        // own local copy — End and OnHeartbeat both run under _lock, so
        // the read is consistent with ResetSessionStateLocked / Pause /
        // Resume. Clamped to zero so a wall-clock rewind (NTP correction,
        // manual change) can never produce a negative duration on the wire.
        private long ComputeEngagedSecondsLocked()
        {
            var now = _getUtcNow();
            var livePause = _pausedAt.HasValue ? now - _pausedAt.Value : TimeSpan.Zero;
            var engagedSeconds = ((now - _sessionStart) - _accumulatedPause - livePause).TotalSeconds;
            if (engagedSeconds < 0) return 0;
            return (long)Math.Round(engagedSeconds, MidpointRounding.AwayFromZero);
        }

        // Caller must hold _lock. Clears per-session state after End so
        // the Session falls back to an unstarted shape (SessionId == null,
        // no pause accumulator). End is the only caller; Start inlines
        // the same three assignments because it simultaneously writes
        // live values (a new Guid, _getUtcNow()) that cannot be expressed
        // by a "reset" helper. Adding a new state field requires updating
        // both Start and this helper.
        private void ResetSessionStateLocked()
        {
            _sessionId = null;
            _pausedAt = null;
            _accumulatedPause = TimeSpan.Zero;
        }
    }
}
