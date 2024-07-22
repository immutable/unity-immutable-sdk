#if UNITY_EDITOR_OSX

using UnityEngine;
using UnityEditor;
using AltTester.AltTesterUnitySDK.Editor;
using AltTester.AltTesterUnitySDK;
using System;

public class BuildScript
{
    static void BuildForAltTester()
    {
        try
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new string[] {
            "Assets/Scenes/SelectAuthMethod.unity", "Assets/Scenes/UnauthenticatedScene.unity", "Assets/Scenes/AuthenticatedScene.unity"
        };

            buildPlayerOptions.locationPathName = "Builds/MacOS/SampleApp.app";
            buildPlayerOptions.target = BuildTarget.StandaloneOSX;
            buildPlayerOptions.options = BuildOptions.Development | BuildOptions.IncludeTestAssemblies | BuildOptions.AutoRunPlayer;

            // Setup for AltTester
            var buildTargetGroup = BuildTargetGroup.Standalone;
            AltBuilder.AddAltTesterInScriptingDefineSymbolsGroup(buildTargetGroup);
            if (buildTargetGroup == UnityEditor.BuildTargetGroup.Standalone)
                AltBuilder.CreateJsonFileForInputMappingOfAxis();
            var instrumentationSettings = new AltInstrumentationSettings();
            AltBuilder.InsertAltInScene(buildPlayerOptions.scenes[0], instrumentationSettings);

            var results = BuildPipeline.BuildPlayer(buildPlayerOptions);
            AltBuilder.RemoveAltTesterFromScriptingDefineSymbols(BuildTargetGroup.Standalone);

        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

#endif
