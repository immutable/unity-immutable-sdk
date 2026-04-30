#nullable enable

namespace Immutable.Audience
{
    /// <summary>
    /// How much data the Audience SDK is allowed to collect.
    /// </summary>
    /// <seealso cref="ImmutableAudience.SetConsent"/>
    public enum ConsentLevel
    {
        /// <summary>
        /// No tracking.
        /// </summary>
        None,

        /// <summary>
        /// Anonymous tracking. Events carry the device's anonymous ID. Identifying
        /// the player via
        /// <see cref="ImmutableAudience.Identify(string, IdentityType, System.Collections.Generic.Dictionary{string, object})"/>
        /// is rejected at this level.
        /// </summary>
        Anonymous,

        /// <summary>
        /// Full tracking. Events may carry a known user ID set via
        /// <see cref="ImmutableAudience.Identify(string, IdentityType, System.Collections.Generic.Dictionary{string, object})"/>.
        /// </summary>
        Full
    }

    internal static class ConsentLevelExtensions
    {
        internal static string ToLowercaseString(this ConsentLevel level) => level switch
        {
            ConsentLevel.None => "none",
            ConsentLevel.Anonymous => "anonymous",
            ConsentLevel.Full => "full",
            _ => throw new System.ArgumentOutOfRangeException(
                nameof(level), level, "Unhandled ConsentLevel"),
        };

        internal static bool CanTrack(this ConsentLevel level) => level != ConsentLevel.None;

        internal static bool CanIdentify(this ConsentLevel level) => level == ConsentLevel.Full;
    }
}
