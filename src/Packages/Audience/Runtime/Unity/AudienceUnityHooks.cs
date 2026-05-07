#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Immutable.Audience.Unity.Mobile;
using UnityEngine;

namespace Immutable.Audience.Unity
{
    internal static class AudienceUnityHooks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            ImmutableAudience.ResetState();

            // Avoid stacked subscriptions on reload.
            Application.quitting -= ImmutableAudience.Shutdown;
            Application.quitting += ImmutableAudience.Shutdown;

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
#endif

            UnityLifecycleBridge.EnsureExists();

            if (Log.Writer == null) Log.Writer = Debug.Log;
        }
    }
}
