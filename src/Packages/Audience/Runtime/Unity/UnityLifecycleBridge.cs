#nullable enable

using UnityEngine;

namespace Immutable.Audience.Unity
{
    internal sealed class UnityLifecycleBridge : MonoBehaviour
    {
        // Volatile: SubsystemRegistration reset vs EnsureExists fence.
        private static volatile UnityLifecycleBridge? _instance;
        private PerformanceCollector? _perf;

        // Drop stale GameObject pointer after Fast Enter Play Mode.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        internal static void EnsureExists()
        {
            if (_instance != null) return;

            var go = new GameObject("[ImmutableAudience.LifecycleBridge]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UnityLifecycleBridge>();
        }

        internal static void SetPerformanceCollector(PerformanceCollector? collector)
        {
            var instance = _instance;
            if (instance != null) instance._perf = collector;
        }

        private void Update()
        {
            _perf?.RecordFrame();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ImmutableAudience.OnPause();
            else ImmutableAudience.OnResume();
        }

#if !UNITY_ANDROID && !UNITY_IOS
        // Desktop only — mobile focus events fire spuriously (soft keyboard, notifications).
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ImmutableAudience.OnPause();
            else ImmutableAudience.OnResume();
        }
#endif

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
