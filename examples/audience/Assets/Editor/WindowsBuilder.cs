#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Invoked by CI via:
    //   Unity -batchmode -buildTarget StandaloneWindows64 \
    //         -executeMethod Immutable.Audience.Samples.SampleApp.Editor.WindowsBuilder.Build \
    //         -quit
    //
    // Optional CLI arg:
    //   --buildPath <path>   Output path for the exe (default: Builds/Windows/AudienceSample.exe)
    internal static class WindowsBuilder
    {
        private const string DefaultBuildPath = "Builds/Windows/AudienceSample.exe";

        public static void Build()
        {
            string buildPath = GetArgValue("--buildPath") ?? DefaultBuildPath;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/SampleApp/Scenes/SampleApp.unity" },
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            Debug.Log($"[WindowsBuilder] Building to: {buildPath}");

            // GameCI ubuntu uses the windows-mono image which has no IL2CPP support.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[WindowsBuilder] Build succeeded ({summary.totalSize / 1024 / 1024} MB).");
            }
            else
            {
                Debug.LogError($"[WindowsBuilder] Build failed: {summary.totalErrors} error(s).");
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
