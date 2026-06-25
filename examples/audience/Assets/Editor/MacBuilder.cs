#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Invoked by CI via:
    //   Unity -batchmode -buildTarget StandaloneOSX \
    //         -executeMethod Immutable.Audience.Samples.SampleApp.Editor.MacBuilder.Build \
    //         -quit
    //
    // Optional CLI arg:
    //   --buildPath <path>   Output path for the .app (default: Builds/macOS/AudienceSample.app)
    internal static class MacBuilder
    {
        private const string DefaultBuildPath = "Builds/macOS/AudienceSample.app";

        public static void Build()
        {
            string buildPath = GetArgValue("--buildPath") ?? DefaultBuildPath;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SampleApp/Scenes/SampleApp.unity" },
                locationPathName = buildPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            Debug.Log($"[MacBuilder] Building to: {buildPath}");

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[MacBuilder] Build succeeded ({summary.totalSize / 1024 / 1024} MB).");
            }
            else
            {
                Debug.LogError($"[MacBuilder] Build failed: {summary.totalErrors} error(s).");
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
