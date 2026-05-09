#nullable enable

using System;
using System.IO;
using Immutable.Audience.Samples.SampleApp;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immutable.Audience.Samples.SampleApp.Editor
{
    // Invoked by CI via:
    //   Unity -batchmode -buildTarget StandaloneLinux64 \
    //         -executeMethod Immutable.Audience.Samples.SampleApp.Editor.LinuxSmokeBuilder.Build \
    //         -quit
    //
    // Optional CLI arg:
    //   --buildPath <path>   Output path for the player (default: Builds/LinuxSmoke/LinuxSmokePlayer.x86_64)
    //
    // Produces a single-scene Linux player whose only behaviour is the
    // LinuxSmokeRunner MonoBehaviour. The scene is generated in memory at
    // build time to avoid shipping a hand-written .unity asset.
    internal static class LinuxSmokeBuilder
    {
        private const string DefaultBuildPath = "Builds/LinuxSmoke/LinuxSmokePlayer.x86_64";
        private const string TempScenePath = "Assets/_LinuxSmokeBuild.unity";

        public static void Build()
        {
            string buildPath = GetArgValue("--buildPath") ?? DefaultBuildPath;
            string scenePath = TempScenePath;

            try
            {
                CreateSmokeScene(scenePath);

                Directory.CreateDirectory(Path.GetDirectoryName(buildPath)!);

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = buildPath,
                    target = BuildTarget.StandaloneLinux64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None,
                };

                Debug.Log($"[LinuxSmokeBuilder] Building -> {buildPath}");
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[LinuxSmokeBuilder] Build succeeded ({summary.totalSize / 1024 / 1024} MB).");
                }
                else
                {
                    Debug.LogError($"[LinuxSmokeBuilder] Build failed: {summary.totalErrors} error(s).");
                    EditorApplication.Exit(1);
                }
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                {
                    AssetDatabase.DeleteAsset(scenePath);
                }
            }
        }

        private static void CreateSmokeScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("SmokeRunner");
            go.AddComponent<LinuxSmokeRunner>();

            EditorSceneManager.SaveScene(scene, scenePath);
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
