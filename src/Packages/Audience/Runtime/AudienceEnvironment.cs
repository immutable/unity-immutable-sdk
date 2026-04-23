#nullable enable

namespace Immutable.Audience
{
    // Which Immutable backend environment the SDK talks to. Defaults to
    // Auto, which picks sandbox for pk_imapik-test- keys and production
    // for everything else. Set explicitly to target the Immutable internal
    // dev environment or to override the key-based default (e.g. staging
    // a test key against production infra during a regression sweep).
    public enum AudienceEnvironment
    {
        // Derive from the publishable key's prefix. Keys starting with
        // pk_imapik-test- hit sandbox; everything else hits production.
        // Matches the SDK's pre-environment behaviour, so existing
        // integrations do not have to set AudienceConfig.Environment.
        Auto = 0,

        // Immutable internal development environment. Studios should
        // normally hit Sandbox or Production — this is for Immutable
        // engineers validating backend changes.
        Dev = 1,

        // Public sandbox / testnet. Pair with a pk_imapik-test-
        // publishable key.
        Sandbox = 2,

        // Production / mainnet. Pair with a non-test publishable key.
        Production = 3,
    }
}
