#nullable enable

namespace Immutable.Audience
{
    // Identity provider accepted by the Audience backend.
    public enum IdentityType
    {
        Passport,
        Steam,
        Epic,
        Google,
        Apple,
        Discord,
        Email,
        Custom,
    }

    internal static class IdentityTypeExtensions
    {
        // Throws on unknown casts. Every identify / alias event must carry an
        // identityType so downstream data-deletion requests can match records
        // to the correct identity namespace; an out-of-range cast must fail
        // loudly rather than ship an event with a missing or empty namespace.
        internal static string ToLowercaseString(this IdentityType type) => type switch
        {
            IdentityType.Passport => "passport",
            IdentityType.Steam => "steam",
            IdentityType.Epic => "epic",
            IdentityType.Google => "google",
            IdentityType.Apple => "apple",
            IdentityType.Discord => "discord",
            IdentityType.Email => "email",
            IdentityType.Custom => "custom",
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type,
                "Unknown IdentityType value; cast an out-of-range value."),
        };
    }
}
