namespace Immutable.Audience
{
    // How much data the Audience SDK is allowed to collect.
    public enum ConsentLevel
    {
        // No tracking.
        None,
        // Anonymous tracking only.
        Anonymous,
        // Full tracking, including identity.
        Full
    }

    internal static class ConsentLevelExtensions
    {
        // Throws on unknown casts rather than emitting null: a null value
        // would poison the backend consent log.
        internal static string ToLowercaseString(this ConsentLevel level) => level switch
        {
            ConsentLevel.None => "none",
            ConsentLevel.Anonymous => "anonymous",
            ConsentLevel.Full => "full",
            _ => throw new System.ArgumentOutOfRangeException(
                nameof(level), level, "Unhandled ConsentLevel"),
        };
    }
}
