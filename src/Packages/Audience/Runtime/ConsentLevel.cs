#nullable enable

namespace Immutable.Audience
{
    // How much data the Audience SDK is allowed to collect.
    public enum ConsentLevel
    {
        // No tracking
        None,
        // Anonymous tracking only
        Anonymous,
        // Full tracking
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
