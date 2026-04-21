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

        internal const string LibraryName = "com.immutable.audience";
        internal const string Surface = "unity";
        internal const string ConsentSource = "UnitySDK";

        internal static string BaseUrl(string publishableKey) =>
            publishableKey != null && publishableKey.StartsWith(TestKeyPrefix)
                ? SandboxBaseUrl
                : ProductionBaseUrl;
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
