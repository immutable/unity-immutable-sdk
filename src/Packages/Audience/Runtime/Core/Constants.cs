#nullable enable

namespace Immutable.Audience
{
    internal static class Constants
    {
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
        internal const string LibraryVersion = "0.2.2";
        internal const string Surface = "unity";
        internal const string ConsentSource = "UnitySDK";

        internal const string PublishableKeyHeader = "x-immutable-publishable-key";

        internal static string MessagesUrl(string? baseUrlOverride = null) =>
            BaseUrl(baseUrlOverride) + MessagesPath;
        internal static string ConsentUrl(string? baseUrlOverride = null) =>
            BaseUrl(baseUrlOverride) + ConsentPath;
        internal static string DataUrl(string? baseUrlOverride = null) =>
            BaseUrl(baseUrlOverride) + DataPath;

        internal static string BaseUrl(string? baseUrlOverride = null)
        {
            if (!string.IsNullOrEmpty(baseUrlOverride)) return baseUrlOverride!;
            return ProductionBaseUrl;
        }
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
