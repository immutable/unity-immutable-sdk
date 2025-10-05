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
/// Unity 6+ builder for macOS builds using Build Profiles.
/// Ensures TextMeshPro Settings are available for AltTester UI components.
/// This class is only available in Unity 6.0.0 and newer due to Build Profile API requirements.
/// </summary>
public class MacBuilderUnity6
{
    private const string DefaultBuildPath = "Builds/MacOS/Sample Unity 6 macOS.app";
    private const string BuildProfilePath = "Assets/Settings/Build Profiles/macOS Profile.asset";

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
                options = setupForAltTester ? (BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.AutoRunPlayer) : BuildOptions.None
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
                Debug.Log("=== Cleaning up AltTester settings after build ===");
                
                // Clean up AltTester settings after build
                Debug.Log("Removing AltTester from scripting define symbols...");
                AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(BuildTargetGroup.Standalone);

                // Clean up custom e2e testing define
                Debug.Log("Removing IMMUTABLE_E2E_TESTING define symbol...");
                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                defineSymbols = defineSymbols.Replace("IMMUTABLE_E2E_TESTING;", "").Replace(";IMMUTABLE_E2E_TESTING", "").Replace("IMMUTABLE_E2E_TESTING", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
                
                var cleanedDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                Debug.Log($"Cleaned scripting define symbols: {cleanedDefineSymbols}");

                RemoveAltFromScene(scenes[0]);
                Debug.Log("✅ AltTester cleanup completed");
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

    private static void ValidateBuildProfile(BuildProfile buildProfile)
    {
        Debug.Log($"Build Profile Debug Info:");
        Debug.Log($"  - Name: {buildProfile.name}");
        
        // Use reflection to get available properties since API might vary
        var type = buildProfile.GetType();
        Debug.Log($"  - Type: {type.Name}");
        
        // Try to get common BuildProfile properties
        try
        {
            var buildTargetProperty = type.GetProperty("buildTarget");
            if (buildTargetProperty != null)
            {
                Debug.Log($"  - Build Target: {buildTargetProperty.GetValue(buildProfile)}");
            }
            
            var subtargetProperty = type.GetProperty("subtarget");
            if (subtargetProperty != null)
            {
                Debug.Log($"  - Subtarget: {subtargetProperty.GetValue(buildProfile)}");
            }
            
            var platformIdProperty = type.GetProperty("platformId");
            if (platformIdProperty != null)
            {
                Debug.Log($"  - Platform ID: {platformIdProperty.GetValue(buildProfile)}");
            }
            
            var overrideGlobalSceneListProperty = type.GetProperty("overrideGlobalSceneList");
            if (overrideGlobalSceneListProperty != null)
            {
                Debug.Log($"  - Override Global Scene List: {overrideGlobalSceneListProperty.GetValue(buildProfile)}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"  - Could not access some BuildProfile properties: {ex.Message}");
        }
        
        Debug.Log($"  - Scenes Count: {(buildProfile.scenes?.Length ?? 0)}");
        
        if (buildProfile.scenes != null && buildProfile.scenes.Length > 0)
        {
            Debug.Log($"  - Scenes:");
            for (int i = 0; i < buildProfile.scenes.Length; i++)
            {
                Debug.Log($"    [{i}] {buildProfile.scenes[i]}");
            }
        }
        else
        {
            Debug.LogWarning("  - Build Profile has no scenes configured");
        }
        
        Debug.Log($"  - Scripting Defines Count: {(buildProfile.scriptingDefines?.Length ?? 0)}");
        if (buildProfile.scriptingDefines != null && buildProfile.scriptingDefines.Length > 0)
        {
            Debug.Log($"  - Scripting Defines: {string.Join(", ", buildProfile.scriptingDefines)}");
        }

        // Check symlink status
        Debug.Log($"Symlink Status Check:");
        string scenesPath = "Assets/Scenes";
        if (System.IO.Directory.Exists(scenesPath))
        {
            Debug.Log($"  - Scenes directory exists: {scenesPath}");
            try
            {
                var scenesDir = new System.IO.DirectoryInfo(scenesPath);
                if (scenesDir.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint))
                {
                    // Use reflection for LinkTarget since it might not be available on all platforms
                    var linkTargetProperty = scenesDir.GetType().GetProperty("LinkTarget");
                    if (linkTargetProperty != null)
                    {
                        Debug.Log($"  - Scenes is a symlink: {linkTargetProperty.GetValue(scenesDir)}");
                    }
                    else
                    {
                        Debug.Log($"  - Scenes is a symlink (LinkTarget property not available)");
                    }
                }
                else
                {
                    Debug.Log($"  - Scenes is a regular directory");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"  - Could not check symlink status: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"  - Scenes directory does not exist: {scenesPath}");
        }
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
        Debug.Log("=== Setting up AltTester for build ===");
        
        // Add AltTester scripting define symbols
        Debug.Log("Adding AltTester to scripting define symbols...");
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(BuildTargetGroup.Standalone);
        
        var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        Debug.Log($"Current scripting define symbols: {defineSymbols}");

        // Add custom define for e2e testing to enable default browser behavior
        if (!defineSymbols.Contains("IMMUTABLE_E2E_TESTING"))
        {
            Debug.Log("Adding IMMUTABLE_E2E_TESTING define symbol...");
            defineSymbols += ";IMMUTABLE_E2E_TESTING";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, defineSymbols);
            defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            Debug.Log($"Updated scripting define symbols: {defineSymbols}");
        }
        else
        {
            Debug.Log("IMMUTABLE_E2E_TESTING define symbol already present");
        }

        Debug.Log("Creating JSON file for input mapping of axis...");
        AltBuilder.CreateJsonFileForInputMappingOfAxis();

        var instrumentationSettings = new AltInstrumentationSettings();
        var host = System.Environment.GetEnvironmentVariable("ALTSERVER_HOST");
        if (!string.IsNullOrEmpty(host))
        {
            Debug.Log($"Using custom AltServer host from environment: {host}");
            instrumentationSettings.AltServerHost = host;
        }
        else
        {
            Debug.Log($"Using default AltServer host: {instrumentationSettings.AltServerHost}");
        }

        var port = System.Environment.GetEnvironmentVariable("ALTSERVER_PORT");
        if (!string.IsNullOrEmpty(port))
        {
            Debug.Log($"Using custom AltServer port from environment: {port}");
            instrumentationSettings.AltServerPort = int.Parse(port);
        }
        else
        {
            instrumentationSettings.AltServerPort = 13000;
            Debug.Log($"Using default AltServer port: 13000");
        }
        
        instrumentationSettings.ResetConnectionData = true;
        Debug.Log($"AltTester instrumentation settings: Host={instrumentationSettings.AltServerHost}, Port={instrumentationSettings.AltServerPort}, ResetConnectionData={instrumentationSettings.ResetConnectionData}");
        
        Debug.Log($"Inserting AltTester prefab into first scene: {scenes[0]}");
        AltBuilder.InsertAltInScene(scenes[0], instrumentationSettings);
        Debug.Log("✅ AltTester setup completed successfully");
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

