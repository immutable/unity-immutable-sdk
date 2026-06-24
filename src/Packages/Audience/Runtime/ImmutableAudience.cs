#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
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

        // Called during Init when config.EnableMobileAttribution is true.
        // Returns true on first SKAN registration, null if already done or not applicable.
        // Set by the Unity layer; null in pure-C# environments.
        internal static volatile Func<bool?>? MobileAttributionProvider;

        // Called during Init when config.EnableMobileAttribution is true.
        // Returns iOS attribution context (attStatus, idfa) to merge into
        // game_launch properties. Set by the Unity layer.
        internal static volatile Func<IReadOnlyDictionary<string, object>?>? MobileAttributionContextProvider;

        // Backs RequestTrackingAuthorizationAsync. Set by the Unity layer to
        // ATTBridge.RequestAsync; null in pure-C# environments and on
        // non-iOS platforms (the public API resolves to NotDetermined).
        internal static volatile Func<Task<int>>? TrackingAuthorizationRequestProvider;

        // Called during Init when config.EnableMobileAttribution is true.
        // Returns the cached Android Play Install Referrer string, or null if
        // not yet cached (first launch, async fetch may complete after
        // game_launch fires) or none exists for this install. Set by the Unity
        // layer; null in pure-C# environments and on non-Android platforms.
        internal static volatile Func<string?>? MobileInstallReferrerProvider;

        // Returns the current iOS ATT status int (0=notDetermined, 1=restricted,
        // 2=denied, 3=authorized). Used by tracking_authorization_changed detection
        // on Init and OnResume. Set by the Unity layer on iOS; null elsewhere.
        internal static volatile Func<int?>? MobileATTStatusProvider;

        // Returns the IDFA string when ATT is authorized. Included in
        // tracking_authorization_changed only when transitioning to authorized
        // with Full consent. Set by the Unity layer on iOS; null elsewhere.
        internal static volatile Func<string?>? MobileIDFAProvider;

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
        /// A stable per-device ID that survives <see cref="Reset"/> (logout).
        /// </summary>
        /// <remarks>
        /// Generated once on first init at Anonymous+ consent and persisted across launches.
        /// Survives <see cref="Reset"/> so the CDP can resolve a returning player without a
        /// re-identify. Wiped on <see cref="ConsentLevel.None"/> (opt-out). App-generated UUID,
        /// not a hardware fingerprint. Null while consent is None.
        /// </remarks>
        public static string? DeviceId
        {
            get
            {
                if (!_initialized) return null;
                var config = _config;
                var level = _state.Level;
                if (config == null || !level.CanTrack()) return null;
                return Identity.GetOrCreateDeviceId(config.PersistentDataPath!, level);
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

            // Consent gate before invoking attribution providers: SKAN
            // registration and Install Referrer fetch are network side
            // effects, and IDFA / ATT status reads are privacy-sensitive.
            // CanTrack() == false (consent None) means we have no licence
            // to run any of them, regardless of whether EnableMobileAttribution
            // is set in config.
            bool? skanRegistered = null;
            IReadOnlyDictionary<string, object>? attributionContext = null;
            string? installReferrer = null;
            if (config.EnableMobileAttribution && consentAtInit.CanTrack())
            {
                try { skanRegistered = MobileAttributionProvider?.Invoke(); }
                catch (Exception ex) { Log.Warn(AudienceLogs.MobileAttributionProviderThrew(ex)); }

                try { attributionContext = MobileAttributionContextProvider?.Invoke(); }
                catch (Exception ex) { Log.Warn(AudienceLogs.MobileAttributionContextProviderThrew(ex)); }

                try { installReferrer = MobileInstallReferrerProvider?.Invoke(); }
                catch (Exception ex) { Log.Warn(AudienceLogs.MobileInstallReferrerProviderThrew(ex)); }
            }

            FireGameLaunch(config, consentAtInit, skanRegistered, attributionContext);
            TryIdentifySteamUser();
            TryIdentifyEpicUser();

            CheckAndFireAttStatusChanged(config, consentAtInit);

            // Fires once per install. installReferrer lands asynchronously
            // from Google Play Services; on the first launch the cache is
            // usually still empty when game_launch fires, so we ship a
            // dedicated event after Init when the value first becomes
            // observable. Idempotent across launches via an on-disk marker.
            // installReferrer encodes campaign attribution source, same privacy
            // class as userId. Only ship at Full; don't write the sent marker
            // at Anonymous so a later consent upgrade can fire the event.
            if (!string.IsNullOrEmpty(installReferrer) && consentAtInit.CanIdentify())
                FireInstallReferrerReceivedOnce(config, installReferrer!);
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
            var deviceId = Identity.GetOrCreateDeviceId(config.PersistentDataPath!, state.Level);
            // ToProperties returns a fresh dict per call, so no snapshot needed.
            var userId = state.Level == ConsentLevel.Full ? state.UserId : null;
            var msg = MessageBuilder.Track(eventName, anonymousId, userId, deviceId, Constants.LibraryVersion, properties, config.TestMode);
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
            var deviceId = Identity.GetOrCreateDeviceId(config.PersistentDataPath!, state.Level);
            var userId = state.Level == ConsentLevel.Full ? state.UserId : null;
            var msg = MessageBuilder.Track(eventName, anonymousId, userId, deviceId, Constants.LibraryVersion,
                SnapshotCallerDict(properties), config.TestMode);
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
            var deviceId = Identity.GetOrCreateDeviceId(config.PersistentDataPath!, level);
            var msg = MessageBuilder.Identify(anonymousId, userId, deviceId, identityType.ToLowercaseString(),
                Constants.LibraryVersion, SnapshotCallerDict(traits), config.TestMode);
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

            var deviceId = Identity.GetOrCreateDeviceId(config.PersistentDataPath!, state.Level);
            var msg = MessageBuilder.Alias(fromId, fromType.ToLowercaseString(), toId, toType.ToLowercaseString(),
                deviceId, Constants.LibraryVersion, config.TestMode);
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

            // Phase 2 outside _initLock: Dispose enqueues session_end, PurgeAll wipes it,
            // RotateAnonymousId rotates anon_id (device_id kept), Start emits the new session_start.
            oldSession?.Dispose();
            queueForPurge?.PurgeAll();
            Identity.RotateAnonymousId(config.PersistentDataPath!);
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

            var url = Constants.DataUrl(config.BaseUrl) + "?" + query;
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

            var url = Constants.ConsentUrl(config.BaseUrl);
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
                // 429 retried up to 4 attempts (1s/2s/4s or Retry-After).
                // Other non-2xx fail fast.
                const int maxAttempts = 4;
                var attempt = 0;
                try
                {
                    while (true)
                    {
                        attempt++;
                        using var request = new HttpRequestMessage(HttpMethod.Put, url);
                        request.Headers.Add(Constants.PublishableKeyHeader, publishableKey);
                        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode) return;

                        if ((int)response.StatusCode == 429 && attempt < maxAttempts)
                        {
                            var delay = HttpRetry.ParseRetryAfter(response)
                                ?? TimeSpan.FromMilliseconds(1_000 * (1 << (attempt - 1)));
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
        // Mobile attribution
        // -----------------------------------------------------------------

        /// <summary>
        /// Requests the iOS App Tracking Transparency authorization. Triggers
        /// the system prompt on first call; returns the cached status on
        /// subsequent calls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Studios decide when to show the prompt. Apple's HIG requires it
        /// to fire at a moment that makes the value to the player obvious,
        /// not at SDK Init. The IDFA, when authorized, is collected
        /// automatically on the next <see cref="ImmutableAudience.Init"/> via
        /// the <c>game_launch</c> event when
        /// <see cref="AudienceConfig.EnableMobileAttribution"/> is set.
        /// </para>
        /// <para>
        /// Resolves to <see cref="TrackingAuthorizationStatus.NotDetermined"/>
        /// in three distinct cases. Callers who use this signal to decide
        /// whether to retry should consult the SDK log to disambiguate:
        /// </para>
        /// <list type="bullet">
        ///   <item>The user has not yet been prompted, or the prompt was dismissed without a choice.</item>
        ///   <item>The platform is not iOS, or the iOS version is &lt; 14.</item>
        ///   <item>The native call threw an unexpected exception (logged via <c>Log.Warn</c>).</item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// A task that completes with the user's authorization decision.
        /// </returns>
        public static async Task<TrackingAuthorizationStatus> RequestTrackingAuthorizationAsync()
        {
            var provider = TrackingAuthorizationRequestProvider;
            if (provider == null)
                return TrackingAuthorizationStatus.NotDetermined;

            int status;
            try
            {
                status = await provider().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.TrackingAuthorizationRequestThrew(ex));
                return TrackingAuthorizationStatus.NotDetermined;
            }

            // Defensively clamp: any value outside Apple's documented range
            // (0..3) is surfaced as NotDetermined rather than as an invalid
            // enum cast that callers can't pattern-match safely.
            if (status < 0 || status > 3)
                return TrackingAuthorizationStatus.NotDetermined;

            // Pass the resolved status directly to avoid a redundant native call.
            var config = _config;
            if (_initialized && config != null)
                CheckAndFireAttStatusChanged(config, _state.Level, status);

            return (TrackingAuthorizationStatus)status;
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

        // Resolves Steamworks.SteamAPI (Steamworks.NET) across all common install methods:
        //   UPM / OpenUPM package  → com.rlabrecque.steamworks.net
        //   .dll plugin in Assets  → Steamworks.NET
        //   source files in Assets → Assembly-CSharp
        // Returns null when Steamworks.NET is not present.
        private static System.Type? ResolveSteamApiType() =>
            System.Type.GetType("Steamworks.SteamAPI, com.rlabrecque.steamworks.net")
            ?? System.Type.GetType("Steamworks.SteamAPI, Steamworks.NET")
            ?? System.Type.GetType("Steamworks.SteamAPI, Assembly-CSharp");

        // Resolves Steamworks.SteamClient (Facepunch.Steamworks) across common install methods.
        // Facepunch ships platform-specific DLLs; the assembly name encodes the platform:
        //   macOS/Linux → Facepunch.Steamworks.Posix
        //   Windows 64  → Facepunch.Steamworks.Win64
        //   Windows 32  → Facepunch.Steamworks.Win32
        //   Source in Assets → Assembly-CSharp
        // Returns null when Facepunch.Steamworks is not present.
        private static System.Type? ResolveFacepunchSteamClientType() =>
            System.Type.GetType("Steamworks.SteamClient, Facepunch.Steamworks.Posix")
            ?? System.Type.GetType("Steamworks.SteamClient, Facepunch.Steamworks.Win64")
            ?? System.Type.GetType("Steamworks.SteamClient, Facepunch.Steamworks.Win32")
            ?? System.Type.GetType("Steamworks.SteamClient, Assembly-CSharp");

        // Sets distribution_platform = "steam" when any supported Steam wrapper is
        // detected as active. Config override wins afterward (line ~1195).
        private static void TryDetectSteamPlatform(Dictionary<string, object> properties)
        {
            try
            {
                if (IsSteamworksNetRunning() || IsFacepunchSteamValid())
                    properties["distribution_platform"] = DistributionPlatforms.Steam;
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.SteamPlatformDetectionFailed(ex));
            }
        }

        // Calls Identify with the logged-in SteamID64. Tries Steamworks.NET first,
        // then Facepunch.Steamworks. No-ops if neither is present, Steam is not
        // running, the ID is invalid, or consent is below Full.
        private static void TryIdentifySteamUser()
        {
            try
            {
                if (!TryGetSteamworksNetId(out var id) && !TryGetFacepunchId(out id))
                    return;
                Log.Debug(AudienceLogs.SteamAutoIdentified(id!));
                Identify(id!, IdentityType.Steam);
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.SteamIdentityCollectionFailed(ex));
            }
        }

        // Returns true if Steamworks.NET's SteamAPI.IsSteamRunning() is true.
        private static bool IsSteamworksNetRunning()
        {
            var steamApi = ResolveSteamApiType();
            if (steamApi == null) return false;
            return steamApi.GetMethod("IsSteamRunning")?.Invoke(null, null) as bool? == true;
        }

        // Returns true if Facepunch.Steamworks's SteamClient.IsValid is true.
        private static bool IsFacepunchSteamValid()
        {
            var steamClient = ResolveFacepunchSteamClientType();
            if (steamClient == null) return false;
            return steamClient.GetProperty("IsValid")?.GetValue(null) as bool? == true;
        }

        // Reads the SteamID64 via Steamworks.NET (SteamUser.GetSteamID → m_SteamID ulong).
        // Attempts SteamAPI.Init() first so the sample app works without manual Steam init;
        // in shipping games Init() is already called before our SDK runs.
        private static bool TryGetSteamworksNetId(out string? id)
        {
            id = null;
            var steamApi = ResolveSteamApiType();
            if (steamApi == null) return false;
            if (steamApi.GetMethod("IsSteamRunning")?.Invoke(null, null) as bool? != true) return false;

            // Safe to call even if already initialised; returns false but is otherwise a no-op.
            steamApi.GetMethod("Init")?.Invoke(null, null);

            var steamUserType =
                System.Type.GetType("Steamworks.SteamUser, com.rlabrecque.steamworks.net")
                ?? System.Type.GetType("Steamworks.SteamUser, Steamworks.NET")
                ?? System.Type.GetType("Steamworks.SteamUser, Assembly-CSharp");
            if (steamUserType == null) return false;

            var steamId = steamUserType.GetMethod("GetSteamID")?.Invoke(null, null);
            if (steamId == null) return false;

            // CSteamID.m_SteamID == 0 means not logged in / not initialised.
            var raw = steamId.GetType().GetField("m_SteamID")?.GetValue(steamId);
            if (raw == null || (ulong)raw == 0) return false;

            id = ((ulong)raw).ToString();
            return true;
        }

        // Reads the SteamID64 via Facepunch.Steamworks (SteamClient.SteamId.Value ulong).
        // Requires SteamClient.Init(appId) to have been called by the game already;
        // the SDK cannot call it as the appId is unknown.
        private static bool TryGetFacepunchId(out string? id)
        {
            id = null;
            var steamClient = ResolveFacepunchSteamClientType();
            if (steamClient == null) return false;
            if (steamClient.GetProperty("IsValid")?.GetValue(null) as bool? != true) return false;

            var steamIdProp = steamClient.GetProperty("SteamId");
            var steamId = steamIdProp?.GetValue(null);
            if (steamId == null) return false;

            // Facepunch SteamId exposes Value as a public ulong field (not a property).
            var raw = steamId.GetType().GetField("Value")?.GetValue(steamId);
            if (raw == null || (ulong)raw == 0) return false;

            id = ((ulong)raw).ToString();
            return true;
        }

        // Resolves PlayEveryWare.EpicOnlineServices.EOSManager across install methods.
        // Returns null when the EOS Unity plugin is not present.
        private static System.Type? ResolveEosManagerType() =>
            System.Type.GetType("PlayEveryWare.EpicOnlineServices.EOSManager, PlayEveryWare.EpicOnlineServices")
            ?? System.Type.GetType("PlayEveryWare.EpicOnlineServices.EOSManager, com.playeveryware.eos.core")
            ?? System.Type.GetType("PlayEveryWare.EpicOnlineServices.EOSManager, Assembly-CSharp");

        // Gets the initialised PlatformInterface handle from EOSManager.Instance.
        // Returns null when the EOS plugin is absent or EOS has not been initialised.
        private static object? GetEosPlatformInterface()
        {
            var managerType = ResolveEosManagerType();
            if (managerType == null) return null;
            // Use the compiled getter name (get_Instance) for IL2CPP compatibility;
            // property metadata can be stripped even when the method body survives.
            var instance = managerType.GetMethod("get_Instance")?.Invoke(null, null)
                ?? managerType.GetProperty("Instance")?.GetValue(null);
            if (instance == null) return null;
            return instance.GetType().GetMethod("GetEOSPlatformInterface")?.Invoke(instance, null);
        }

        // Sets distribution_platform = "epic" when the game was launched from the Epic
        // Games Store launcher. Uses command-line args injected by the EGS launcher
        // (-epicenv=, -epicapp=) rather than EOS-being-initialised, which would
        // misattribute Steam games that use EOS purely for cross-play.
        // Config override wins afterward.
        private static void TryDetectEpicPlatform(Dictionary<string, object> properties)
        {
            try
            {
                if (IsLaunchedFromEpicGamesStore())
                    properties["distribution_platform"] = DistributionPlatforms.Epic;
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.EpicPlatformDetectionFailed(ex));
            }
        }

        // EGS launcher injects these args into every game it launches, regardless of
        // whether the game integrates EOS. Absent when EOS is used for cross-play only.
        // -EpicPortal is the strongest signal (bare flag, no value); -epicapp= and
        // -epicenv= are the environment/artifact identifiers also always present.
        private static bool IsLaunchedFromEpicGamesStore()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Equals("-EpicPortal", StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith("-epicenv=", StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith("-epicapp=", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // Calls Identify with the logged-in EOS ProductUserId.
        // No-op if EOS is not present, not initialised, no user is logged in,
        // or consent is below Full.
        private static void TryIdentifyEpicUser()
        {
            try
            {
                if (!TryGetEpicAccountId(out var id))
                    return;
                Log.Debug(AudienceLogs.EpicAutoIdentified(id!));
                Identify(id!, IdentityType.Epic);
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.EpicIdentityCollectionFailed(ex));
            }
        }

        // Reads the EOS EpicAccountId via AuthInterface.GetLoggedInAccountByIndex(0).
        // EpicAccountId is the player's Epic Games Account, consistent across all products.
        // Requires the game to have already initialised EOS via EOSManager.
        private static bool TryGetEpicAccountId(out string? id)
        {
            id = null;

            // Guard: EOS C# bindings must be present (assembly name varies by install method).
            if (System.Type.GetType("Epic.OnlineServices.Auth.AuthInterface, com.Epic.OnlineServices") == null
                && System.Type.GetType("Epic.OnlineServices.Auth.AuthInterface, EOSSDK") == null)
                return false;

            var platformInterface = GetEosPlatformInterface();
            if (platformInterface == null) return false;

            var authInterface = platformInterface.GetType()
                .GetMethod("GetAuthInterface")?.Invoke(platformInterface, null);
            if (authInterface == null) return false;

            // Skip if no accounts are logged in.
            var countResult = authInterface.GetType()
                .GetMethod("GetLoggedInAccountsCount")?.Invoke(authInterface, null);
            if (!(countResult is int count && count > 0)) return false;

            // C# binding takes a plain int index, not an options struct.
            var epicAccountId = authInterface.GetType()
                .GetMethod("GetLoggedInAccountByIndex")?.Invoke(authInterface, new object[] { 0 });
            if (epicAccountId == null) return false;

            if (epicAccountId.GetType().GetMethod("IsValid")?.Invoke(epicAccountId, null) as bool? != true)
                return false;

            id = epicAccountId.ToString();
            return !string.IsNullOrEmpty(id);
        }

        // consentAtInit only gates the launch; Track still checks live _state via CanTrack.
        private static void FireGameLaunch(
            AudienceConfig config,
            ConsentLevel consentAtInit,
            bool? skanRegistered = null,
            IReadOnlyDictionary<string, object>? attributionContext = null)
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

            // Auto-detect distribution platform via reflection. Config override wins below.
            TryDetectSteamPlatform(properties);
            TryDetectEpicPlatform(properties);

            // Config-supplied distributionPlatform overrides the auto-detected value.
            if (config.DistributionPlatform != null)
                properties["distribution_platform"] = config.DistributionPlatform;

            // Emitted only on the first launch where SKAN registration fires.
            if (skanRegistered == true)
                properties["skan_registered"] = true;

            // iOS ATT/IDFA snapshot, merged after Unity context so attribution
            // keys are authoritative if both sources happen to set the same key.
            // idfa and gaid are cross-app device identifiers, same privacy class
            // as userId; gate them at Full-only. State-class keys (att_status,
            // gaid_limit_ad_tracking) are non-identifying and ship at Anon+Full.
            if (attributionContext != null)
            {
                var canIdentify = consentAtInit.CanIdentify();
                foreach (var kvp in attributionContext)
                {
                    if ((kvp.Key == "idfa" || kvp.Key == "gaid") && !canIdentify)
                        continue;
                    properties[kvp.Key] = kvp.Value;
                }
            }

            // No sessionId on game_launch per Event Reference. Pipeline correlates
            // via eventTimestamp with the session_start that fires just before.
            Track("game_launch", properties.Count > 0 ? properties : null);
        }

        // Fires install_referrer_received exactly once per install. Cache
        // file presence alone isn't enough. On first launch the bridge may
        // write the cache after Init has already run, so the event must be
        // dispatched at the next Init that observes a cache hit. The on-disk
        // "sent" marker provides idempotency across that boundary.
        private static void FireInstallReferrerReceivedOnce(AudienceConfig config, string installReferrer)
        {
            var sentFile = AudiencePaths.InstallReferrerSentFile(config.PersistentDataPath!);
            if (File.Exists(sentFile)) return;

            Track("install_referrer_received", new Dictionary<string, object>
            {
                ["install_referrer"] = installReferrer,
            });

            try
            {
                var dir = Path.GetDirectoryName(sentFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(sentFile, string.Empty);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Marker write failed. The event will re-fire on the next
                // launch. Pipeline-side dedup or the cost of one duplicate is
                // less bad than never sending the event at all.
                Log.Warn(AudienceLogs.InstallReferrerSentMarkerWriteFailed(ex));
            }
        }

        // Mirrors AttributionContext.AttStatusToString in the Unity layer; defined
        // here so the Core assembly has no dependency on the Unity assembly.
        private static string AttStatusToString(int status)
        {
            switch (status)
            {
                case 0: return "notDetermined";
                case 1: return "restricted";
                case 2: return "denied";
                case 3: return "authorized";
                default: return "unknown";
            }
        }

        // Fires tracking_authorization_changed when the ATT status differs from
        // the last-persisted observation. knownStatus skips the native re-read
        // when the caller already has the resolved value (e.g. after
        // RequestTrackingAuthorizationAsync resolves).
        //
        // First observation (no file): persists the baseline and returns without
        // firing. game_launch already captures the initial state on that Init.
        private static void CheckAndFireAttStatusChanged(
            AudienceConfig config,
            ConsentLevel consent,
            int? knownStatus = null)
        {
            if (!config.EnableMobileAttribution) return;
            if (!consent.CanTrack()) return;

            int currentStatus;
            if (knownStatus.HasValue)
            {
                currentStatus = knownStatus.Value;
            }
            else
            {
                var provider = MobileATTStatusProvider;
                if (provider == null) return;
                int? raw;
                try { raw = provider(); }
                catch (Exception ex) { Log.Warn(AudienceLogs.ATTStatusProviderThrew(ex)); return; }
                if (!raw.HasValue) return;
                currentStatus = raw.Value;
            }

            var previous = AttStatusStore.Load(config.PersistentDataPath!);

            if (previous == currentStatus) return;

            AttStatusStore.Save(config.PersistentDataPath!, currentStatus);

            if (!previous.HasValue)
                return; // first observation: no transition to report

            var props = new Dictionary<string, object>
            {
                ["previous_status"] = AttStatusToString(previous.Value),
                ["new_status"] = AttStatusToString(currentStatus),
            };

            if (currentStatus == 3 && consent.CanIdentify())
            {
                try
                {
                    var idfa = MobileIDFAProvider?.Invoke();
                    if (!string.IsNullOrEmpty(idfa))
                        props["idfa"] = idfa!;
                }
                catch (Exception ex) { Log.Warn(AudienceLogs.ATTIDFAProviderThrew(ex)); }
            }

            Track("tracking_authorization_changed", props);
        }
    }
}
