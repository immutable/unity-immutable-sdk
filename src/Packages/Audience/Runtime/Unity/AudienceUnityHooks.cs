#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immutable.Audience.Unity.Mobile;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace Immutable.Audience.Unity
{
    [Preserve]
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

            // Set before the collectors below run so a collector failure logs via Debug.Log, not Console.WriteLine.
            if (Log.Writer == null) Log.Writer = Debug.Log;
            if (Log.ErrorWriter == null) Log.ErrorWriter = Debug.LogError;

            // Captured once on main thread; ReadOnlyDictionary blocks downstream mutation.
            // Each collector is isolated so one throwing can't block the other's provider or abort Install().
            IReadOnlyDictionary<string, object> launchProps;
            try
            {
                launchProps = new ReadOnlyDictionary<string, object>(DeviceCollector.CollectGameLaunchProperties());
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.LaunchPropertiesCollectionFailed(ex));
                launchProps = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            }
            ImmutableAudience.LaunchContextProvider = () => launchProps;

            IReadOnlyDictionary<string, object> contextProps;
            try
            {
                contextProps = new ReadOnlyDictionary<string, object>(DeviceCollector.CollectContext());
            }
            catch (Exception ex)
            {
                Log.Warn(AudienceLogs.ContextCollectionFailed(ex));
                contextProps = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            }
            ImmutableAudience.ContextProvider = () => contextProps;

#if UNITY_IOS && !UNITY_EDITOR && AUDIENCE_MOBILE_ATTRIBUTION
            ImmutableAudience.MobileAttributionProvider = () => SkanRegistration.RegisterIfFirstLaunch();
            ImmutableAudience.MobileAttributionContextProvider = () => AttributionContext.Capture();
            ImmutableAudience.TrackingAuthorizationRequestProvider = () => ATTBridge.RequestAsync();
            ImmutableAudience.MobileATTStatusProvider = () => ATTBridge.GetStatus();
            ImmutableAudience.MobileIDFAProvider = () => ATTBridge.GetIDFA();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            ImmutableAudience.MobileInstallReferrerProvider = ProvideInstallReferrer;
#if AUDIENCE_MOBILE_ATTRIBUTION
            // Gated on the define so a build that disables GAID at compile
            // time can't read a stale cache file left over from a prior
            // install where the define was on.
            ImmutableAudience.MobileAttributionContextProvider = ProvideAndroidAttributionContext;
#endif
#endif

            UnityLifecycleBridge.EnsureExists();
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

#if UNITY_ANDROID && !UNITY_EDITOR && AUDIENCE_MOBILE_ATTRIBUTION
        // Kicks off a background GAID fetch for the next launch (Google
        // requires getAdvertisingIdInfo run off the main thread) and returns
        // whatever was cached by the previous launch. First launch returns
        // an empty dict; launch #2+ ships gaid + gaidLimitAdTracking.
        // Exceptions propagate to ImmutableAudience.Init's
        // MobileAttributionContextProviderThrew handler.
        private static IReadOnlyDictionary<string, object>? ProvideAndroidAttributionContext()
        {
            var path = _persistentDataPath;
            if (string.IsNullOrEmpty(path)) return AttributionContext.Capture();

            GAIDBridge.EnsureFetchStarted(path!);
            return AttributionContext.Capture(path);
        }
#endif
    }
}
