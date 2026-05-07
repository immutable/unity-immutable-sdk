#nullable enable

using System;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace Immutable.Audience.Unity.Mobile
{
    internal static class SKANBridge
    {
        internal static Action Impl = NativeImpl;

        internal static void Register() => Impl();

#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _AudienceRegisterSKAN();

        private static void NativeImpl() => _AudienceRegisterSKAN();
#else
        private static void NativeImpl() { }
#endif
    }
}
