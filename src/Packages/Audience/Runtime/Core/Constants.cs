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

        internal const string LibraryName = "com.immutable.audience";
        internal const string LibraryVersion = "0.1.0";
        internal const string Surface = "unity";
        internal const string ConsentSource = "UnitySDK";

        internal const string PublishableKeyHeader = "x-immutable-publishable-key";

        internal static string MessagesUrl(string publishableKey) => BaseUrl(publishableKey) + MessagesPath;
        internal static string ConsentUrl(string publishableKey) => BaseUrl(publishableKey) + ConsentPath;
        internal static string DataUrl(string publishableKey) => BaseUrl(publishableKey) + DataPath;

        internal static string BaseUrl(string publishableKey) =>
            publishableKey != null && publishableKey.StartsWith(TestKeyPrefix)
                ? SandboxBaseUrl
                : ProductionBaseUrl;
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
