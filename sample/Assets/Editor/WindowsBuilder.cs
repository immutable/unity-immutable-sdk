#if UNITY_EDITOR_WIN

using AltTester.AltTesterUnitySDK.Editor;
using AltTester.AltTesterUnitySDK;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class WindowsBuildScript
{
    static void BuildForAltTester()
    {
        try
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new string[] {
                "Assets/Scenes/SelectAuthMethod.unity",
                "Assets/Scenes/UnauthenticatedScene.unity",
                "Assets/Scenes/AuthenticatedScene.unity",
                "Assets/Scenes/ZkEvmGetBalance.unity",
                "Assets/Scenes/ZkEvmGetTransactionReceipt.unity",
                "Assets/Scenes/ZkEvmSendTransaction.unity",
                "Assets/Scenes/ImxNftTransfer.unity"
            };

            buildPlayerOptions.locationPathName = "Builds/Windows64/SampleApp.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.AutoRunPlayer;

            // Setup for AltTester
            var buildTargetGroup = BuildTargetGroup.Standalone;
            AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(buildTargetGroup);
            if (buildTargetGroup == UnityEditor.BuildTargetGroup.Standalone)
                AltBuilder.CreateJsonFileForInputMappingOfAxis();
            var instrumentationSettings = new AltInstrumentationSettings();
            AltBuilder.InsertAltInScene(buildPlayerOptions.scenes[0], instrumentationSettings);

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(BuildTargetGroup.Standalone);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
            }
            else
            {
                Debug.LogError("Build failed");
            }

        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

#endif
