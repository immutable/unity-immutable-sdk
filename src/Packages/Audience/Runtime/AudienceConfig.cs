#nullable enable

using System;

namespace Immutable.Audience
{
    /// <summary>
    /// Configuration passed to <see cref="ImmutableAudience.Init"/>.
    /// </summary>
    public class AudienceConfig
    {
        /// <summary>
        /// Studio API key issued by Immutable Hub.
        /// </summary>
        /// <remarks>
        /// Required. <see cref="ImmutableAudience.Init"/> throws if null or empty.
        /// </remarks>
        public string? PublishableKey { get; set; }

        /// <summary>
        /// Override the default API base URL.
        /// </summary>
        /// <remarks>
        /// When null, publishable keys starting with <c>pk_imapik-test-</c>
        /// resolve to Sandbox. All other keys resolve to Production. Set
        /// explicitly to target a different backend.
        /// </remarks>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Initial consent level.
        /// </summary>
        /// <remarks>
        /// If the SDK persisted a different level on a previous launch, that
        /// persisted level overrides this default at
        /// <see cref="ImmutableAudience.Init"/>.
        /// </remarks>
        public ConsentLevel Consent { get; set; } = ConsentLevel.None;

        /// <summary>
        /// Distribution platform the game is running on.
        /// </summary>
        /// <seealso cref="DistributionPlatforms"/>
        public string? DistributionPlatform { get; set; }

        /// <summary>
        /// Whether the SDK emits debug log lines for every event, flush,
        /// and consent change.
        /// </summary>
        public bool Debug { get; set; } = false;

        /// <summary>
        /// Opts into mobile install-attribution signals (iOS ATT / IDFA /
        /// SKAdNetwork, Android Advertising ID / Install Referrer). Default
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Two gates control attribution; both must be set for any data to
        /// ship:
        ///
        /// 1. Build-time: add <c>AUDIENCE_MOBILE_ATTRIBUTION</c> to Player
        ///    Settings → Other Settings → Scripting Define Symbols. Controls
        ///    the AD_ID Android manifest permission, the iOS Privacy Manifest
        ///    variant (<c>NSPrivacyTracking</c>), and whether native
        ///    attribution code is compiled into the binary.
        ///
        /// 2. Runtime: this flag. Controls whether attribution data is
        ///    collected at runtime. Without the define, this setter is a
        ///    no-op.
        ///
        /// Studios who set neither ship a clean binary — no AD_ID permission,
        /// no native attribution code, <c>NSPrivacyTracking = false</c>.
        /// </remarks>
        public bool EnableMobileAttribution { get; set; } = false;

        /// <summary>
        /// SKAdNetwork IDs the iOS post-processor injects into <c>Info.plist</c>
        /// at build time. Ignored on Android.
        /// </summary>
        /// <remarks>
        /// Read only when <see cref="EnableMobileAttribution"/> and the
        /// <c>AUDIENCE_MOBILE_ATTRIBUTION</c> scripting define are both set.
        /// </remarks>
        public string[]? SKAdNetworkIds { get; set; }

        /// <summary>
        /// Interval between automatic flushes to the backend, in seconds.
        /// </summary>
        public int FlushIntervalSeconds { get; set; } = Constants.DefaultFlushIntervalSeconds;

        /// <summary>
        /// Queued-event threshold that triggers an automatic flush before the
        /// next interval tick.
        /// </summary>
        public int FlushSize { get; set; } = Constants.DefaultFlushSize;

        /// <summary>
        /// Callback fired when the SDK encounters a recoverable failure.
        /// </summary>
        /// <remarks>
        /// Exceptions thrown from the callback are swallowed so a bad handler
        /// cannot wedge the SDK.
        /// </remarks>
        public Action<AudienceError>? OnError { get; set; }

        /// <summary>
        /// Directory the SDK uses for identity, consent, and queued events.
        /// Usually populated automatically by Unity hooks.
        /// </summary>
        public string? PersistentDataPath { get; set; }

        /// <summary>
        /// Library version sent on every message.
        /// </summary>
        public string PackageVersion { get; set; } = Constants.LibraryVersion;

        /// <summary>
        /// Maximum time <see cref="ImmutableAudience.Shutdown"/> waits for
        /// the final flush, in milliseconds.
        /// </summary>
        public int ShutdownFlushTimeoutMs { get; set; } = 2_000;

        // Test seam for HttpTransport. Not part of the public API.
        internal System.Net.Http.HttpMessageHandler? HttpHandler { get; set; }
    }
}
