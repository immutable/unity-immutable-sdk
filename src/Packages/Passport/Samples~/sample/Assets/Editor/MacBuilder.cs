#if UNITY_EDITOR_OSX

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using AltTester.AltTesterUnitySDK.Editor;
using AltTester.AltTesterUnitySDK;
using System;

public class MacBuilder
{
    private const string DefaultBuildPath = "Builds/MacOS/SampleApp.app";

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
                "Assets/Scenes/SelectAuthMethod.unity",
                "Assets/Scenes/UnauthenticatedScene.unity",
                "Assets/Scenes/AuthenticatedScene.unity",
                "Assets/Scenes/ZkEvmGetBalance.unity",
                "Assets/Scenes/ZkEvmGetTransactionReceipt.unity",
                "Assets/Scenes/ZkEvmSendTransaction.unity",
                "Assets/Scenes/ImxNftTransfer.unity"
            },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = buildOptions
        };
    }

    private static void SetupAltTester(BuildPlayerOptions buildPlayerOptions)
    {
        AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(BuildTargetGroup.Standalone);
        AltBuilder.CreateJsonFileForInputMappingOfAxis();

        var instrumentationSettings = new AltInstrumentationSettings();
        AltBuilder.InsertAltInScene(buildPlayerOptions.scenes[0], instrumentationSettings);
    }

    public static void RemoveAltFromScene(string scene)
    {
        Debug.Log("Removing AltTesterPrefab from the [" + scene + "] scene.");

        var sceneToModify = EditorSceneManager.OpenScene(scene);

        // Find the AltTesterPrefab instance in the scene
        var altRunner = GameObject.FindObjectOfType<AltRunner>();

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
