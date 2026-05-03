#nullable enable

namespace Immutable.Audience
{
    internal static class Constants
    {
        internal const string TestKeyPrefix = "pk_imapik-test-";
        internal const string SandboxBaseUrl = "https://api.sandbox.immutable.com";
        internal const string ProductionBaseUrl = "https://api.immutable.com";

        internal const string MessagesPath = "/v1/audience/messages";
        internal const string ConsentPath = "/v1/audience/tracking-consent";
        internal const string DataPath = "/v1/audience/data";

        internal const int DefaultFlushIntervalSeconds = 5;
        internal const int DefaultFlushSize = 20;
        internal const int MaxBatchSize = 100;
        internal const int StaleEventDays = 30;
        internal const int MaxFieldLength = 256; // Backend schema limit.

        // Timestamp format the backend wants on every event.
        internal const string IsoTimestampFormat = "o";

        // Format that lets numbers survive a JSON round-trip unchanged.
        internal const string RoundTripNumberFormat = "R";
        internal const int ControlPlaneRequestTimeoutSeconds = 30;

        // How long we wait for one POST before giving up.
        // Without this, one stuck request can block everything else.
        internal const int MessagesRequestTimeoutSeconds = 30;

        // How long we wait before retrying after a failed POST. Doubles each time.
        internal const int HttpBackoff1stMs = 5_000;
        internal const int HttpBackoff2ndMs = 10_000;
        internal const int HttpBackoff3rdMs = 20_000;
        internal const int HttpBackoff4thMs = 40_000;
        internal const int HttpBackoffCapMs = 60_000;

        // How often a session_heartbeat is emitted while a session is live.
        internal const int SessionHeartbeatIntervalMs = 60_000;

        // How long a paused session can stay paused before Resume rolls it.
        internal const int SessionPauseTimeoutMs = 30_000;

        // How long we wait for an in-flight heartbeat callback to finish during teardown.
        internal const int SessionHeartbeatDrainTimeoutMs = 1_000;

        // How long we wait for the previous heartbeat to clear when Start is called twice.
        internal const int SessionStartDrainTimeoutMs = 500;

        // How many times we retry the consent-sync PUT after a 429.
        internal const int ConsentSyncMaxAttempts = 4;

        // How long we wait before the first consent-sync retry. Doubles each time.
        internal const int ConsentSyncBaseRetryMs = 1_000;

        internal const string LibraryName = "com.immutable.audience";
        internal const string LibraryVersion = "0.1.0";
        internal const string Surface = "unity";
        internal const string ConsentSource = "UnitySDK";

        internal const string PublishableKeyHeader = "x-immutable-publishable-key";
        internal const string ContentEncodingHeader = "Content-Encoding";

        internal const string MediaTypeJson = "application/json";
        internal const string GzipEncoding = "gzip";

        internal static string MessagesUrl(string? publishableKey, string? baseUrlOverride = null) =>
            BaseUrl(publishableKey, baseUrlOverride) + MessagesPath;
        internal static string ConsentUrl(string? publishableKey, string? baseUrlOverride = null) =>
            BaseUrl(publishableKey, baseUrlOverride) + ConsentPath;
        internal static string DataUrl(string? publishableKey, string? baseUrlOverride = null) =>
            BaseUrl(publishableKey, baseUrlOverride) + DataPath;

        // Override wins when non-empty; otherwise test keys map to Sandbox
        // and every other key maps to Production. Matches @imtbl/audience.
        internal static string BaseUrl(string? publishableKey, string? baseUrlOverride = null)
        {
            if (!string.IsNullOrEmpty(baseUrlOverride)) return baseUrlOverride!;
            return publishableKey != null && publishableKey.StartsWith(TestKeyPrefix)
                ? SandboxBaseUrl
                : ProductionBaseUrl;
        }
    }

    // Message type values written to (and read back from) the "type" field.
    internal static class MessageTypes
    {
        internal const string Track = "track";
        internal const string Identify = "identify";
        internal const string Alias = "alias";
    }

