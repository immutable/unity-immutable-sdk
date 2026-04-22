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
        // Returns null on unknown casts. The string overloads of Identify /
        // Alias check for null/empty and drop + warn, so an out-of-range
        // cast surfaces as a dropped event, not a corrupt wire payload.
        internal static string? ToLowercaseString(this IdentityType type) => type switch
        {
            IdentityType.Passport => "passport",
            IdentityType.Steam => "steam",
            IdentityType.Epic => "epic",
            IdentityType.Google => "google",
            IdentityType.Apple => "apple",
            IdentityType.Discord => "discord",
            IdentityType.Email => "email",
            IdentityType.Custom => "custom",
            _ => null,
        };
    }
}
