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
    // Pairs the consent level with the user id so the two always move
    // together. Updates swap the whole pair at once — a reader never sees
    // the new consent level alongside a leftover user id.
    internal sealed record ConsentState(ConsentLevel Level, string? UserId)
    {
        internal static readonly ConsentState None = new(ConsentLevel.None, null);
    }
}
