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

    // Strings emitted under MessageFields.IdentityType.
    internal static class IdentityTypeWireFormat
    {
        internal const string Passport = "passport";
        internal const string Steam = "steam";
        internal const string Epic = "epic";
        internal const string Google = "google";
        internal const string Apple = "apple";
        internal const string Discord = "discord";
        internal const string Email = "email";
        internal const string Custom = "custom";
    }

    internal static class IdentityTypeExtensions
    {
        // Throws on unknown casts. Every identify / alias event must carry an
        // identityType so downstream data-deletion requests can match records
        // to the correct identity namespace. An out-of-range cast must fail
        // loudly rather than ship an event with a missing or empty namespace.
        internal static string ToLowercaseString(this IdentityType type) => type switch
        {
            IdentityType.Passport => IdentityTypeWireFormat.Passport,
            IdentityType.Steam => IdentityTypeWireFormat.Steam,
            IdentityType.Epic => IdentityTypeWireFormat.Epic,
            IdentityType.Google => IdentityTypeWireFormat.Google,
            IdentityType.Apple => IdentityTypeWireFormat.Apple,
            IdentityType.Discord => IdentityTypeWireFormat.Discord,
            IdentityType.Email => IdentityTypeWireFormat.Email,
            IdentityType.Custom => IdentityTypeWireFormat.Custom,
            _ => throw new System.ArgumentOutOfRangeException(nameof(type), type,
                "Unknown IdentityType value; cast an out-of-range value."),
        };

        internal static IdentityType ParseLowercaseString(string? value) =>
            (value ?? string.Empty).ToLowerInvariant() switch
            {
                IdentityTypeWireFormat.Passport => IdentityType.Passport,
                IdentityTypeWireFormat.Steam => IdentityType.Steam,
                IdentityTypeWireFormat.Epic => IdentityType.Epic,
                IdentityTypeWireFormat.Google => IdentityType.Google,
                IdentityTypeWireFormat.Apple => IdentityType.Apple,
                IdentityTypeWireFormat.Discord => IdentityType.Discord,
                IdentityTypeWireFormat.Email => IdentityType.Email,
                _ => IdentityType.Custom,
            };
    }
}
