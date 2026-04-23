#nullable enable

namespace System.Runtime.CompilerServices
{
    // Polyfill: record { init } properties compile to init-only setters that
    // reference IsExternalInit. .NET Standard 2.1 (Unity) doesn't ship it.
    internal static class IsExternalInit { }
}

namespace Immutable.Audience
{
    // Immutable consent + userId pair. Stored as a volatile reference so
    // readers observe both fields atomically — a SetConsent(Full → Anonymous)
    // swap cannot be observed as Anonymous+oldUserId.
    internal sealed record ConsentState(ConsentLevel Level, string? UserId)
    {
        internal static readonly ConsentState None = new(ConsentLevel.None, null);
    }
}
