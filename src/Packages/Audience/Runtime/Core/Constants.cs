#nullable enable

namespace Immutable.Audience
{
    internal static class Constants
    {
        internal const string DevBaseUrl = "https://api.dev.immutable.com";
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
        internal const int ControlPlaneRequestTimeoutSeconds = 30;

        internal const string LibraryName = "com.immutable.audience";
        internal const string LibraryVersion = "0.1.0";
        internal const string Surface = "unity";
        internal const string ConsentSource = "UnitySDK";

        internal const string PublishableKeyHeader = "x-immutable-publishable-key";

        internal static string MessagesUrl(AudienceEnvironment environment) => BaseUrl(environment) + MessagesPath;
        internal static string ConsentUrl(AudienceEnvironment environment) => BaseUrl(environment) + ConsentPath;
        internal static string DataUrl(AudienceEnvironment environment) => BaseUrl(environment) + DataPath;

        internal static string BaseUrl(AudienceEnvironment environment) =>
            environment switch
            {
                AudienceEnvironment.Dev => DevBaseUrl,
                AudienceEnvironment.Sandbox => SandboxBaseUrl,
                AudienceEnvironment.Production => ProductionBaseUrl,
                // Defensive: a future enum addition we forget to wire up
                // falls back to Sandbox so accidental production traffic
                // is impossible without explicit opt-in.
                _ => SandboxBaseUrl,
            };
    }

    // Message type values written to (and read back from) the "type" field.
    internal static class MessageTypes
    {
        internal const string Track = "track";
        internal const string Identify = "identify";
        internal const string Alias = "alias";
    }

    // Wire-format field names that cross module boundaries inside the SDK
    // (read by one module, written by another).
    internal static class MessageFields
    {
        internal const string Type = "type";
        internal const string UserId = "userId";
    }

    // Common distribution platform values for AudienceConfig.DistributionPlatform.
    public static class DistributionPlatforms
    {
        public const string Steam = "steam";
        public const string Epic = "epic";
        public const string GOG = "gog";
        public const string Itch = "itch";
        public const string Standalone = "standalone";
    }
}
