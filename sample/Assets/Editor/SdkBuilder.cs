#if UNITY_EDITOR_WIN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Immutable.Passport.Editor
{
    public class SdkBuilder
    {
        [MenuItem("ImmutableSDK/Build Windows 64")]
        public static void StartWindows()
        {
            // Get filename.
            string path = EditorUtility.SaveFolderPanel("Build out WINDOWS to...",
                                                        GetProjectFolderPath() + "/Builds/",
                                                        "");
            var filename = path.Split('/');
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, filename[filename.Length - 1], path + "/");
        }

        static void BuildPlayer(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string filename, string path)
        {
            string fileExtension = "";
            string dataPath = "";
            string modifier = "";

            // configure path variables based on the platform we're targeting
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    modifier = "_windows";
                    fileExtension = ".exe";
                    dataPath = "_Data/";
                    break;
            }

            Debug.Log("====== BuildPlayer: " + buildTarget.ToString() + " at " + path + filename);
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

            string buildPath = path + filename + modifier + "/";
            Debug.Log("buildpath: " + buildPath);
            string playerPath = buildPath + filename + modifier + fileExtension;
            Debug.Log("playerpath: " + playerPath);
            BuildPipeline.BuildPlayer(GetScenePaths(), playerPath, buildTarget, BuildOptions.None);

            string fullDataPath = buildPath + filename + modifier + dataPath;
            Debug.Log("fullDataPath: " + fullDataPath);

            // Copy SDK contents
            string outputDir = $"{path}ImmutableSDK/";
            CopyDirectory($"{fullDataPath}ImmutableSDK", outputDir);

            // Copy DLLs
            CopyFiles($"{fullDataPath}Managed", $"{outputDir}Runtime/Dlls", new List<string>(){
                "Immutable.Browser.Core.dll",
                "Immutable.Passport.Runtime.Private.dll",
                "Newtonsoft.Json.dll",
                "UniTask.dll",
                "VoltRpc.dll",
                "VoltstroStudios.UnityWebBrowser.dll",
                "VoltstroStudios.UnityWebBrowser.Shared.dll"
            });
            // Copy public Passport file
            string publicPath = Path.GetFullPath("Packages/com.immutable.passport/Runtime/Scripts/Public");
            CopyFiles(publicPath, $"{outputDir}Runtime", new List<string>() { "Passport.cs" });
            // Immutable runtime assembly file
            File.Copy($"./Assets/Editor/SdkRuntimeAssembly.txt", $"{outputDir}Runtime/ImmutableSDK.Runtime.asmdef");
            // Create post process file
            Directory.CreateDirectory($"{outputDir}Editor");
            File.Copy($"./Assets/Editor/SdkPostProcess.txt", $"{outputDir}Editor/SdkPostProcess.cs");
            // Immutable edtitor assembly file
            File.Copy($"./Assets/Editor/SdkEditorAssembly.txt", $"{outputDir}Editor/ImmutableSDK.Editor.asmdef");
            // Read me file
            File.Copy($"./Assets/Editor/README.txt", $"{outputDir}README.md");

            // Delete sample app
            Debug.Log("Deleting sample app...");
            Directory.Delete(buildPath, true);

            Debug.Log($"Sucessfully created ImmutableSDK");
        }

        private static void CopyFiles(string sourceDir, string destinationDir, List<string> fileNames)
        {
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }
            foreach (string fileName in fileNames)
            {
                File.Copy($"{sourceDir}/{fileName}", $"{destinationDir}/{fileName}");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        static string[] GetScenePaths()
        {
            string[] scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = EditorBuildSettings.scenes[i].path;
            }
            return scenes;
        }

        static string GetProjectFolderPath()
        {
            var s = Application.dataPath;
            s = s.Substring(s.Length - 7, 7); // remove "Assets/"
            return s;
        }
    }
}

#endif