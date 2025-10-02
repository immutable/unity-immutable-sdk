#if UNITY_6000_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using AltTester.AltTesterUnitySDK.Editor;
using System;
using System.Linq;
using AltTester.AltTesterUnitySDK.Commands;
using AltTester.AltTesterSDK.Driver;

/// <summary>
/// Unity 6+ builder that uses Build Profiles for macOS builds.
/// This builder loads the "macOS Profile" build profile asset and uses it to build.
/// </summary>
public class MacBuilderUnity6
{
    private const string DefaultBuildPath = "Builds/MacOS/Sample Unity 6 macOS.app";
    private const string BuildProfilePath = "Assets/Settings/Build Profiles/macOS Profile.asset";

    static void Build()
    {
        BuildPlayer(DefaultBuildPath, false);
    }

    static void BuildForAltTester()
    {
        BuildPlayer(DefaultBuildPath, true);
    }

    private static void BuildPlayer(string defaultBuildPath, bool setupForAltTester = false)
    {
        try
        {
            string buildPath = GetBuildPathFromArgs(defaultBuildPath);
            
            // Load the Build Profile
            BuildProfile buildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(BuildProfilePath);
            if (buildProfile == null)
            {
                Debug.LogError($"Build Profile not found at path: {BuildProfilePath}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Using Build Profile: {buildProfile.name}");

            // Get scenes from the build profile or use default scenes
            string[] scenes = GetScenesToBuild(buildProfile);

            if (setupForAltTester)
            {
                SetupAltTester(scenes);
            }

            // Build using the Build Profile
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                locationPathName = buildPath,
                scenes = scenes
            };

            if (setupForAltTester)
            {
                options.options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.AutoRunPlayer;
            }

            var result = BuildPipeline.BuildPlayer(options, buildProfile);

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

    private static string[] GetScenesToBuild(BuildProfile buildProfile)
    {
        // Check if the build profile has custom scenes configured
        if (buildProfile.scenes != null && buildProfile.scenes.Length > 0)
        {
            var sceneAssets = buildProfile.scenes;
            return sceneAssets.Select(scene => AssetDatabase.GetAssetPath(scene)).ToArray();
        }

        // Otherwise, use the default scenes
        return new[]
        {
            "Assets/Scenes/Passport/Initialisation.unity",
            "Assets/Scenes/Passport/UnauthenticatedScene.unity",
            "Assets/Scenes/Passport/AuthenticatedScene.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmGetBalance.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmGetTransactionReceipt.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEvmSendTransaction.unity",
            "Assets/Scenes/Passport/Imx/ImxNftTransfer.unity",
            "Assets/Scenes/Passport/ZkEvm/ZkEVMSignTypedData.unity",
            "Assets/Scenes/Passport/Other/SetCallTimeout.unity"
        };
    }

    private static void SetupAltTester(string[] scenes)
    {
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(BuildTargetGroup.Standalone);

        // Add custom define for e2e testing to enable default browser behavior
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
#endif

