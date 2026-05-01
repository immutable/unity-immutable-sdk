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
