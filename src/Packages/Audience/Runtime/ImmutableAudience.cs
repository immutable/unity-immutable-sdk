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
        // _consent and _userId are mutated outside the lock and need volatile themselves.
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

        // AudienceUnityHooks sets this at SubsystemRegistration so Unity studios
        // can omit PersistentDataPath from AudienceConfig and Init will fill it
        // from Application.persistentDataPath. Non-Unity callers must still set
        // PersistentDataPath on the config.
        internal static Func<string>? DefaultPersistentDataPathProvider;

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
            }

            FireGameLaunch(config, consentAtInit);
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
        // For predefined event names (e.g. purchase), prefer the typed
        // overload Track(new Purchase { ... }) — it enforces required fields
        // and value types at compile time. This overload does not validate
        // property shapes, so missing or mistyped fields can break
        // attribution/conversion reporting.
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
        public static void Identify(string userId, string? identityType, Dictionary<string, object>? traits = null)
        {
            if (!_initialized) return;

            // Validate inputs before consent so null-arg callers get the right warning.
            if (string.IsNullOrEmpty(userId))
            {
                Log.Warn("Identify called with null or empty userId — dropping.");
                return;
            }
            if (string.IsNullOrEmpty(identityType))
            {
                Log.Warn("Identify called with null or empty identityType — dropping.");
                return;
            }
            if (_consent != ConsentLevel.Full)
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
        public static void Alias(string fromId, string? fromType, string toId, string? toType)
        {
            if (!_initialized) return;

            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                Log.Warn("Alias called with null or empty fromId/toId — dropping.");
                return;
            }
            if (string.IsNullOrEmpty(fromType) || string.IsNullOrEmpty(toType))
            {
                Log.Warn("Alias called with null or empty fromType/toType — dropping.");
                return;
            }
            if (_consent != ConsentLevel.Full)
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

        // Ask the backend to erase this player's data.
        public static void DeleteData(string? userId = null)
        {
            if (!_initialized) return;

            var config = _config;
            var client = _controlClient;
            if (config == null || client == null) return;

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
                    return;
                query = "anonymousId=" + Uri.EscapeDataString(anonymousId);
            }

            var url = Constants.DataUrl(config.PublishableKey) + "?" + query;
            var onError = config.OnError;
            var publishableKey = config.PublishableKey;
            var cancellationToken = _shutdownCancellationSource?.Token ?? CancellationToken.None;

            Task.Run(async () =>
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
            }

            if (level == ConsentLevel.None)
            {
                queue?.PurgeAll();
                Identity.Reset(config.PersistentDataPath!);
            }
            else if (previous == ConsentLevel.Full && level == ConsentLevel.Anonymous)
            {
                _userId = null;
                queue?.ApplyAnonymousDowngrade();
            }

            SyncConsentToBackend(config, level, anonymousIdForPut);
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
            if (!_initialized) return;

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

        // -----------------------------------------------------------------
        // Internal — shared with tests and AudienceUnityHooks
        // -----------------------------------------------------------------

        // Shuts down (if initialised) and clears per-session state so a
        // fresh Init starts clean. Used on test teardown and by Unity
        // SubsystemRegistration to survive "disable domain reload".
        internal static void ResetState()
        {
            if (_initialized)
                Shutdown();

            _consent = ConsentLevel.None;
            // Drop Identity's static cache so a subsequent Init with a different
            // persistentDataPath (tests, domain reload with changed config) reads
            // the file from the new path, not the previous session's cached id.
            Identity.ClearCache();
        }

        internal static ConsentLevel CurrentConsentForTesting => _consent;

        internal static void FlushQueueToDiskForTesting() => _queue?.FlushSync();

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        private static bool CanTrack()
        {
            return _initialized && _consent != ConsentLevel.None;
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
            queue.EnqueueChecked(msg, () => _consent != ConsentLevel.None);
        }

        private static void SendBatch()
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
            if (consentAtInit == ConsentLevel.None) return;

            var properties = new Dictionary<string, object>();

            if (config.DistributionPlatform != null)
                properties["distributionPlatform"] = config.DistributionPlatform;

            // Device-derived fields (platform, version, buildGuid, unityVersion) land with DeviceCollector.
            Track("game_launch", properties.Count > 0 ? properties : null);
        }
    }
}
