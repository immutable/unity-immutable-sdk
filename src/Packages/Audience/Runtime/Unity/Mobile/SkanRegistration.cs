#nullable enable

using System;
using UnityEngine;

namespace Immutable.Audience.Unity.Mobile
{
    internal static class SkanRegistration
    {
        private const string PrefsKey = "ImmutableAudience.skan_registered";

        // Replaceable in tests.
        internal static Func<bool> HasRegistered = DefaultHasRegistered;
        internal static Action MarkRegistered = DefaultMarkRegistered;

        // Returns true on first registration (SKAN was called), null if already done or N/A.
        internal static bool? RegisterIfFirstLaunch()
        {
            if (HasRegistered()) return null;
            SKANBridge.Register();
            MarkRegistered();
            return true;
        }

        private static bool DefaultHasRegistered() => PlayerPrefs.HasKey(PrefsKey);

        private static void DefaultMarkRegistered()
        {
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
        }
    }
}
