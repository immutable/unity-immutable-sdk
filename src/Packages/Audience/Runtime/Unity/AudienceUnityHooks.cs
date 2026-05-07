#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immutable.Audience.Unity.Mobile;
using UnityEngine;

namespace Immutable.Audience.Unity
{
    internal static class AudienceUnityHooks
    {
        // Captured at SubsystemRegistration so the Install Referrer provider
        // (called from ImmutableAudience.Init on whatever thread the user
        // invokes it from) can read it without touching
        // Application.persistentDataPath off the main thread.
        private static string? _persistentDataPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            ImmutableAudience.ResetState();

            // Avoid stacked subscriptions on reload.
            Application.quitting -= ImmutableAudience.Shutdown;
            Application.quitting += ImmutableAudience.Shutdown;

            _persistentDataPath = Application.persistentDataPath;
            ImmutableAudience.DefaultPersistentDataPathProvider = () => Application.persistentDataPath;

            // Captured once on main thread; ReadOnlyDictionary blocks downstream mutation.
            IReadOnlyDictionary<string, object> launchProps =
                new ReadOnlyDictionary<string, object>(DeviceCollector.CollectGameLaunchProperties());
            IReadOnlyDictionary<string, object> contextProps =
                new ReadOnlyDictionary<string, object>(DeviceCollector.CollectContext());
            ImmutableAudience.LaunchContextProvider = () => launchProps;
            ImmutableAudience.ContextProvider = () => contextProps;

#if UNITY_IOS && !UNITY_EDITOR
            ImmutableAudience.MobileAttributionProvider = () => SkanRegistration.RegisterIfFirstLaunch();
            ImmutableAudience.MobileAttributionContextProvider = () => AttributionContext.Capture();
            ImmutableAudience.TrackingAuthorizationRequestProvider = () => ATTBridge.RequestAsync();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            ImmutableAudience.MobileInstallReferrerProvider = ProvideInstallReferrer;
#endif

            UnityLifecycleBridge.EnsureExists();

            if (Log.Writer == null) Log.Writer = Debug.Log;
        }

        // Warms the install referrer cache for the next launch and returns
        // the currently cached value if any. Returns null on first launch
        // (cache miss while async fetch is in flight) or when the device
        // reports no referrer for this install. Exceptions propagate to
        // ImmutableAudience.Init's MobileInstallReferrerProviderThrew handler.
        private static string? ProvideInstallReferrer()
        {
            var path = _persistentDataPath;
            if (string.IsNullOrEmpty(path)) return null;

            InstallReferrerBridge.EnsureFetchStarted(path!);
            return InstallReferrerBridge.GetCachedInstallReferrer(path!);
        }
    }
}
