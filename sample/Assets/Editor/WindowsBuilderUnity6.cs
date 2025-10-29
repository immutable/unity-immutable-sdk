#if UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using AltTester.AltTesterUnitySDK.Editor;
using System;
using System.Linq;
using AltTester.AltTesterUnitySDK.Commands;
using AltTester.AltTesterSDK.Driver;
using TMPro;

/// <summary>
/// Unity 6+ builder for Windows builds using Build Profiles.
/// Ensures TextMeshPro Settings are available for AltTester UI components.
/// This class is only available in Unity 6.0.0 and newer due to Build Profile API requirements.
/// </summary>
public class WindowsBuilderUnity6
{
    private const string DefaultBuildPath = "Builds/Windows64/Sample Unity 6 Windows.exe";
    private const string BuildProfilePath = "Assets/Settings/Build Profiles/Windows Profile.asset";

    public static void Build()
    {
        BuildPlayer(DefaultBuildPath, false);
    }

    public static void BuildForAltTester()
    {
        BuildPlayer(DefaultBuildPath, true);
    }

    private static void BuildPlayer(string defaultBuildPath, bool setupForAltTester = false)
    {
        try
        {
            string buildPath = GetBuildPathFromArgs(defaultBuildPath);

            // Get scenes from the build profile or use default scenes
            string[] scenes = GetScenesToBuild(setupForAltTester);

            if (setupForAltTester)
            {
                Debug.Log("🧪 Building with AltTester support enabled");
                SetupAltTester(scenes);
            }
            else
            {
                Debug.Log("Building without AltTester (regular build)");
            }

            BuildProfile buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(BuildProfilePath);
            if (buildProfile == null)
            {
                Debug.LogError($"Build Profile not found at path: {BuildProfilePath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Using Build Profile: {buildProfile.name}");

            BuildPlayerWithProfileOptions options = new BuildPlayerWithProfileOptions
            {
                buildProfile = buildProfile,
                locationPathName = buildPath,
                options = setupForAltTester ? (BuildOptions.Development | BuildOptions.IncludeTestAssemblies) : BuildOptions.None
            };

            Debug.Log($"Build options: {options.options}");
            Debug.Log($"Starting build to: {buildPath}");

            var result = BuildPipeline.BuildPlayer(options);

            if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {result.summary.totalSize} bytes");
            }
            else
            {
                Debug.LogError($"Build failed: {result.summary.result}");
                EditorApplication.Exit(1);
            }


            if (setupForAltTester)
            {
                // Clean up AltTester settings after build
                AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(BuildTargetGroup.Standalone);

                // Clean up custom e2e testing define
                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                defineSymbols = defineSymbols.Replace("IMMUTABLE_E2E_TESTING;", "").Replace(";IMMUTABLE_E2E_TESTING", "").Replace("IMMUTABLE_E2E_TESTING", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);

                var cleanedDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

                RemoveAltFromScene(scenes[0]);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static string GetBuildPathFromArgs(string defaultBuildPath)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--buildPath" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return defaultBuildPath;
    }

    private static string[] GetScenesToBuild(bool setupForAltTester = false)
    {
        return new[]
        {
            "Assets/Scenes/Passport/Initialisation.unity",
            "Assets/Scenes/Passport/UnauthenticatedScene.unity",
            "Assets/Scenes/Passport/AuthenticatedScene.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmGetBalance.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmGetTransactionReceipt.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmSendTransaction.unity",
            "Assets/Scenes/Passport/Imx/ImxNftTransfer.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmSignTypedData.unity",
            "Assets/Scenes/Passport/Other/SetCallTimeout.unity"
        };
    }

    private static void SetupAltTester(string[] scenes)
    {
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(BuildTargetGroup.Standalone);

        var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (!defineSymbols.Contains("IMMUTABLE_E2E_TESTING"))
        {
            defineSymbols += ";IMMUTABLE_E2E_TESTING";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
        }

        AltBuilder.CreateJsonFileForInputMappingOfAxis();

        var instrumentationSettings = new AltInstrumentationSettings();
        var host = System.Environment.GetEnvironmentVariable("ALTSERVER_HOST");
        if (!string.IsNullOrEmpty(host))
        {
            instrumentationSettings.AltServerHost = host;
        }

        var port = System.Environment.GetEnvironmentVariable("ALTSERVER_PORT");
        if (!string.IsNullOrEmpty(port))
        {
            instrumentationSettings.AltServerPort = int.Parse(port);
        }
        else
        {
            instrumentationSettings.AltServerPort = 13000;
        }

        instrumentationSettings.ResetConnectionData = true;
        AltBuilder.InsertAltInScene(scenes[0], instrumentationSettings);
    }

    public static void RemoveAltFromScene(string scene)
    {
        Debug.Log("Removing AltTesterPrefab from the [" + scene + "] scene.");

        var sceneToModify = EditorSceneManager.OpenScene(scene);

        // Find the AltTesterPrefab instance in the scene
        var altRunner = GameObject.FindFirstObjectByType<AltRunner>();

        if (altRunner != null)
        {
            // Destroy the AltTesterPrefab instance
            GameObject.DestroyImmediate(altRunner.gameObject);

            // Mark the scene as dirty and save it
            EditorSceneManager.MarkSceneDirty(sceneToModify);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("AltTesterPrefab successfully removed from the [" + scene + "] scene.");
        }
        else
        {
            Debug.LogWarning("AltTesterPrefab was not found in the [" + scene + "] scene.");
        }
    }
}
#endif // UNITY_6000_0_OR_NEWER

