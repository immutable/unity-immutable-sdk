#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Immutable.Audience
{
    // Fires a session event (session_start / session_heartbeat / session_end)
    // through ImmutableAudience.Track. Declared as a named delegate so Session
    // can be driven by tests with a mock without touching the static SDK surface.
    internal delegate void TrackDelegate(string eventName, Dictionary<string, object> properties);

    // Unity session lifecycle. Emits session_start / session_heartbeat / session_end.
    // duration is engagement time (excludes pause). Heartbeat fires off-thread;
    // public methods run on the caller's thread. _track fires outside _lock.
    //
    // Serialisation: Start / End / Dispose are NOT reentrant-safe from multiple
    // threads. Callers serialise them (ImmutableAudience holds _initLock across
    // Init / SetConsent / Shutdown / Reset, which are the only public entry points
    // that touch a Session). Pause / Resume / OnHeartbeat are thread-safe.
    internal sealed class Session : IDisposable
    {
        internal const int HeartbeatIntervalMs = 60_000;

        // 30s: alt-tab beyond this rolls the session on Resume.
        internal const int PauseTimeoutMs = 30_000;

        private readonly TrackDelegate _track;
        private readonly Func<Dictionary<string, object>>? _performanceSnapshot;
        private readonly Func<DateTime> _getUtcNow;
        private readonly int _heartbeatIntervalMs;
        private readonly object _lock = new object();

        private Timer? _heartbeatTimer;
        private string? _sessionId;
        private DateTime _sessionStart;
        private DateTime? _pausedAt;
        // Subtracted from wall-clock so duration reflects engagement.
        private TimeSpan _accumulatedPause;
        private bool _disposed;

        // Current session ID. Null before Start() is called and after End()/Dispose().
        internal string? SessionId
        {
            get { lock (_lock) return _sessionId; }
        }

        // track: fires session events. performanceSnapshot: merges fps/memory
        // into heartbeats (null on non-Unity). getUtcNow/heartbeatIntervalMs: test seams.
        internal Session(
            TrackDelegate track,
            Func<Dictionary<string, object>>? performanceSnapshot = null,
            Func<DateTime>? getUtcNow = null,
            int heartbeatIntervalMs = HeartbeatIntervalMs)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _performanceSnapshot = performanceSnapshot;
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
            _heartbeatIntervalMs = heartbeatIntervalMs;
        }

        // Starts a session. Fires session_start and arms the heartbeat timer.
        internal void Start()
        {
            // Phase 1: drain old timer outside _lock (callback re-enters _lock).
            // Old state left intact so a trailing callback emits for the old session
            // — wire-ordered before the new session_start.
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
                    // 500ms budget (double-Start is a misuse path).
                    if (oldTimer.Dispose(waited))
                        waited.WaitOne(TimeSpan.FromMilliseconds(500));
                }
                catch (ObjectDisposedException)
                {
                }
            }

            // Phase 2: populate new state. Re-check _disposed (may have flipped during drain).
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

        // Pause on focus-loss. Quiesces heartbeat; 30s threshold evaluated on next Resume.
        internal void Pause()
        {
            lock (_lock)
            {
                if (_disposed || _sessionId == null) return;
                // Keep the original anchor; double-Pause would shift it forward
                // and under-count the pause window.
                if (_pausedAt.HasValue) return;
                _pausedAt = _getUtcNow();
            }
        }

        // Resume on focus-gain. Pause >30s rolls the session (End + Start).
        internal void Resume()
        {
            bool extended;
            lock (_lock)
            {
                if (_disposed || _sessionId == null || _pausedAt == null) return;

                var pauseDuration = _getUtcNow() - _pausedAt.Value;
                _pausedAt = null;

                // Clamp: wall-clock rewind (NTP) would otherwise over-credit engagement.
                if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;

                extended = pauseDuration.TotalMilliseconds > PauseTimeoutMs;

                // Credit in both paths. End (and then Start) reset the accumulator
                // on the extended-pause rollover so there is no double-count.
                _accumulatedPause += pauseDuration;
            }

            if (extended)
            {
                // Extended pause: roll the session. End/Start fire _track outside _lock.
                // Between End and Start other public methods early-return on _sessionId=null.
                End();
                Start();
            }
        }

        // Ends the session. Drains heartbeat before emitting session_end so wire
        // order holds (drain timeout is best-effort; logs a warning on timeout).
        internal void End()
        {
            // Phase 1: drain outside _lock (OnHeartbeat re-enters _lock).
            DrainHeartbeatTimer();

            // Phase 2: capture fields and reset so subsequent Start/Dispose sees clean state.
            string sessionId;
            long duration;
            lock (_lock)
            {
                if (_sessionId == null) return;
                sessionId = _sessionId!;

                // ComputeEngagedSecondsLocked folds in the live pause.
                duration = ComputeEngagedSecondsLocked();
                ResetSessionStateLocked();
            }

            // duration is engagement-aware (excludes pause). Web SDK emits
            // wall-clock; dashboards should not assume parity.
            SafeTrack("session_end", new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["durationSec"] = duration
            });
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }

            // End does the drain + emit. Dispose adds the _disposed latch
            // which blocks subsequent Start/Pause/Resume.
            End();
        }

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        // Fires a heartbeat. Internal so tests can drive without waiting 60s.
        // Skips while paused so backgrounded games don't dribble heartbeats.
        internal void OnHeartbeat()
        {
            string sessionId;
            long duration;
            lock (_lock)
            {
                if (_disposed || _sessionId == null) return;
                // Paused sessions don't ship heartbeats (timer still fires; this gates _track).
                if (_pausedAt.HasValue) return;
                sessionId = _sessionId!;

                duration = ComputeEngagedSecondsLocked();
            }

            // Build outside _lock so snapshot + track don't re-enter.
            var properties = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["durationSec"] = duration
            };

            var perf = SafePerformanceSnapshot();
            if (perf != null)
            {
                foreach (var kv in perf)
                {
                    // Don't let the provider clobber core fields.
                    if (properties.ContainsKey(kv.Key)) continue;
                    properties[kv.Key] = kv.Value;
                }
            }

            SafeTrack("session_heartbeat", properties);
        }

        // Guards _track from escaping. Heartbeat is a Timer callback (process
        // kill on .NET 5+); Start/End are caller-thread (would bubble to Init/Shutdown).
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

        // Guards the studio-supplied snapshot from escaping to the timer thread.
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

        // Stops the timer and waits for the in-flight callback. Runs outside
        // _lock (OnHeartbeat re-enters). 1s budget (quits must not hang). Warns on timeout.
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
                // Dispose(wh)=false: already disposed, wh never signals — skip the wait.
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
            }
        }

        // Caller must hold _lock. Engagement seconds = wall-clock − accumulated − live pause.
        // Rounded to match Web SDK's Math.round. Clamped ≥0 for clock rewinds.
        private long ComputeEngagedSecondsLocked()
        {
            var now = _getUtcNow();
            var livePause = _pausedAt.HasValue ? now - _pausedAt.Value : TimeSpan.Zero;
            var engagedSeconds = ((now - _sessionStart) - _accumulatedPause - livePause).TotalSeconds;
            if (engagedSeconds < 0) return 0;
            return (long)Math.Round(engagedSeconds, MidpointRounding.AwayFromZero);
        }

        // Caller must hold _lock. Clears per-session state after End.
        // Start inlines equivalent assignments; new state fields must update both.
        private void ResetSessionStateLocked()
        {
            _sessionId = null;
            _pausedAt = null;
            _accumulatedPause = TimeSpan.Zero;
        }
    }
}
