#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Immutable.Audience
{
    // Entry point for the Immutable Audience SDK.
    public static class ImmutableAudience
    {
        // Reference fields are written inside _initLock; readers check the
        // `volatile _initialized` flag first so they never see a half-initialised state.
        // _consent and _session are written only inside _initLock but read outside,
        // so they stay `volatile` to make writes visible across threads.
        // _userId is written outside the lock (Identify, Reset) — `volatile` for the same reason.
        private static AudienceConfig? _config;
        private static DiskStore? _store;
        private static EventQueue? _queue;
        private static HttpTransport? _transport;
        private static HttpClient? _controlClient;
        private static CancellationTokenSource? _shutdownCancellationSource;
        private static Timer? _sendTimer;
        private static volatile ConsentLevel _consent;
        private static volatile string? _userId;
        private static volatile bool _initialized;
        private static readonly object _initLock = new object();

        // Gate against overlapping timer ticks (Timer callbacks run on independent ThreadPool threads).
        private static int _sendInFlight;

        // AudienceUnityHooks sets these at SubsystemRegistration.
        // DefaultPersistentDataPathProvider fills PersistentDataPath from
        // Application.persistentDataPath. LaunchContextProvider supplies
        // Unity context for game_launch without Core referencing UnityEngine.
        internal static Func<string>? DefaultPersistentDataPathProvider;
        internal static Func<Dictionary<string, object>>? LaunchContextProvider;

        // Active session. Created at Init (or on upgrade from None) and disposed
        // on Shutdown or SetConsent(None). Volatile so OnPause/OnResume see
        // assignments from SetConsent without taking _initLock.
        private static volatile Session? _session;

        // Starts the SDK. Call once at launch.
        public static void Init(AudienceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.PublishableKey))
                throw new ArgumentException("PublishableKey is required", nameof(config));

            if (string.IsNullOrEmpty(config.PersistentDataPath))
                config.PersistentDataPath = DefaultPersistentDataPathProvider?.Invoke();
            if (string.IsNullOrEmpty(config.PersistentDataPath))
                throw new ArgumentException("PersistentDataPath is required", nameof(config));

            ConsentLevel consentAtInit;
            Session? sessionToStart;
            lock (_initLock)
            {
                if (_initialized)
                {
                    Log.Warn("Init called more than once — ignoring; original config retained. " +
                             "Call Shutdown() first if reconfiguring is intended.");
                    return;
                }

                _config = config;
                Log.Enabled = config.Debug;
                // Persisted consent overrides the config default (prior downgrade survives restart).
                _consent = ConsentStore.Load(config.PersistentDataPath) ?? config.Consent;

                _store = new DiskStore(config.PersistentDataPath);
                _queue = new EventQueue(_store, config.FlushIntervalSeconds, config.FlushSize);
                _transport = new HttpTransport(_store, config.PublishableKey, config.OnError, config.HttpHandler);
                _controlClient = config.HttpHandler != null
                    ? new HttpClient(config.HttpHandler, disposeHandler: false)
                    : new HttpClient();
                _controlClient.Timeout = TimeSpan.FromSeconds(Constants.ControlPlaneRequestTimeoutSeconds);
                _shutdownCancellationSource = new CancellationTokenSource();

                // Disk → network timer. EventQueue owns the separate memory → disk drain.
                var sendIntervalMs = Math.Max(1, config.FlushIntervalSeconds) * 1000;
                _sendTimer = new Timer(_ => SendBatch(), null, sendIntervalMs, sendIntervalMs);

                _initialized = true;

                // Snapshot so a racing SetConsent(None) can't drop the launch event.
                consentAtInit = _consent;

                // Session created under the lock; Start() deferred until after
                // release because session_start → Track takes its own locks.
                if (consentAtInit.CanTrack())
                    _session = new Session(Track);

                // Captured reference: a later SetConsent(None) may dispose this
                // Session (Start then no-ops on _disposed). Either way no duplicate
                // session_start and no post-revocation leak.
                sessionToStart = _session;
            }

            // session_start fires before game_launch so the wire stream
            // shows the new sessionId ahead of the launch event.
            sessionToStart?.Start();

            FireGameLaunch(config, consentAtInit);
        }

        // Pause/Resume hooks for the Unity lifecycle bridge.
        // Internal; reached via InternalsVisibleTo from the Unity assembly.
        internal static void OnPause()
        {
            if (!_initialized) return;
            _session?.Pause();
        }

        internal static void OnResume()
        {
            if (!_initialized) return;
            _session?.Resume();
        }

        // -----------------------------------------------------------------
        // Track
        // -----------------------------------------------------------------

        // Sends a typed event. Prefer this over the string overload —
        // IEvent implementations validate required fields at compile time.
        public static void Track(IEvent evt)
        {
            if (!CanTrack()) return;
            if (evt == null)
            {
                Log.Warn("Track(IEvent) called with null event — dropping.");
                return;
            }

            var config = _config;
            if (config == null) return;

            // Consumer-supplied impl; catch so a buggy IEvent cannot crash the game.
            string eventName;
            Dictionary<string, object> properties;
            try
            {
                eventName = evt.EventName;
                properties = evt.ToProperties();
            }
            catch (Exception ex)
            {
                Log.Warn($"Track(IEvent) — {evt.GetType().Name}.ToProperties()/EventName threw {ex.GetType().Name}: {ex.Message}. Dropping.");
                return;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                Log.Warn($"Track(IEvent) — {evt.GetType().Name}.EventName returned null or empty. Dropping.");
                return;
            }

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, _consent);
            // ToProperties returns a fresh dict per call, so no snapshot needed.
            var msg = MessageBuilder.Track(eventName, anonymousId, _userId, config.PackageVersion, properties);
            Enqueue(msg);
        }

        // Sends a custom event. For predefined names (purchase, progression,
        // resource, milestone_reached), prefer the typed overload which
        // validates required fields.
        public static void Track(string eventName, Dictionary<string, object>? properties = null)
        {
            if (!CanTrack()) return;
            if (string.IsNullOrEmpty(eventName))
            {
                Log.Warn("Track(string) called with null or empty event name — dropping.");
                return;
            }

            var config = _config;
            if (config == null) return;

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, _consent);
            var msg = MessageBuilder.Track(eventName, anonymousId, _userId, config.PackageVersion,
                SnapshotCallerDict(properties));
            Enqueue(msg);
        }

        // -----------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------

        // Attaches a known user id to subsequent events.
        public static void Identify(string userId, IdentityType identityType, Dictionary<string, object>? traits = null) =>
            Identify(userId, identityType.ToLowercaseString(), traits);

        // String overload for providers outside the IdentityType enum.
        // identityType is required — data-deletion matches events by this namespace.
        public static void Identify(string userId, string identityType, Dictionary<string, object>? traits = null)
        {
            if (!_initialized) return;

            // Validate inputs before consent so null-arg callers get the right warning.
            if (string.IsNullOrEmpty(userId))
            {
                Log.Warn("Identify called with null or empty userId — dropping.");
                return;
            }
            if (!_consent.CanIdentify())
            {
                Log.Warn($"Identify discarded — requires Full consent, current is {_consent}");
                return;
            }

            var config = _config;
            if (config == null) return;

            _userId = userId;

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, _consent);
            var msg = MessageBuilder.Identify(anonymousId, userId, identityType, config.PackageVersion,
                SnapshotCallerDict(traits));
            Enqueue(msg);
        }

        // Links two user ids for the same player.
        public static void Alias(string fromId, IdentityType fromType, string toId, IdentityType toType) =>
            Alias(fromId, fromType.ToLowercaseString(), toId, toType.ToLowercaseString());

        // String overload for providers outside the IdentityType enum.
        // from/toType are required — data-deletion matches by these namespaces.
        public static void Alias(string fromId, string fromType, string toId, string toType)
        {
            if (!_initialized) return;

            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                Log.Warn("Alias called with null or empty fromId/toId — dropping.");
                return;
            }
            if (!_consent.CanIdentify())
            {
                Log.Warn($"Alias discarded — requires Full consent, current is {_consent}");
                return;
            }

            var config = _config;
            if (config == null) return;

            var msg = MessageBuilder.Alias(fromId, fromType, toId, toType, config.PackageVersion);
            Enqueue(msg);
        }

        // Logs out the current player. Clears userId, discards queued events,
        // mints a fresh anonymousId, and starts a new session. Matches Web SDK
        // reset(): no session_end is emitted for the old session (it is enqueued
        // and then purged). Call FlushAsync() first to preserve queued events.
        public static void Reset()
        {
            // Phase 1 under _initLock: swap _session and reset identity. Blocking
            // work (session drain, disk purge, new session_start) runs outside
            // the lock so callers racing on _initLock don't wait on it.
            Session? oldSession;
            Session? newSession = null;
            EventQueue? queueForPurge;

            lock (_initLock)
            {
                if (!_initialized) return;
                var config = _config;
                if (config == null) return;

                oldSession = _session;
                queueForPurge = _queue;

                Identity.Reset(config.PersistentDataPath!);
                _userId = null;

                // Swap under the lock so racing SetConsent/OnPause/OnResume see
                // either the old, the new, or null — never a torn reference.
                _session = _consent.CanTrack() ? new Session(Track) : null;
                newSession = _session;
            }

            // Phase 2 outside _initLock:
            // Dispose enqueues session_end → PurgeAll wipes it → Start emits the
            // new session_start. Order matches the in-lock sequence this replaces.
            oldSession?.Dispose();
            queueForPurge?.PurgeAll();
            newSession?.Start();
        }

        // Asks the backend to erase this player's data. Await for ack, or discard for fire-and-forget.
        public static Task DeleteData(string? userId = null)
        {
            if (!_initialized) return Task.CompletedTask;

            var config = _config;
            var client = _controlClient;
            if (config == null || client == null) return Task.CompletedTask;

            string query;
            if (!string.IsNullOrEmpty(userId))
            {
                query = "userId=" + Uri.EscapeDataString(userId);
            }
            else
            {
                // Get (not GetOrCreate): a fresh install must not register an id just to delete it.
                var anonymousId = Identity.Get(config.PersistentDataPath!);
                if (string.IsNullOrEmpty(anonymousId))
                    return Task.CompletedTask;
                query = "anonymousId=" + Uri.EscapeDataString(anonymousId);
            }

            var url = Constants.DataUrl(config.PublishableKey) + "?" + query;
            var onError = config.OnError;
            var publishableKey = config.PublishableKey;
            var cancellationToken = _shutdownCancellationSource?.Token ?? CancellationToken.None;

            return Task.Run(async () =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                    request.Headers.Add(Constants.PublishableKeyHeader, publishableKey);
                    using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        NotifyErrorCallback(onError, AudienceErrorCode.NetworkError,
                            $"Data delete failed with status {(int)response.StatusCode}");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Shutdown cancelled — caller is tearing down, no error fired.
                }
                catch (Exception ex)
                {
                    NotifyErrorCallback(onError, AudienceErrorCode.NetworkError,
                        $"Data delete threw: {ex.Message}");
                }
            });
        }

        private static void NotifyErrorCallback(Action<AudienceError>? onError, AudienceErrorCode code, string message)
        {
            if (onError == null) return;
            try
            {
                onError(new AudienceError(code, message));
            }
            catch
            {
                // Swallow: a buggy OnError must not crash the SDK.
            }
        }

        // -----------------------------------------------------------------
        // Consent
        // -----------------------------------------------------------------

        // Changes the player's consent level.
        public static void SetConsent(ConsentLevel level)
        {
            if (!_initialized) return;

            // Serialised under _initLock: prevents concurrent upgrades from each
            // building a fresh Session (stranding timers), and prevents racing
            // Init's _session assignment.
            Session? sessionToStart = null;
            lock (_initLock)
            {
                if (!_initialized) return;

                var config = _config;
                var queue = _queue;
                if (config == null) return;

                var previous = _consent;
                if (level == previous) return;

                // Snapshot anonymousId before Identity.Reset (on None) wipes it.
                // The PUT audit trail needs to record whose consent changed.
                var anonymousIdForPut = previous == ConsentLevel.None
                    ? Identity.GetOrCreate(config.PersistentDataPath!, level)
                    : Identity.Get(config.PersistentDataPath!);

                _consent = level;

                try
                {
                    // PersistentDataPath validated non-null in Init; compiler can't propagate that.
                    ConsentStore.Save(config.PersistentDataPath!, level);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Log.Warn($"SetConsent — failed to persist consent level: {ex.GetType().Name}: {ex.Message}. " +
                             "In-memory level is updated but will revert on next launch.");
                    NotifyErrorCallback(config.OnError, AudienceErrorCode.ConsentPersistFailed,
                        $"Consent persist failed: {ex.Message}");
                }

                if (level == ConsentLevel.None)
                {
                    // Dispose for timer cleanup. session_end is gated out by
                    // CanTrack (post-flip), matching revocation semantics.
                    _session?.Dispose();
                    _session = null;

                    queue?.PurgeAll();
                    Identity.Reset(config.PersistentDataPath!);
                }
                else if (previous == ConsentLevel.Full && level == ConsentLevel.Anonymous)
                {
                    _userId = null;
                    queue?.ApplyAnonymousDowngrade();
                }
                else if (previous == ConsentLevel.None && _session == null)
                {
                    // Upgrade from None: previous session was disposed. Start a
                    // fresh one. Start() deferred to outside the lock.
                    _session = new Session(Track);
                    sessionToStart = _session;
                }

                SyncConsentToBackend(config, level, anonymousIdForPut);
            }

            sessionToStart?.Start();
        }

        // Fire-and-forget PUT /v1/audience/tracking-consent.
        private static void SyncConsentToBackend(AudienceConfig config, ConsentLevel level, string? anonymousId)
        {
            var client = _controlClient;
            if (client == null) return;

            var url = Constants.ConsentUrl(config.PublishableKey);
            var publishableKey = config.PublishableKey;
            var onError = config.OnError;
            var cancellationToken = _shutdownCancellationSource?.Token ?? CancellationToken.None;

            var body = Json.Serialize(new Dictionary<string, object>
            {
                ["status"] = level.ToLowercaseString(),
                ["source"] = Constants.ConsentSource,
                // Explicit null lets the backend distinguish "unknown" from a missing field.
                ["anonymousId"] = anonymousId!,
            });

            Task.Run(async () =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Put, url);
                    request.Headers.Add(Constants.PublishableKeyHeader, publishableKey);
                    request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        NotifyErrorCallback(onError, AudienceErrorCode.ConsentSyncFailed,
                            $"Consent sync failed with status {(int)response.StatusCode}");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Shutdown cancelled.
                }
                catch (Exception ex)
                {
                    NotifyErrorCallback(onError, AudienceErrorCode.ConsentSyncFailed,
                        $"Consent sync threw: {ex.Message}");
                }
            });
        }

        // -----------------------------------------------------------------
        // Flush / Shutdown
        // -----------------------------------------------------------------

        // Sends all pending events now.
        public static async Task FlushAsync()
        {
            if (!_initialized) return;

            var queue = _queue;
            var transport = _transport;
            if (queue == null || transport == null) return;

            queue.FlushSync();

            while (!transport.IsInBackoffWindow &&
                   await transport.SendBatchAsync().ConfigureAwait(false))
            {
            }
        }

        // Flushes and stops the SDK.
        public static void Shutdown()
        {
            // Phase 1 under _initLock: flip _initialized and capture references.
            // Other callers racing on _initLock re-check _initialized once they
            // acquire and early-return, so they don't wait on Phase 2's drain /
            // flush / dispose budget (up to ~10s worst case).
            Session? session;
            Timer? timer;
            EventQueue? queue;
            HttpTransport? transport;
            HttpClient? controlClient;
            CancellationTokenSource? cts;
            int timeoutMs;

            lock (_initLock)
            {
                if (!_initialized) return;

                // Flip the gate first. Init / SetConsent / Reset acquiring after
                // this see _initialized == false and return cleanly.
                _initialized = false;

                session       = _session;                    _session = null;
                timer         = _sendTimer;                  _sendTimer = null;
                queue         = _queue;                      _queue = null;
                transport     = _transport;                  _transport = null;
                controlClient = _controlClient;              _controlClient = null;
                cts           = _shutdownCancellationSource; _shutdownCancellationSource = null;

                timeoutMs = _config?.ShutdownFlushTimeoutMs ?? 2_000;

                // Drop Identity's in-memory cache so a later Init with a different
                // persistentDataPath reads the new file, not the stale cached id.
                Identity.ClearCache();

                _config = null;
                _store = null;
                _userId = null;
            }

            // Phase 2 outside _initLock: end session, drain timers, flush, dispose.

            // End session first so session_end hits the queue before the final flush.
            session?.Dispose();

            // Drain in-flight timer callbacks before disposing dependents.
            // Parameterless Timer.Dispose would return immediately and race SendBatch.
            if (timer != null)
            {
                using var disposed = new ManualResetEvent(false);
                if (timer.Dispose(disposed))
                {
                    disposed.WaitOne(TimeSpan.FromSeconds(2));
                }
            }

            // Clear the gate in case WaitOne timed out with SendBatch still running
            // — a later Init would otherwise be stranded at 1.
            Interlocked.Exchange(ref _sendInFlight, 0);

            queue?.Shutdown();

            // Best-effort final send, capped so a slow network can't hang quit.
            if (transport != null)
            {
                try
                {
                    var send = transport.SendBatchAsync();
                    if (!send.Wait(timeoutMs))
                    {
                        Log.Warn($"Shutdown flush exceeded {timeoutMs}ms — abandoning. " +
                                 "Queued events remain on disk and will retry on next startup.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Shutdown flush threw: {ex.GetType().Name}: {ex.Message}");
                }
            }

            // Cancel in-flight control-plane requests before disposing the client
            // so awaiters see OperationCanceledException, not ObjectDisposedException.
            cts?.Cancel();

            transport?.Dispose();
            queue?.Dispose();
            controlClient?.Dispose();
            cts?.Dispose();
        }

        // -----------------------------------------------------------------
        // Internal — shared with tests and AudienceUnityHooks
        // -----------------------------------------------------------------

        // Shuts down (if initialised) and clears per-session state. Used on
        // test teardown and Unity SubsystemRegistration to survive "disable
        // domain reload". LaunchContextProvider is re-assigned by AudienceUnityHooks.
        internal static void ResetState()
        {
            // Shutdown manages its own serialisation and releases _initLock before
            // its Phase 2 teardown, so calling it here does not strand waiters.
            Shutdown();

            lock (_initLock)
            {
                _consent = ConsentLevel.None;
                // Defensive: Shutdown nulls _session too, but a future refactor
                // that bails before that null must not leak a stale Session.
                _session = null;
                Identity.ClearCache();
            }
        }

        internal static ConsentLevel CurrentConsentForTesting => _consent;

        internal static void FlushQueueToDiskForTesting() => _queue?.FlushSync();

        // Drives SendBatch without a real timer so the overlapping-tick guard is testable.
        internal static void SendBatchForTesting() => SendBatch();

        // Drives a single heartbeat so lifecycle tests don't wait the 60s cadence.
        internal static void InvokeSessionHeartbeatForTesting() => _session?.OnHeartbeat();

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        private static bool CanTrack()
        {
            return _initialized && _consent.CanTrack();
        }

        // Copy the dictionary so the caller editing it later can't corrupt the
        // message while the background thread is writing it to disk.
        private static Dictionary<string, object>? SnapshotCallerDict(Dictionary<string, object>? src) =>
            src != null ? new Dictionary<string, object>(src) : null;

        private static void Enqueue(Dictionary<string, object>? msg)
        {
            var queue = _queue;
            if (queue == null) return;

            // Re-check consent inside _drainLock so a racing SetConsent(None) can't leak past the purge.
            queue.EnqueueChecked(msg, () => _consent.CanTrack());
        }

        private static void SendBatch()
        {
            // If a previous send is still running, skip this one. That send
            // will reschedule the next tick when it finishes.
            if (Interlocked.CompareExchange(ref _sendInFlight, 1, 0) != 0)
                return;

            try
            {
                var transport = _transport;
                if (transport == null) return;

                if (!transport.IsInBackoffWindow)
                {
                    try
                    {
                        transport.SendBatchAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        // Timer-thread callback; no caller above to catch.
                        Log.Warn($"SendBatch unexpected exception: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                RescheduleSendTimer(transport);
            }
            finally
            {
                Interlocked.Exchange(ref _sendInFlight, 0);
            }
        }

        // Realigns the timer to NextAttemptAt so backoff windows aren't repolled.
        private static void RescheduleSendTimer(HttpTransport transport)
        {
            var timer = _sendTimer;
            var config = _config;
            if (timer == null || config == null || transport == null) return;

            var sendIntervalMs = Math.Max(1, config.FlushIntervalSeconds) * 1000;
            var nextMs = sendIntervalMs;

            if (transport.NextAttemptAt is DateTime scheduled)
            {
                var delayMs = (scheduled - DateTime.UtcNow).TotalMilliseconds;
                if (delayMs > sendIntervalMs)
                    nextMs = (int)Math.Min(int.MaxValue, delayMs);
            }

            timer.Change(nextMs, sendIntervalMs);
        }

        // consentAtInit only gates the launch; Track still checks live _consent via CanTrack.
        private static void FireGameLaunch(AudienceConfig config, ConsentLevel consentAtInit)
        {
            if (!consentAtInit.CanTrack()) return;

            var properties = new Dictionary<string, object>();

            // Unity auto-detected context (platform, version, buildGuid, unityVersion).
            // Core stays pure C#; Unity layer fills via LaunchContextProvider.
            var provider = LaunchContextProvider;
            if (provider != null)
            {
                Dictionary<string, object>? unityContext = null;
                try { unityContext = provider(); }
                catch (Exception ex)
                {
                    Log.Warn($"LaunchContextProvider threw {ex.GetType().Name}: {ex.Message}. " +
                             "game_launch will ship without auto-detected Unity context.");
                }

                if (unityContext != null)
                {
                    foreach (var kvp in unityContext)
                        properties[kvp.Key] = kvp.Value;
                }
            }

            // Config-supplied distributionPlatform overrides the provider value.
            if (config.DistributionPlatform != null)
                properties["distributionPlatform"] = config.DistributionPlatform;

            // No sessionId on game_launch per Event Reference. Pipeline correlates
            // via eventTimestamp with the session_start that fires just before.
            Track("game_launch", properties.Count > 0 ? properties : null);
        }
    }
}
