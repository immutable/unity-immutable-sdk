#nullable enable

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Build pre-process hook that restricts the StandaloneLinux64 player to
    // OpenGLCore when AUDIENCE_LINUX_GLCORE_ONLY is set.
    //
    // Why: the unityci/editor Linux container has no GPU. Unity falls back
    // to Mesa software OpenGL via llvmpipe. The runtime -force-glcore flag
    // picks OpenGL when the player launches, but the build still ships
    // every active graphics API's shader variants. With Vulkan also active
    // by default, the shader compiler emits both glcore and vulkan
    // variants. Roughly half of the 413 shader compiles measured on Unity
    // 6 Linux were wasted on Vulkan variants the player never used.
    //
    // The hook runs only when the env flag is set, only for the
    // StandaloneLinux64 build target, and only modifies in-memory
    // PlayerSettings during the build. Other Standalone targets (Win, Mac)
    // and other CI workflows are unaffected. Local builds without the env
    // var see no change.
    //
    // Usage:
    //   AUDIENCE_LINUX_GLCORE_ONLY=1 Unity -batchmode -buildTarget StandaloneLinux64 -runTests ...
    internal sealed class GraphicsApisLinuxOverride : IPreprocessBuildWithReport
    {
        private const string EnvVar = "AUDIENCE_LINUX_GLCORE_ONLY";

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneLinux64) return;

            var requested = Environment.GetEnvironmentVariable(EnvVar);
            if (string.IsNullOrEmpty(requested)) return;

            var current = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneLinux64);
            if (current.Length == 1 && current[0] == GraphicsDeviceType.OpenGLCore)
            {
                Debug.Log($"[{nameof(GraphicsApisLinuxOverride)}] StandaloneLinux64 already at OpenGLCore only.");
                return;
            }

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.OpenGLCore });
            Debug.Log($"[{nameof(GraphicsApisLinuxOverride)}] StandaloneLinux64 graphics APIs forced to OpenGLCore. Vulkan shader variants will be skipped.");
        }
    }
}
