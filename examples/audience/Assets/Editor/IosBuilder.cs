#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Invoked by CI via:
    //   Unity -batchmode -buildTarget iOS \
    //         -executeMethod Immutable.Audience.Samples.SampleApp.Editor.IosBuilder.Build \
    //         -quit
    //
    // Optional CLI arg:
    //   --buildPath <path>   Output directory for the Xcode project (default: Builds/iOS)
    internal static class IosBuilder
    {
        private const string DefaultBuildPath = "Builds/iOS";

        public static void Build()
        {
            string buildPath = GetArgValue("--buildPath") ?? DefaultBuildPath;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SampleApp/Scenes/SampleApp.unity" },
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            Debug.Log($"[IosBuilder] Building Xcode project → {buildPath}");

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[IosBuilder] Build succeeded ({summary.totalSize / 1024 / 1024} MB).");
            }
            else
            {
                Debug.LogError($"[IosBuilder] Build failed: {summary.totalErrors} error(s).");
                EditorApplication.Exit(1);
            }
        }

        private static string? GetArgValue(string flag)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                    return args[i + 1];
            }
            return null;
        }
    }
}
