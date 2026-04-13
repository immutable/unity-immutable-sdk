namespace Immutable.Audience
{
    internal static class Constants
    {
        // Base URL derived from publishable key prefix -- no environment param exposed to studios.
        // Per the Product Environments RFC, Audience APIs will consolidate to a single endpoint.
        // When that ships, only this file changes -- no studio-facing interface change required.
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

    /// <summary>
    /// String constants for common game distribution platforms.
    /// Any string is accepted -- studios are not limited to these values.
    /// </summary>
    public static class DistributionPlatforms
    {
        public const string Steam = "steam";
        public const string Epic = "epic";
        public const string GOG = "gog";
        public const string Itch = "itch";
        public const string Standalone = "standalone";
    }
}