    // Property keys for the auto-fired game_launch event.
    internal static class GameLaunchPropertyKeys
    {
        internal const string Platform = "platform";
        internal const string Version = "version";
        internal const string BuildGuid = "buildGuid";
        internal const string UnityVersion = "unityVersion";
        internal const string OsFamily = "osFamily";
        internal const string DeviceModel = "deviceModel";
        internal const string Gpu = "gpu";
        internal const string GpuVendor = "gpuVendor";
        internal const string Cpu = "cpu";
        internal const string CpuCores = "cpuCores";
        internal const string RamMb = "ramMb";
        internal const string ScreenDpi = "screenDpi";
        internal const string DistributionPlatform = "distributionPlatform";
    }

    // Keys merged into every event's context dictionary.
    internal static class ContextKeys
    {
        internal const string UserAgent = "userAgent";
        internal const string Timezone = "timezone";
        internal const string Locale = "locale";
        internal const string Screen = "screen";
    }

    // JSON keys for the consent-sync PUT body.
    internal static class ConsentBodyFields
    {
        internal const string Status = "status";
        internal const string Source = "source";
    }

    // JSON keys for the messages POST envelope and response.
    internal static class ResponseFields
    {
        internal const string MessagesEnvelope = "messages";
        internal const string Rejected = "rejected";
    }

    // Keys inside each event's "properties" dict.
    internal static class EventPropertyKeys
    {
        // Shared across multiple events (Session + Progression for DurationSec,
        // Resource + Purchase for Currency / ItemId).
        internal const string SessionId = "sessionId";
        internal const string DurationSec = "durationSec";
        internal const string Currency = "currency";
        internal const string ItemId = "itemId";

        // Progression-specific
        internal const string Status = "status";
        internal const string World = "world";
        internal const string Level = "level";
        internal const string Stage = "stage";
        internal const string Score = "score";

        // Resource-specific
        internal const string Flow = "flow";
        internal const string Amount = "amount";
        internal const string ItemType = "itemType";

        // Purchase-specific
        internal const string Value = "value";
        internal const string ItemName = "itemName";
        internal const string Quantity = "quantity";
        internal const string TransactionId = "transactionId";

        // MilestoneReached-specific
        internal const string Name = "name";
    }

    // Event names we send on Track.
    internal static class EventNames
    {
        // Session lifecycle (auto-fired)
        internal const string SessionStart = "session_start";
        internal const string SessionEnd = "session_end";
        internal const string SessionHeartbeat = "session_heartbeat";

        // Init lifecycle (auto-fired)
        internal const string GameLaunch = "game_launch";

        // Typed events (IEvent implementations)
        internal const string Progression = "progression";
        internal const string Resource = "resource";
        internal const string Purchase = "purchase";
        internal const string MilestoneReached = "milestone_reached";
    }

    // Wire-format field names that cross module boundaries inside the SDK
    // (read by one module, written by another).
    internal static class MessageFields
    {
        // Envelope keys present on every message
        internal const string Type = "type";
        internal const string MessageId = "messageId";
        internal const string EventTimestamp = "eventTimestamp";
        internal const string Context = "context";
        internal const string Surface = "surface";

        // Track envelope
        internal const string EventName = "eventName";
        internal const string Properties = "properties";

        // Identity envelope (track, identify, alias)
        internal const string AnonymousId = "anonymousId";
        internal const string UserId = "userId";

        // Identify envelope
        internal const string IdentityType = "identityType";
        internal const string Traits = "traits";

        // Alias envelope
        internal const string FromId = "fromId";
        internal const string FromType = "fromType";
        internal const string ToId = "toId";
        internal const string ToType = "toType";

        // Context dictionary keys
        internal const string Library = "library";
        internal const string LibraryVersion = "libraryVersion";
    }

    /// <summary>
    /// Common values for <see cref="AudienceConfig.DistributionPlatform"/>.
    /// </summary>
    public static class DistributionPlatforms
    {
        /// <summary>Steam.</summary>
        public const string Steam = "steam";

        /// <summary>Epic Games Store.</summary>
        public const string Epic = "epic";

        /// <summary>GOG.com.</summary>
        public const string GOG = "gog";

        /// <summary>itch.io.</summary>
        public const string Itch = "itch";

        /// <summary>
        /// Standalone build, distributed outside any storefront.
        /// </summary>
        public const string Standalone = "standalone";
    }
}
