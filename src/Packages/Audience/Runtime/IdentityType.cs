#nullable enable

namespace Immutable.Audience
{
    /// <summary>
    /// Identity provider accepted by the Audience backend.
    /// </summary>
    /// <seealso cref="ImmutableAudience.Identify(string, IdentityType, System.Collections.Generic.Dictionary{string, object})"/>
    /// <seealso cref="ImmutableAudience.Alias(string, IdentityType, string, IdentityType)"/>
    public enum IdentityType
    {
        /// <summary>Immutable Passport.</summary>
        Passport,

        /// <summary>Steam.</summary>
        Steam,

        /// <summary>Epic Games Store.</summary>
        Epic,

        /// <summary>Google.</summary>
        Google,

        /// <summary>Apple.</summary>
        Apple,

        /// <summary>Discord.</summary>
        Discord,

        /// <summary>Email.</summary>
        Email,

        /// <summary>
        /// Studio-defined identity that does not match any of the other
        /// providers.
        /// </summary>
        Custom,
    }

    internal static class IdentityTypeExtensions
    {
        // Throws on unknown casts. Every identify / alias event must carry an
        // identityType so downstream data-deletion requests can match records
        // to the correct identity namespace. An out-of-range cast must fail
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

        internal static IdentityType ParseLowercaseString(string? value) =>
            (value ?? string.Empty).ToLowerInvariant() switch
            {
                "passport" => IdentityType.Passport,
                "steam" => IdentityType.Steam,
                "epic" => IdentityType.Epic,
                "google" => IdentityType.Google,
                "apple" => IdentityType.Apple,
                "discord" => IdentityType.Discord,
                "email" => IdentityType.Email,
                _ => IdentityType.Custom,
            };
    }
}
