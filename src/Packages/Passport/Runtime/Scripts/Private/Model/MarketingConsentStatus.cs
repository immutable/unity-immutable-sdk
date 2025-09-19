using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Enum representing marketing consent status.
    /// </summary>
    [Serializable]
    public enum MarketingConsentStatus
    {
        OptedIn,
        Unsubscribed
    }

    /// <summary>
    /// Extension methods for MarketingConsentStatus enum.
    /// </summary>
    public static class MarketingConsentStatusExtensions
    {
        /// <summary>
        /// Converts the enum value to the string format expected by the game bridge.
        /// </summary>
        /// <param name="status">The marketing consent status</param>
        /// <returns>The corresponding string value</returns>
        public static string ToApiString(this MarketingConsentStatus status)
        {
            return status switch
            {
                MarketingConsentStatus.OptedIn => "opted_in",
                MarketingConsentStatus.Unsubscribed => "unsubscribed",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown MarketingConsentStatus value")
            };
        }
    }
}
