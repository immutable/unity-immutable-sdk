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

            if (Log.Writer == null) Log.Writer = Debug.Log;
        }
    }
}
