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
}
