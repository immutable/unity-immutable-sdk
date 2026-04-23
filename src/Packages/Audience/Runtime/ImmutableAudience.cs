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
        // Reference fields are written inside _initLock; readers fence off the volatile _initialized load.
        // _consent and _session are written only inside _initLock but read outside (Track's CanTrack,
        // OnPause/OnResume) so they stay volatile for release/acquire visibility.
        // _userId is written outside the lock (Identify, Reset) and needs volatile for the same reason.
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

        // Guard against overlapping timer ticks. System.Threading.Timer fires
        // callbacks on independent ThreadPool threads and does not serialise
        // them; without this gate, a slow SendBatchAsync (up to the HTTP
        // timeout) would stack on every interval tick, each tick holding its
        // own thread blocked on a pending request.
        private static int _sendInFlight;

        // AudienceUnityHooks sets this at SubsystemRegistration so Unity studios
        // can omit PersistentDataPath from AudienceConfig and Init will fill it
        // from Application.persistentDataPath. Non-Unity callers must still set
        // PersistentDataPath on the config.
        internal static Func<string>? DefaultPersistentDataPathProvider;

        // AudienceUnityHooks sets this so game_launch can auto-include
        // Unity context without the core referencing UnityEngine.
        internal static Func<Dictionary<string, object>>? LaunchContextProvider;

        // Session is created when consent allows — either at Init or on
        // upgrade from None — and is disposed on Shutdown or SetConsent(None).
        // Holds the current sessionId, fires session_start / session_heartbeat
        // / session_end via the Track callback, and handles pause/resume
        // rollover.
        //
        // Volatile + nullable because the field is read outside _initLock on
        // the Unity main thread (OnPause, OnResume) and written outside the
        // lock by SetConsent and Shutdown. volatile gives a release/acquire
        // fence so a freshly-assigned Session from SetConsent(None → *) is
        // visible to a subsequent OnPause on any thread.
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
                // Persisted consent overrides the config default so a prior runtime downgrade survives restart.
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

                // Snapshot under the lock so a racing SetConsent(None) can't drop the launch event.
                consentAtInit = _consent;

                // Create the session object under the lock so ResetState /
                // Shutdown have a consistent view. Start() is deferred
                // until outside the lock because Track() (which
                // session_start calls) acquires its own locks.
                if (consentAtInit.CanTrack())
                    _session = new Session(Track);

                // Captured under the lock so the Start() call below
                // operates on the Session this Init created, not a
                // replacement from a SetConsent that slips in after we
                // release _initLock. SetConsent itself takes _initLock so
                // it cannot land in the middle of this block, but between
                // the lock release and Start() a SetConsent(None) can
                // Dispose the captured Session (Start then early-returns
                // on _disposed, suppressing session_start) or a
                // SetConsent(None)+SetConsent(Anonymous) pair can swap
                // _session for a new one (we still Start the captured
                // original, which is disposed and no-ops; the upgrade path
                // Starts the replacement itself). Either way, no duplicate
                // session_start on the wire and no "events after consent
                // dropped" leak.
                sessionToStart = _session;
            }

            // session_start fires before game_launch so the wire stream
            // shows the new sessionId ahead of the launch event.
            sessionToStart?.Start();

            FireGameLaunch(config, consentAtInit);
        }

        // Notifies the session that the game was paused (alt-tab,
        // minimize, OS pause). If not resumed within 30 s the session
        // ends and a new one starts on resume. Called only from the
        // Unity lifecycle bridge, so it stays internal; the Unity
        // assembly reaches it via InternalsVisibleTo in AssemblyInfo.cs.
        internal static void OnPause()
        {
            if (!_initialized) return;
            _session?.Pause();
        }

        // Notifies the session that the game resumed. Called only from
        // the Unity lifecycle bridge; see OnPause for the visibility
        // rationale.
        internal static void OnResume()
        {
            if (!_initialized) return;
            _session?.Resume();
        }

        // -----------------------------------------------------------------
        // Track
        // -----------------------------------------------------------------

        // Send a typed event.
        //
        // Prefer this overload for predefined event names (e.g. purchase) — the
        // IEvent implementation enforces required fields and value types at
        // compile time. The string overload accepts any property shape and
        // cannot catch missing or mistyped fields.
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

        // Send a custom event.
        //
        // For predefined event names (e.g. purchase, progression, resource,
        // milestone_reached), prefer the typed overload —
        // Track(new Purchase { Currency = "USD", Value = 9.99m }) — which
        // validates required fields at send time. This overload accepts any
        // property shape and does not: Track("purchase", new Dictionary...)
        // that omits currency or value still enqueues and ships, but breaks
        // attribution and conversion reporting downstream because the
        // payload is missing the fields CDP needs to reconstruct the event.
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

        // Attach a known user id to subsequent events.
        public static void Identify(string userId, IdentityType identityType, Dictionary<string, object>? traits = null) =>
            Identify(userId, identityType.ToLowercaseString(), traits);

        // Attach a known user id to subsequent events. String overload for
        // providers not in IdentityType.
        //
        // identityType is required: data-deletion processing relies on it to
        // match identify events to the correct identity namespace, so an
        // event without one cannot be cleaned up.
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

        // Link two user ids for the same player.
        public static void Alias(string fromId, IdentityType fromType, string toId, IdentityType toType) =>
            Alias(fromId, fromType.ToLowercaseString(), toId, toType.ToLowercaseString());

        // Link two user ids for the same player. String overload for
        // providers not in IdentityType.
        //
        // fromType and toType are required: data-deletion processing uses
        // them to match alias events to the correct identity namespaces.
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

        // Log out the current player. Clears the user id, generates a fresh
        // anonymous id, and discards queued events (in-memory and on-disk)
        // so the next player on this device isn't attributed to the previous
        // one.
        //
        // To send queued events before they're discarded,
        // invoke await FlushAsync() first:
        //
        //     await ImmutableAudience.FlushAsync();
        //     ImmutableAudience.Reset();
        public static void Reset()
        {
            if (!_initialized) return;

            var config = _config;
            if (config == null) return;

            _userId = null;
            _queue?.PurgeAll();
            Identity.Reset(config.PersistentDataPath!);
        }

        // Ask the backend to erase this player's data. Returns a task the
        // caller can await to know when the request is acknowledged, or
        // discard for fire-and-forget.
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
                // Get, not GetOrCreate — a brand-new install must not register an ID just to delete it.
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
                    // Shutdown cancelled the request — no error fired; caller is tearing down.
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
                // Swallow: a buggy OnError must not crash the SDK surface.
            }
        }

        // -----------------------------------------------------------------
        // Consent
        // -----------------------------------------------------------------

        // Change the player's consent level.
        public static void SetConsent(ConsentLevel level)
        {
            if (!_initialized) return;

            // Serialize the whole transition under _initLock:
            //   - Two concurrent SetConsent calls that both see previous=None
            //     would otherwise both take the upgrade branch and each build
            //     a fresh Session, stranding one timer. The lock forces them
            //     to observe each other.
            //   - A SetConsent landing in the narrow window between Init's
            //     _initialized = true and its _session = new Session(...)
            //     assignment would otherwise see _session = null, skip the
            //     Dispose path, and let Init finish creating a Session whose
            //     timer never gets disposed. Under the lock, SetConsent can
            //     only enter after Init has fully released it, so _session
            //     reflects Init's final assignment.
            Session? sessionToStart = null;
            lock (_initLock)
            {
                if (!_initialized) return;

                var config = _config;
                var queue = _queue;
                if (config == null) return;

                var previous = _consent;
                if (level == previous) return;

                // Snapshot the anonymousId BEFORE Identity.Reset (on downgrade to
                // None) wipes the file. The PUT audit trail needs it to record
                // whose consent changed.
                var anonymousIdForPut = previous == ConsentLevel.None
                    ? Identity.GetOrCreate(config.PersistentDataPath!, level)
                    : Identity.Get(config.PersistentDataPath!);

                _consent = level;

                try
                {
                    // PersistentDataPath is validated non-null in Init; compiler can't propagate that.
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
                    // Dispose the session for heartbeat-timer cleanup.
                    // session_end is intentionally NOT emitted: Session.Dispose
                    // calls back into Track("session_end", ...) which sees
                    // _consent == None (set above) and drops the event via
                    // CanTrack. That matches revocation semantics — no events
                    // should leave the device after consent is None — and the
                    // queue purge below clears anything on disk anyway.
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
                    // Upgrade from None: the previous session was disposed on
                    // the prior downgrade. Start a fresh one so
                    // session_heartbeat and session_end can resume. Start()
                    // is deferred until outside the lock because Track()
                    // (which session_start calls) acquires its own locks.
                    _session = new Session(Track);
                    sessionToStart = _session;
                }

                SyncConsentToBackend(config, level, anonymousIdForPut);
            }

            sessionToStart?.Start();
        }

        // Fire-and-forget PUT /v1/audience/tracking-consent. Failures do not
        // block or surface; the local consent change has already applied.
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
                // Json.Serialize emits null → "anonymousId": null. Preserves the backend's ability to distinguish "unknown" from a missing field.
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
                    // Shutdown cancelled the request — no error fired.
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

        // Send pending events now.
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

        // Flush and stop the SDK.
        public static void Shutdown()
        {
            // Serialize with Init and SetConsent under the same lock. Without
            // this, Shutdown racing a concurrent Init could observe
            // _initialized = true (volatile set by Init) and proceed to tear
            // down fields Init is still in the middle of assigning, or Init
            // could finish assigning fields that Shutdown already disposed.
            // The lock also pins _controlClient / _shutdownCancellationSource
            // against a SetConsent whose SyncConsentToBackend Task closure
            // captured them just before Shutdown disposed them.
            lock (_initLock)
            {
                if (!_initialized) return;

                // End the session first so session_end hits the queue before
                // the final flush.
                _session?.Dispose();
                _session = null;

                // Drain in-flight timer callbacks before disposing dependents.
                // Parameterless Timer.Dispose returns immediately and would race SendBatch.
                var timer = _sendTimer;
                if (timer != null)
                {
                    using var disposed = new ManualResetEvent(false);
                    if (timer.Dispose(disposed))
                    {
                        disposed.WaitOne(TimeSpan.FromSeconds(2));
                    }
                    _sendTimer = null;
                }

                // Clear the in-flight guard in case the WaitOne above timed out
                // with a SendBatch callback still running: without this, a later
                // Init would leave _sendInFlight stranded at 1 and suppress every
                // tick of the new timer.
                Interlocked.Exchange(ref _sendInFlight, 0);

                _queue?.Shutdown();

                // Best-effort final send, capped so a slow network can't hang quit.
                if (_transport != null)
                {
                    var timeoutMs = _config?.ShutdownFlushTimeoutMs ?? 2_000;
                    try
                    {
                        var send = _transport.SendBatchAsync();
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

                // Cancel in-flight control-plane HTTP requests (DeleteData / SyncConsentToBackend)
                // before disposing the client so awaiting callers observe OperationCanceledException
                // rather than ObjectDisposedException.
                _shutdownCancellationSource?.Cancel();

                _transport?.Dispose();
                _queue?.Dispose();
                _controlClient?.Dispose();
                _shutdownCancellationSource?.Dispose();
                _shutdownCancellationSource = null;

                // Drop Identity's in-memory cache so a subsequent Init with a
                // different persistentDataPath reads the file from the new path
                // instead of returning the previous session's id.
                Identity.ClearCache();

                _initialized = false;
                _config = null;
                _store = null;
                _queue = null;
                _transport = null;
                _controlClient = null;
                _userId = null;
            }
        }

        // -----------------------------------------------------------------
        // Internal — shared with tests and AudienceUnityHooks
        // -----------------------------------------------------------------

        // Shuts down (if initialised) and clears per-session state so a
        // fresh Init starts clean. Used on test teardown and by Unity
        // SubsystemRegistration to survive "disable domain reload".
        // LaunchContextProvider is not cleared: AudienceUnityHooks
        // re-assigns it on the same SubsystemRegistration call.
        internal static void ResetState()
        {
            // Same lock as Shutdown/Init so a concurrent Init cannot repopulate
            // fields we are in the middle of clearing. Monitor is recursive,
            // so the inner Shutdown() re-enters cleanly on the same thread.
            lock (_initLock)
            {
                if (_initialized)
                    Shutdown();

                _consent = ConsentLevel.None;
                // Shutdown already nulls _session. Repeat here as a defensive
                // belt-and-braces step so a future Shutdown refactor that bails
                // before the null (early return on a new guard, reordering,
                // etc.) cannot leak a stale Session into the next Init cycle.
                _session = null;
                // Drop Identity's static cache so a subsequent Init with a different
                // persistentDataPath (tests, domain reload with changed config) reads
                // the file from the new path, not the previous session's cached id.
                Identity.ClearCache();
            }
        }

        internal static ConsentLevel CurrentConsentForTesting => _consent;

        internal static void FlushQueueToDiskForTesting() => _queue?.FlushSync();

        // Invokes the timer callback body directly so the overlapping-tick
        // guard can be exercised without a real timer.
        internal static void SendBatchForTesting() => SendBatch();

        // Drives a single heartbeat through the active Session so lifecycle
        // tests can assert that OnPause / OnResume actually route to
        // Session.Pause / Session.Resume. The real heartbeat cadence is
        // 60 s (Session.HeartbeatIntervalMs) so a timer-driven pass would
        // either take 60 s or need a bespoke interval override per test.
        internal static void InvokeSessionHeartbeatForTesting() => _session?.OnHeartbeat();

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        private static bool CanTrack()
        {
            return _initialized && _consent.CanTrack();
        }

        // Shallow-copy the caller's dict so a post-call mutation cannot race the drain-thread serialiser.
        private static Dictionary<string, object>? SnapshotCallerDict(Dictionary<string, object>? src) =>
            src != null ? new Dictionary<string, object>(src) : null;

        private static void Enqueue(Dictionary<string, object>? msg)
        {
            var queue = _queue;
            if (queue == null) return;

            // Re-check consent inside the drain lock so a SetConsent(None) racing
            // the caller's CanTrack cannot leak this event past the purge.
            queue.EnqueueChecked(msg, () => _consent.CanTrack());
        }

        private static void SendBatch()
        {
            // CAS in the guard before doing any work; a previous tick still
            // running means skip entirely, including the reschedule — the
            // in-flight tick will reschedule on its own finally path.
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
                        // ThreadPool timer thread; no caller above to catch.
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

        // Realigns the timer to NextAttemptAt so we don't repoll through a long backoff window.
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

        // consentAtInit snapshot is only used to skip the launch event under None;
        // Track still consults live _consent via CanTrack, so a SetConsent(None)
        // landing between Init returning and here still drops the event.
        private static void FireGameLaunch(AudienceConfig config, ConsentLevel consentAtInit)
        {
            if (!consentAtInit.CanTrack()) return;

            var properties = new Dictionary<string, object>();

            // Unity-side auto-detected context (platform, version, buildGuid,
            // unityVersion) from AudienceUnityHooks. Core stays pure C#; the
            // Unity layer fills these via LaunchContextProvider.
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

            // Config-supplied distributionPlatform wins over any provider value;
            // studios set it explicitly because Unity cannot auto-detect the store.
            if (config.DistributionPlatform != null)
                properties["distributionPlatform"] = config.DistributionPlatform;

            // No sessionId on game_launch. Event Reference (v1) schema for
            // game_launch lists platform, version, buildGuid,
            // distributionPlatform, unityVersion — not sessionId.
            // Correlation with the session happens at the pipeline layer via
            // eventTimestamp ordering; session_start fires immediately
            // before game_launch (see Init ordering) and carries the id
            // explicitly.

            Track("game_launch", properties.Count > 0 ? properties : null);
        }
    }
}
