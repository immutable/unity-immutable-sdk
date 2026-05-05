#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Invoked by CI via:
    //   Unity -batchmode -buildTarget Android \
    //         -executeMethod Immutable.Audience.Samples.SampleApp.Editor.AndroidBuilder.Build \
    //         -quit
    //
    // Optional CLI arg:
    //   --buildPath <path>   Output path for the APK (default: Builds/Android/audience.apk)
    internal static class AndroidBuilder
    {
        private const string DefaultBuildPath = "Builds/Android/audience.apk";

        public static void Build()
        {
            string buildPath = GetArgValue("--buildPath") ?? DefaultBuildPath;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SampleApp/Scenes/SampleApp.unity" },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"[AndroidBuilder] Building APK → {buildPath}");

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuilder] Build succeeded ({summary.totalSize / 1024 / 1024} MB).");
            }
            else
            {
                Debug.LogError($"[AndroidBuilder] Build failed: {summary.totalErrors} error(s).");
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
