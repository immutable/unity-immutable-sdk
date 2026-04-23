#nullable enable

namespace Immutable.Audience
{
    // Which Immutable backend environment the SDK talks to. Set
    // explicitly on AudienceConfig — there is no auto-detection.
    // Defaults to Sandbox so accidental production traffic from a
    // misconfigured integration cannot happen; pin to Production
    // explicitly when shipping to live players.
    public enum AudienceEnvironment
    {
        // Public sandbox / testnet. Default for AudienceConfig so
        // first-run integrations cannot accidentally hit production.
        Sandbox,

        // Immutable internal development environment. Studios should
        // normally hit Sandbox or Production — this is for Immutable
        // engineers validating backend changes.
        Dev,

        // Production / mainnet. Studios must opt in explicitly.
        Production,
    }
}
