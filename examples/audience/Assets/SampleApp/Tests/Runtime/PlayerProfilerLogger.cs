#nullable enable

#if UNITY_STANDALONE_LINUX
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace Immutable.Audience.Samples.SampleApp.Tests
{
    // Linux PlayMode test player profiler hook.
    //
    // The editor profile we captured on PR #764 only covered the editor
    // process. The actual test loop runs in a separate PlayerWithTests
    // subprocess, which never had a profiler attached. This hook plugs
    // that gap from inside the player itself.
    //
    // Behaviour: at BeforeSceneLoad, reads AUDIENCE_PLAYER_PROFILE_PATH
    // from the player process env. When set, points
    // UnityEngine.Profiling.Profiler at that path and starts a binary
    // log of every captured frame. Output can be loaded into Unity
    // Editor: Window > Analysis > Profiler > Load Profile.
    //
    // Engages only on StandaloneLinux64 builds (gated by the #if) and
    // only when the env var is set (gated at runtime). Local dev builds
    // and other-platform CI runs are unaffected.
    //
    // Note: this enables regular profiling, not deep profiling. Deep
    // profiling is set at editor build time via the -deepprofiling
    // command line flag and cannot be toggled from runtime code. Regular
    // profiling still surfaces per-frame CPU breakdowns and the function
    // hot list, which is what we need to identify the per-frame UI
    // Toolkit cost (or whatever else is eating ~37 sec per test).
    public static class PlayerProfilerLogger
    {
        private const string PathEnvVar = "AUDIENCE_PLAYER_PROFILE_PATH";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var path = Environment.GetEnvironmentVariable(PathEnvVar);
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Profiler.logFile = path;
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;

                Debug.Log($"[PlayerProfilerLogger] Profiler binary log enabled. Output: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerProfilerLogger] Failed to enable profiler at {path}: {ex.Message}");
            }
        }
    }
}
#endif
