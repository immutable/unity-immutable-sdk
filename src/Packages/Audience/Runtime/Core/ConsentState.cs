#nullable enable

namespace System.Runtime.CompilerServices
{
    // Unity's .NET runtime doesn't include this type, but the C# compiler
    // needs it to exist to build the ConsentState record below. Declaring
    // an empty one here gives the compiler what it looks for.
    internal static class IsExternalInit { }
}

namespace Immutable.Audience
{
    // Pairs the consent level with the user id and identity type so all three
    // always move together. Updates swap the whole set at once. A reader
    // never sees the new consent level alongside a leftover user id or
    // identity type. IdentityType has no default: every construction site
    // must state it explicitly so a future caller can't silently drop it.
    internal sealed record ConsentState(ConsentLevel Level, string? UserId, IdentityType? IdentityType)
    {
        internal static readonly ConsentState None = new(ConsentLevel.None, null, null);
    }
}
