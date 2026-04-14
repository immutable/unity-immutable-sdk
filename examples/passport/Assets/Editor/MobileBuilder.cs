using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AltTester.AltTesterUnitySDK.Editor;
using System;
using AltTester.AltTesterUnitySDK.Commands;
using AltTester.AltTesterSDK.Driver;

public class MobileBuilder
{
    private const string DefaultAndroidBuildPath = "Builds/Android/SampleApp.apk";
    private const string DefaultiOSBuildPath = "Builds/iOS";

    static void Build()
    {
        var platform = GetPlatformFromArgs();
        string defaultBuildPath = platform == BuildTarget.Android ? DefaultAndroidBuildPath : DefaultiOSBuildPath;
        BuildPlayer(defaultBuildPath, BuildOptions.Development, platform);
    }

    static void BuildForAltTester()
    {
        var platform = GetPlatformFromArgs();
        string defaultBuildPath = platform == BuildTarget.Android ? DefaultAndroidBuildPath : DefaultiOSBuildPath;
        BuildPlayer(defaultBuildPath, BuildOptions.Development | BuildOptions.IncludeTestAssemblies, platform, true);
    }

    private static void BuildPlayer(string defaultBuildPath, BuildOptions buildOptions, BuildTarget platform, bool setupForAltTester = false)
    {
        try
        {
            string buildPath = GetBuildPathFromArgs(defaultBuildPath);

            if (platform == BuildTarget.iOS)
            {
                string bundleIdentifier = GetBundleIdentifierFromArgs();
                PlayerSettings.applicationIdentifier = bundleIdentifier;
            }

            BuildPlayerOptions buildPlayerOptions = CreateBuildPlayerOptions(buildPath, buildOptions, platform);

            if (setupForAltTester)
            {
                SetupAltTester(buildPlayerOptions, platform);
            }

            var results = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (setupForAltTester)
            {
                // Clean up AltTester settings after build
                AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(platform == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS);
                RemoveAltFromScene(buildPlayerOptions.scenes[0]);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
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

    private static BuildTarget GetPlatformFromArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--platform" && i + 1 < args.Length)
            {
                return args[i + 1].Equals("iOS", StringComparison.OrdinalIgnoreCase) ? BuildTarget.iOS : BuildTarget.Android;
            }
        }
        return BuildTarget.Android; // Default to Android if no platform is specified
    }

    private static string GetBundleIdentifierFromArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--bundleIdentifier" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return "com.immutable.Immutable-Sample";
    }

    private static string? GetAltTesterHostFromArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--host" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static BuildPlayerOptions CreateBuildPlayerOptions(string buildPath, BuildOptions buildOptions, BuildTarget platform)
    {
        return new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Passport/Initialisation.unity",
                "Assets/Scenes/Passport/UnauthenticatedScene.unity",
                "Assets/Scenes/Passport/AuthenticatedScene.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmGetBalance.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmGetTransactionReceipt.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmSendTransaction.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEVMSignTypedData.unity",
                "Assets/Scenes/Passport/Other/SetCallTimeout.unity"
            },
            locationPathName = buildPath,
            target = platform,
            options = buildOptions
        };
    }

    private static void SetupAltTester(BuildPlayerOptions buildPlayerOptions, BuildTarget platform)
    {
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(platform == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS);
        AltBuilder.CreateJsonFileForInputMappingOfAxis();

        var instrumentationSettings = new AltInstrumentationSettings();
        var host = System.Environment.GetEnvironmentVariable("ALTSERVER_HOST") ?? GetAltTesterHostFromArgs();
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
        AltBuilder.InsertAltInScene(buildPlayerOptions.scenes[0], instrumentationSettings);
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
