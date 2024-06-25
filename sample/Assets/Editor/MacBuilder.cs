#if UNITY_EDITOR_OSX

using UnityEngine;
using UnityEditor;

public class BuildScript
{
    [MenuItem("Build/Build MacOS")]
    public static void BuildMacOS()
    {
        string[] scenes = { "Assets/Scenes/UnauthenticatedScene.unity", "Assets/Scenes/AuthenticatedScene.unity" };
        string buildPath = "Builds/MacOS/SampleApp.app";
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneOSX, BuildOptions.None);
        Debug.Log("Mac sample app build completed successfully.");
    }
}

#endif