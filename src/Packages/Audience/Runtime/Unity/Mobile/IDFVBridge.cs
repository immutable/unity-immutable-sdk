#nullable enable

using System;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace Immutable.Audience.Unity.Mobile
{
    internal static class IDFVBridge
    {
        // Replaceable in tests — captures NativeImpl by default.
        internal static Func<string?> Impl = NativeImpl;

        internal static string? GetIDFV() => Impl();

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern string _AudienceGetIDFV();

        private static string? NativeImpl() => _AudienceGetIDFV();
#else
        private static string? NativeImpl() => null;
#endif
    }
}
