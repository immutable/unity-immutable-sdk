using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Audience.Unity
{
    internal static class AudienceUnityHooks
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            // Clear surviving statics before re-wiring in case "disable domain reload" kept them alive.
            ImmutableAudience.ResetState();

            // -= then += so repeat SubsystemRegistration cycles don't stack subscriptions.
            Application.quitting -= ImmutableAudience.Shutdown;
            Application.quitting += ImmutableAudience.Shutdown;

            ImmutableAudience.DefaultPersistentDataPathProvider = () => Application.persistentDataPath;
            ImmutableAudience.LaunchContextProvider = BuildLaunchContext;

            if (Log.Writer == null) Log.Writer = Debug.Log;
        }

        private static Dictionary<string, object> BuildLaunchContext() =>
            new Dictionary<string, object>
            {
                ["platform"] = Application.platform.ToString(),
                ["version"] = Application.version,
                ["buildGuid"] = Application.buildGUID,
                ["unityVersion"] = Application.unityVersion,
            };
    }
}
