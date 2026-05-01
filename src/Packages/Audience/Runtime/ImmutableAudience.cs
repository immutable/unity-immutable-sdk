#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Immutable.Audience
{
    /// <summary>
    /// Entry point for the Immutable Audience SDK.
    /// </summary>
    public static class ImmutableAudience
    {
        // Reference fields are written inside _initLock; readers check the
        // `volatile _initialized` flag first so they never see a half-initialised state.
        // _state (consent level + userId) and _session are volatile so a write
        // on one thread is visible on any other. Every _state write happens
        // under _initLock so level and userId always move together. Callers
        // never observe (Anonymous, oldUserId).
        //
        // Init / Shutdown / Reset / SetConsent hold _initLock only to flip state
        // and capture references; they release the lock before running blocking
        // teardown (Session.Dispose, timer drain, queue shutdown, transport
        // flush, disposes). This keeps the hold time to nanoseconds so a caller
        // arriving on a different thread is not stranded behind those budgets.
        // How many times we retry the consent-sync PUT after a 429.
        internal const int ConsentSyncMaxAttempts = 4;

        // How long we wait before the first consent-sync retry. Doubles each time.
        internal const int ConsentSyncBaseRetryMs = 1_000;

        private static AudienceConfig? _config;
        private static DiskStore? _store;
        private static EventQueue? _queue;
        private static HttpTransport? _transport;
        private static HttpClient? _controlClient;
        private static CancellationTokenSource? _shutdownCancellationSource;
        private static Timer? _sendTimer;
        private static volatile ConsentState _state = ConsentState.None;
        private static volatile bool _initialized;
        private static readonly object _initLock = new object();

        // Gate against overlapping timer ticks (Timer callbacks run on independent ThreadPool threads).
        private static int _sendInFlight;

        // volatile: assigned on the Unity main thread at SubsystemRegistration,
        // read from the drain thread in Track / Identify paths.
        // The assignments happen before any event can fire in practice, but
        // volatile documents the cross-thread publish contract explicitly.
        internal static volatile Func<string>? DefaultPersistentDataPathProvider;
        internal static volatile Func<IReadOnlyDictionary<string, object>>? LaunchContextProvider;
        internal static volatile Func<IReadOnlyDictionary<string, object>>? ContextProvider;

        // Active session. Created at Init (or on upgrade from None) and disposed
        // on Shutdown or SetConsent(None). Volatile so OnPause/OnResume see
        // assignments from SetConsent without taking _initLock.
        private static volatile Session? _session;

        /// <summary>
        /// True between <see cref="Init"/> and <see cref="Shutdown"/>.
        /// </summary>
        public static bool Initialized => _initialized;

        /// <summary>
        /// The consent level the SDK is currently honouring.
        /// </summary>
        /// <seealso cref="SetConsent"/>
        public static ConsentLevel CurrentConsent => _state.Level;

        /// <summary>
        /// The user ID from the most recent
        /// <see cref="Identify(string, IdentityType, Dictionary{string, object})"/>
        /// call.
        /// </summary>
        /// <remarks>
        /// Null after <see cref="Reset"/> or when consent is below
        /// <see cref="ConsentLevel.Full"/>.
        /// </remarks>
        public static string? UserId => _state.UserId;

        /// <summary>
        /// An anonymous, persistent ID for this device.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="SessionId"/> (rotates per session) and
        /// <see cref="UserId"/> (identifies the player), this stays stable
        /// across sessions. <see cref="Reset"/> and <see cref="SetConsent"/>
        /// with <see cref="ConsentLevel.None"/> wipe it. Null while consent
        /// is None.
        /// </remarks>
        public static string? AnonymousId
        {
            get
            {
                if (!_initialized) return null;
                var config = _config;
                if (config == null || !_state.Level.CanTrack()) return null;
                // PersistentDataPath is validated non-null in Init; compiler can't propagate that.
                return Identity.Get(config.PersistentDataPath!);
            }
        }

        /// <summary>
        /// The current session's ID.
        /// </summary>
        /// <remarks>
        /// A new ID is assigned at <see cref="Init"/>, at <see cref="Reset"/>,
        /// and when the app resumes after the previous session has timed
        /// out. Null while consent is None.
        /// </remarks>
        public static string? SessionId => _session?.SessionId;

        /// <summary>
        /// Number of unsent events, in memory and on disk.
        /// </summary>
        public static int QueueSize
        {
            get
            {
                // Fence off the volatile _initialized load first, matching
                // the protocol documented on the reference fields. Without
                // this, a weak-memory-order reader could observe
                // _initialized=true but _queue/_store still null; the ?.
                // short-circuits to 0 in that case, but the inconsistency
                // would break the protocol the file claims to follow.
                if (!_initialized) return 0;
                var queue = _queue;
                var store = _store;
                var memory = queue?.InMemoryCount ?? 0;
                var disk = store?.Count() ?? 0;
                return memory + disk;
            }
        }

        /// <summary>
        /// Starts the SDK. Call once at launch.
        /// </summary>
        /// <param name="config">
        /// SDK configuration. <see cref="AudienceConfig.PublishableKey"/> is required.
        /// </param>
        public static void Init(AudienceConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.PublishableKey))
                throw new ArgumentException("PublishableKey is required", nameof(config));

            if (string.IsNullOrEmpty(config.PersistentDataPath))
                config.PersistentDataPath = DefaultPersistentDataPathProvider?.Invoke();
            if (string.IsNullOrEmpty(config.PersistentDataPath))
                throw new ArgumentException("PersistentDataPath is required", nameof(config));

            // Normalize casing so dashboards aggregate consistently. The
            // DistributionPlatforms constants ship lowercase; a studio that
            // passes "Steam" or "STEAM" would otherwise split rows from
            // constant-using studios in the same project.
            if (!string.IsNullOrEmpty(config.DistributionPlatform))
                config.DistributionPlatform = config.DistributionPlatform.ToLowerInvariant();

            ConsentLevel consentAtInit;
            Session? sessionToStart;
            lock (_initLock)
            {
                if (_initialized)
                {
                    Log.Warn(AudienceLogs.InitCalledTwice);
                    return;
                }

                WarnIfKeyEnvironmentMismatch(config.PublishableKey, config.BaseUrl);

                _config = config;
                Log.Enabled = config.Debug;
                // Persisted consent overrides the config default (prior downgrade survives restart).
                var initialLevel = ConsentStore.Load(config.PersistentDataPath) ?? config.Consent;
                _state = new ConsentState(initialLevel, null);

                _store = new DiskStore(config.PersistentDataPath);
                _queue = new EventQueue(_store, config.FlushIntervalSeconds, config.FlushSize);
                _transport = new HttpTransport(_store, config.PublishableKey, config.BaseUrl, config.OnError, config.HttpHandler);
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
                consentAtInit = initialLevel;

                // Session created under the lock; Start() deferred until after
                // release because session_start → Track takes its own locks.
                if (initialLevel.CanTrack())
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

        /// <summary>
        /// Sends a typed event. Prefer over the string overload for
        /// compile-time required-field validation.
        /// </summary>
        /// <param name="evt">The event to send.</param>
        public static void Track(IEvent evt)
        {
            var state = _state;
            if (!_initialized || !state.Level.CanTrack()) return;
            if (evt == null)
            {
                Log.Warn(AudienceLogs.TrackIEventNull);
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
                Log.Warn(AudienceLogs.TrackIEventThrew(evt.GetType().Name, ex));
                return;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                Log.Warn(AudienceLogs.TrackIEventEmptyName(evt.GetType().Name));
                return;
            }

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, state.Level);
            // ToProperties returns a fresh dict per call, so no snapshot needed.
            var userId = state.Level == ConsentLevel.Full ? state.UserId : null;
            var msg = MessageBuilder.Track(eventName, anonymousId, userId, config.PackageVersion, properties);
            EnqueueTrack(msg);
        }

        /// <summary>
        /// Sends a custom event. For <c>purchase</c>, <c>progression</c>,
        /// <c>resource</c>, and <c>milestone_reached</c>, prefer the typed
        /// overload.
        /// </summary>
        /// <param name="eventName">The wire-format event name.</param>
        /// <param name="properties">Optional event properties.</param>
        public static void Track(string eventName, Dictionary<string, object>? properties = null)
        {
            var state = _state;
            if (!_initialized || !state.Level.CanTrack()) return;
            if (string.IsNullOrEmpty(eventName))
            {
                Log.Warn(AudienceLogs.TrackStringEmptyName);
                return;
            }

            var config = _config;
            if (config == null) return;

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, state.Level);
            var userId = state.Level == ConsentLevel.Full ? state.UserId : null;
            var msg = MessageBuilder.Track(eventName, anonymousId, userId, config.PackageVersion,
                SnapshotCallerDict(properties));
            EnqueueTrack(msg);
        }

        // -----------------------------------------------------------------
        // Identity
        // -----------------------------------------------------------------

        /// <summary>
        /// Attaches a known user ID to subsequent events.
        /// </summary>
        /// <param name="userId">The player's identifier within the chosen provider.</param>
        /// <param name="identityType">The identity provider that issued <paramref name="userId"/>.</param>
        /// <param name="traits">Optional player attributes (email, name, etc.).</param>
        public static void Identify(string userId, IdentityType identityType, Dictionary<string, object>? traits = null)
        {
            if (!_initialized) return;

            // Validate inputs before consent so null-arg callers get the right warning.
            if (string.IsNullOrEmpty(userId))
            {
                Log.Warn(AudienceLogs.IdentifyEmptyUserId);
                return;
            }

            AudienceConfig? config;
            ConsentLevel level;
            // Update consent + userId under the init lock so they always move
            // together; another thread reading _state never sees one half-updated.
            lock (_initLock)
            {
                if (!_initialized) return;
                var current = _state;
                level = current.Level;
                if (!level.CanIdentify())
                {
                    Log.Warn(AudienceLogs.IdentifyDiscarded(level));
                    return;
                }
                config = _config;
                if (config == null) return;
                _state = current with { UserId = userId };
            }

            var anonymousId = Identity.GetOrCreate(config.PersistentDataPath!, level);
            var msg = MessageBuilder.Identify(anonymousId, userId, identityType.ToLowercaseString(),
                config.PackageVersion, SnapshotCallerDict(traits));
            EnqueueIdentity(msg);
        }

        /// <summary>
        /// Links two user IDs for the same player.
        /// </summary>
        /// <param name="fromId">The previously-known identifier.</param>
        /// <param name="fromType">Identity provider for <paramref name="fromId"/>.</param>
        /// <param name="toId">The new identifier.</param>
        /// <param name="toType">Identity provider for <paramref name="toId"/>.</param>
        public static void Alias(string fromId, IdentityType fromType, string toId, IdentityType toType)
        {
            if (!_initialized) return;

            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId))
            {
                Log.Warn(AudienceLogs.AliasEmptyIds);
                return;
            }
            var state = _state;
            if (!state.Level.CanIdentify())
            {
                Log.Warn(AudienceLogs.AliasDiscarded(state.Level));
                return;
            }

            var config = _config;
            if (config == null) return;

            var msg = MessageBuilder.Alias(fromId, fromType.ToLowercaseString(), toId, toType.ToLowercaseString(),
                config.PackageVersion);
            EnqueueIdentity(msg);
        }

        /// <summary>
        /// Logs out the current player. Call <see cref="FlushAsync"/> first
        /// to preserve queued events.
        /// </summary>
        public static void Reset()
        {
            // Phase 1 under _initLock: atomic _state.UserId clear + _session swap.
            // Blocking work (session drain, disk purge, identity wipe, new
            // session_start) runs outside the lock so callers racing on
            // _initLock don't wait.
            AudienceConfig? config;
            Session? oldSession;
            Session? newSession = null;
            EventQueue? queueForPurge;

            lock (_initLock)
            {
                if (!_initialized) return;
                config = _config;
                if (config == null) return;

                oldSession = _session;
                queueForPurge = _queue;
                _state = _state with { UserId = null };

                // Swap under the lock so racing SetConsent/OnPause/OnResume see
                // either the old, the new, or null; never a torn reference.
                _session = _state.Level.CanTrack() ? new Session(Track) : null;
                newSession = _session;
            }

            // Phase 2 outside _initLock. Order: Dispose enqueues session_end →
            // PurgeAll wipes it → Identity.Reset clears the anonymousId file →
            // Start emits the new session_start against the fresh id. Matches
            // the in-lock sequence this replaces.
            oldSession?.Dispose();
            queueForPurge?.PurgeAll();
            Identity.Reset(config.PersistentDataPath!);
            newSession?.Start();
        }

        /// <summary>
        /// Asks the backend to erase this player's data.
        /// </summary>
        /// <param name="userId">
        /// Optional. The known user ID to delete. When null, the SDK uses
        /// the device's persisted anonymous ID.
        /// </param>
        /// <returns>A task that completes when the backend has responded.</returns>
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

            var url = Constants.DataUrl(config.PublishableKey, config.BaseUrl) + "?" + query;
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
                    // Shutdown cancelled; caller is tearing down, no error fired.
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
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.OnErrorThrew(ex));
            }
        }

        // -----------------------------------------------------------------
        // Consent
        // -----------------------------------------------------------------

        /// <summary>
        /// Changes the player's consent level. Persists across restart.
        /// </summary>
        /// <param name="level">The new consent level.</param>
        public static void SetConsent(ConsentLevel level)
        {
            if (!_initialized) return;

            var config = _config;
            if (config == null) return;

            // Snapshot check before any I/O: no-op if already at target consent.
            var snapshotPrevious = _state.Level;
            if (level == snapshotPrevious) return;

            // Capture anonymousId for the PUT audit trail outside _initLock.
            // Identity methods hold their own _sync lock; disk I/O on a cold
            // cache (None → Anonymous/Full upgrade creates the UUID file) does
            // not block _initLock. A racing SetConsent may change _state
            // between this read and our lock acquire (acceptable, the racing
            // call fires its own PUT and our slightly-stale ID still
            // identifies the user.
            var anonymousIdForPut = snapshotPrevious == ConsentLevel.None
                ? Identity.GetOrCreate(config.PersistentDataPath!, level)
                : Identity.Get(config.PersistentDataPath!);

            // Phase 1 under _initLock: atomic _state swap and _session swap.
            // Phase 2 outside the lock runs the blocking side effects (persist,
            // dispose, purge, downgrade, backend sync, new session_start) so a
            // concurrent Shutdown / Init / Reset isn't held waiting on them.
            ConsentLevel previous;
            EventQueue? queue;
            Session? oldSession = null;
            Session? newSession = null;
            bool downgradeFullToAnonymous = false;

            lock (_initLock)
            {
                if (!_initialized) return;

                config = _config;
                queue = _queue;
                if (config == null) return;

                var previousState = _state;
                previous = previousState.Level;
                if (level == previous) return;

                // Atomic swap: Level + UserId publish together. Drop UserId on
                // any downgrade out of Full so a racing Track/Identify cannot
                // observe (Anonymous, oldUserId).
                _state = new ConsentState(
                    level,
                    level == ConsentLevel.Full ? previousState.UserId : null);

                if (level == ConsentLevel.None)
                {
                    // Swap the session reference under the lock; dispose outside.
                    // session_end is gated out by CanTrack (post-flip), matching
                    // revocation semantics.
                    oldSession = _session;
                    _session = null;
                }
                else if (previous == ConsentLevel.Full && level == ConsentLevel.Anonymous)
                {
                    downgradeFullToAnonymous = true;
                }
                else if (previous == ConsentLevel.None && _session == null)
                {
                    // Upgrade from None: allocate + publish the new Session under
                    // the lock so a concurrent SetConsent / Init sees the new
                    // reference and the double-allocation guard above fires.
                    newSession = new Session(Track);
                    _session = newSession;
                }
            }

            // Phase 2 outside _initLock.
            try
            {
                // PersistentDataPath validated non-null in Init; compiler can't propagate that.
                ConsentStore.Save(config.PersistentDataPath!, level);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Warn(AudienceLogs.ConsentPersistFailed(ex));
                NotifyErrorCallback(config.OnError, AudienceErrorCode.ConsentPersistFailed,
                    $"Consent persist failed: {ex.Message}");
            }

            if (level == ConsentLevel.None)
            {
                oldSession?.Dispose();
                queue?.PurgeAll();
                Identity.Reset(config.PersistentDataPath!);
            }
            else if (downgradeFullToAnonymous)
            {
                // Synchronous: EventQueue.ApplyAnonymousDowngrade holds _drainLock
                // while it rewrites on-disk files, blocking the in-queue drain
                // and shutting the race with HttpTransport. See the method's comment.
                queue?.ApplyAnonymousDowngrade();
            }

            newSession?.Start();


            SyncConsentToBackend(config, level, anonymousIdForPut);
        }

        // Fire-and-forget PUT /v1/audience/tracking-consent.
        private static void SyncConsentToBackend(AudienceConfig config, ConsentLevel level, string? anonymousId)
        {
            var client = _controlClient;
            if (client == null) return;

            var url = Constants.ConsentUrl(config.PublishableKey, config.BaseUrl);
            var publishableKey = config.PublishableKey;
            var onError = config.OnError;
            var cancellationToken = _shutdownCancellationSource?.Token ?? CancellationToken.None;

            var body = Json.Serialize(new Dictionary<string, object>
            {
                [ConsentBodyFields.Status] = level.ToLowercaseString(),
                [ConsentBodyFields.Source] = Constants.ConsentSource,
                // Explicit null lets the backend distinguish "unknown" from a missing field.
                ["anonymousId"] = anonymousId!,
            });

            Task.Run(async () =>
            {
                // 429 retried up to ConsentSyncMaxAttempts attempts (1s/2s/4s
                // or Retry-After). Other non-2xx fail fast.
                const int maxAttempts = ConsentSyncMaxAttempts;
                var attempt = 0;
                try
                {
                    while (true)
                    {
                        attempt++;
                        using var request = new HttpRequestMessage(HttpMethod.Put, url);
                        request.Headers.Add(Constants.PublishableKeyHeader, publishableKey);
                        request.Content = new StringContent(body, System.Text.Encoding.UTF8, Constants.MediaTypeJson);
                        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode) return;

                        if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxAttempts)
                        {
                            var delay = HttpRetry.ParseRetryAfter(response)
                                ?? TimeSpan.FromMilliseconds(ConsentSyncBaseRetryMs * (1 << (attempt - 1)));
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        NotifyErrorCallback(onError, AudienceErrorCode.ConsentSyncFailed,
                            $"Consent sync failed with status {(int)response.StatusCode}");
                        return;
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

        /// <summary>
        /// Sends all pending events now.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes when the flush finishes.</returns>
        public static async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (!_initialized) return;

            var queue = _queue;
            var transport = _transport;
            if (queue == null || transport == null) return;

            queue.FlushSync();

            // Only one send runs at a time. Without this, two FlushAsync
            // callers would both read the same batch from disk and send it
            // twice. Yield while another caller (the timer or another
            // FlushAsync) holds the in-flight slot.
            while (Interlocked.CompareExchange(ref _sendInFlight, 1, 0) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            try
            {
                while (!transport.IsInBackoffWindow &&
                       await transport.SendBatchAsync(cancellationToken).ConfigureAwait(false))
                {
                }
            }
            catch (ObjectDisposedException)
            {
                // Concurrent Shutdown disposed the transport. Exit silently;
                // caller is tearing down.
            }
            finally
            {
                Interlocked.Exchange(ref _sendInFlight, 0);
            }
        }

        /// <summary>
        /// Flushes and stops the SDK.
        /// </summary>
        public static void Shutdown()
        {
            // Fire session_end before taking _initLock. _initialized is still
            // true here so Track's CanTrack gate lets it through. Idempotent
            // under concurrent Shutdown / SetConsent(None) via the _sessionId
            // reset inside EmitEndAndSeal: a second call finds _sessionId
            // null and no-ops. Heartbeat timer drain still runs in Phase 2
            // via session.Dispose(); its re-emission inside End() also no-ops.
            _session?.EmitEndAndSeal();

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

                // Race guard: a concurrent Reset or SetConsent(upgrade-from-None)
                // may have swapped _session to a new instance that has already
                // fired session_start. Seal it too so its session_end lands
                // before the flag flip. Idempotent on the same instance (no-op
                // via _sessionId null check); the slow path only runs when
                // Reset fully completed its Start() between the outside-lock
                // call above and this point (a narrow window).
                _session?.EmitEndAndSeal();

                // Flip the gate. Init / SetConsent / Reset acquiring after
                // this see _initialized == false and return cleanly.
                _initialized = false;

                session = _session;
                _session = null;
                timer = _sendTimer;
                _sendTimer = null;
                queue = _queue;
                _queue = null;
                transport = _transport;
                _transport = null;
                controlClient = _controlClient;
                _controlClient = null;
                cts = _shutdownCancellationSource;
                _shutdownCancellationSource = null;

                timeoutMs = _config?.ShutdownFlushTimeoutMs ?? 2_000;

                // Drop Identity's in-memory cache so a later Init with a different
                // persistentDataPath reads the new file, not the stale cached id.
                Identity.ClearCache();

                _config = null;
                _store = null;
                _state = _state with { UserId = null };
            }

            // Phase 2 outside _initLock: end session, drain timers, flush, dispose.

            // End session first so session_end hits the queue before the final flush.
            session?.Dispose();

            // Parameterless Timer.Dispose would return immediately and race SendBatch.
            TimerDisposal.DisposeAndWait(timer, TimeSpan.FromSeconds(2));

            // Clear the gate in case WaitOne timed out with SendBatch still running
            // (a later Init would otherwise be stranded at 1).
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
                        Log.Warn(AudienceLogs.ShutdownFlushExceeded(timeoutMs));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(AudienceLogs.ShutdownFlushThrew(ex));
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
        // Internal: shared with tests and AudienceUnityHooks
        // -----------------------------------------------------------------

        // Providers reassigned by SubsystemRegistration.
        internal static void ResetState()
        {
            // Shutdown manages its own serialisation and releases _initLock before
            // its Phase 2 teardown, so calling it here does not strand waiters.
            Shutdown();

            lock (_initLock)
            {
                _state = ConsentState.None;
                // Defensive: Shutdown nulls _session too, but a future refactor
                // that bails before that null must not leak a stale Session.
                _session = null;
                Identity.ClearCache();
            }
        }

        internal static void FlushQueueToDiskForTesting() => _queue?.FlushSync();

        // Drives SendBatch without a real timer so the overlapping-tick guard is testable.
        internal static void SendBatchForTesting() => SendBatch();

        // Drives a single heartbeat so lifecycle tests don't wait the 60s cadence.
        internal static void InvokeSessionHeartbeatForTesting() => _session?.OnHeartbeat();

        // -----------------------------------------------------------------
        // Private
        // -----------------------------------------------------------------

        // Shallow-copy the caller's dict so a post-call mutation cannot race the drain-thread serialiser.
        private static Dictionary<string, object>? SnapshotCallerDict(Dictionary<string, object>? src) =>
            src != null ? new Dictionary<string, object>(src) : null;

        // Only the exact production/sandbox swap is flagged; custom dev/staging
        // URLs are intentional and left alone.
        private static void WarnIfKeyEnvironmentMismatch(string publishableKey, string? baseUrlOverride)
        {
            if (string.IsNullOrEmpty(baseUrlOverride)) return;

            var trimmed = baseUrlOverride!.TrimEnd('/');
            var isTestKey = publishableKey.StartsWith(Constants.TestKeyPrefix);

            if (isTestKey && trimmed == Constants.ProductionBaseUrl)
            {
                Log.Warn(AudienceLogs.TestKeyAgainstProduction);
            }
            else if (!isTestKey && trimmed == Constants.SandboxBaseUrl)
            {
                Log.Warn(AudienceLogs.NonTestKeyAgainstSandbox);
            }
        }

        // Checks the current consent inside the drain lock. If consent has
        // since dropped to None the message is discarded. If it dropped to
        // Anonymous the userId is stripped.
        private static void EnqueueTrack(Dictionary<string, object>? msg)
        {
            MergeUnityContext(msg);
            _queue?.EnqueueChecked(msg, m =>
            {
                var state = _state;
                if (!state.Level.CanTrack()) return null;
                if (state.Level != ConsentLevel.Full)
                    m.Remove(MessageFields.UserId);
                return m;
            });
        }

        // Identify / Alias require Full; drop if consent has downgraded.
        private static void EnqueueIdentity(Dictionary<string, object>? msg)
        {
            MergeUnityContext(msg);
            _queue?.EnqueueChecked(msg, m =>
                _state.Level == ConsentLevel.Full ? m : null);
        }

        private static void MergeUnityContext(Dictionary<string, object>? msg)
        {
            if (msg == null) return;

            var provider = ContextProvider;
            if (provider == null) return;

            IReadOnlyDictionary<string, object>? extra;
            try
            {
                extra = provider();
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.ContextProviderThrew(ex));
                return;
            }
            if (extra == null) return;

            if (!(msg.TryGetValue("context", out var ctxObj) && ctxObj is Dictionary<string, object> ctx))
            {
                ctx = new Dictionary<string, object>();
                msg["context"] = ctx;
            }

            foreach (var kv in extra)
                ctx[kv.Key] = kv.Value;
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
                        Log.Warn(AudienceLogs.SendBatchUnexpected(ex));
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

        // consentAtInit only gates the launch; Track still checks live _state via CanTrack.
        private static void FireGameLaunch(AudienceConfig config, ConsentLevel consentAtInit)
        {
            if (!consentAtInit.CanTrack()) return;

            var properties = new Dictionary<string, object>();

            // Unity auto-detected context (platform, version, buildGuid, unityVersion).
            // Core stays pure C#; Unity layer fills via LaunchContextProvider.
            var provider = LaunchContextProvider;
            if (provider != null)
            {
                IReadOnlyDictionary<string, object>? unityContext = null;
                try { unityContext = provider(); }
                catch (Exception ex)
                {
                    Log.Warn(AudienceLogs.LaunchContextProviderThrew(ex));
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
