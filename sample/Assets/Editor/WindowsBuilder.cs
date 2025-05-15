using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AltTester.AltTesterUnitySDK.Editor;
using System;
using AltTester.AltTesterUnitySDK.Commands;
using AltTester.AltTesterUnitySDK.Driver;

public class WindowsBuilder
{
    private static readonly string DefaultBuildPath = $"Builds/Windows64/{Application.productName}.exe";

    static void Build()
    {
        BuildPlayer(DefaultBuildPath, BuildOptions.None);
    }

    static void BuildForAltTester()
    {
        BuildPlayer(DefaultBuildPath, BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.AutoRunPlayer, true);
    }

    private static void BuildPlayer(string defaultBuildPath, BuildOptions buildOptions, bool setupForAltTester = false)
    {
        try
        {
            string buildPath = GetBuildPathFromArgs(defaultBuildPath);
            BuildPlayerOptions buildPlayerOptions = CreateBuildPlayerOptions(buildPath, buildOptions);

            if (setupForAltTester)
            {
                SetupAltTester(buildPlayerOptions);
            }

            var results = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (setupForAltTester)
            {
                // Clean up AltTester settings after build
                AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(BuildTargetGroup.Standalone);
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

    private static BuildPlayerOptions CreateBuildPlayerOptions(string buildPath, BuildOptions buildOptions)
    {
        return new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Passport/SelectAuthMethod.unity",
                "Assets/Scenes/Passport/UnauthenticatedScene.unity",
                "Assets/Scenes/Passport/AuthenticatedScene.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmGetBalance.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmGetTransactionReceipt.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEvmSendTransaction.unity",
                "Assets/Scenes/Passport/Imx/ImxNftTransfer.unity",
                "Assets/Scenes/Passport/ZkEvm/ZkEVMSignTypedData.unity",
                "Assets/Scenes/Passport/Other/SetCallTimeout.unity"
            },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = buildOptions
        };
    }

    private static void SetupAltTester(BuildPlayerOptions buildPlayerOptions)
    {
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(BuildTargetGroup.Standalone);
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