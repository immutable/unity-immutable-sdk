#if UNITY_EDITOR

using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Immutable.Passport.Editor
{
    internal class PassportPostprocess : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log($"Passport post-processing...");

            if (report.summary.result is BuildResult.Failed || report.summary.result is BuildResult.Cancelled)
                return;

            BuildTarget buildTarget = report.summary.platform;

            string buildFullOutputPath = report.summary.outputPath;
            string buildAppName = Path.GetFileNameWithoutExtension(buildFullOutputPath);
            string buildOutputPath = Path.GetDirectoryName(buildFullOutputPath);

            // Get the build's data folder
            string buildDataPath = Path.GetFullPath($"{buildOutputPath}/{buildAppName}_Data/");
            if (buildTarget == BuildTarget.StandaloneOSX)
            {
                buildDataPath =
                    Path.GetFullPath($"{buildOutputPath}/{buildAppName}.app/Contents/Resources/Data/");
                Debug.Log($"StandaloneOSX buildDataPath: {buildDataPath}");
                // For OSX, checking to see if the path exists because the user might have exported the project to XCode
                // but there is no way to check that so try another path
                if (!Directory.Exists(buildDataPath))
                {
                    buildDataPath = Path.GetFullPath($"{buildFullOutputPath}/{Application.productName}/Resources/Data/");
                    Debug.Log($"StandaloneOSX buildDataPath 2: {buildDataPath}");
                }
            }
            else if (buildTarget == BuildTarget.iOS)
            {
                buildDataPath = Path.GetFullPath($"{buildOutputPath}/{buildAppName}/Data/");
                Debug.Log($"iOS buildDataPath: {buildDataPath}");
                if (!Directory.Exists(buildDataPath))
                {
                    buildDataPath = Path.GetFullPath($"{buildFullOutputPath}/Data/");
                    Debug.Log($"iOS buildDataPath 2: {buildDataPath}");
                }
            }

            // Copy passport files to data directory for these target
            // For other platforms, check the pre process file
            if (buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.StandaloneOSX || buildTarget == BuildTarget.iOS)
            {
                CopyIntoDataDir(buildDataPath);
                Debug.Log($"Successfully copied Passport files");
            }

            if (buildTarget == BuildTarget.iOS)
            {
                string projPath = $"{buildOutputPath}/{buildAppName}" + "/Unity-iPhone.xcodeproj/project.pbxproj";
                var type = Type.GetType("UnityEditor.iOS.Xcode.PBXProject, UnityEditor.iOS.Extensions.Xcode");

                if (type == null)
                {
                    Debug.LogError("unitywebview: failed to get PBXProject. please install iOS build support.");
                    return;
                }

                var src = File.ReadAllText(projPath);
                var proj = type.GetConstructor(Type.EmptyTypes).Invoke(null);
                {
                    var method = type.GetMethod("ReadFromString");
                    method.Invoke(proj, new object[] { src });
                }

                var target = "";
#if UNITY_2019_3_OR_NEWER
                {
                    var method = type.GetMethod("GetUnityFrameworkTargetGuid");
                    target = (string)method.Invoke(proj, null);
                }
#else
                {
                    var method = type.GetMethod("TargetGuidByName");
                    target = (string)method.Invoke(proj, new object[]{"Unity-iPhone"});
                }
#endif
                {
                    var method = type.GetMethod("AddFrameworkToProject");
                    method.Invoke(proj, new object[] { target, "WebKit.framework", false });
                }
                {
                    var method = type.GetMethod("AddFrameworkToProject");
                    method.Invoke(proj, new object[] { target, "AuthenticationServices.framework", false });
                }

                var cflags = "";
                if (EditorUserBuildSettings.development)
                {
                    cflags += " -DUNITYWEBVIEW_DEVELOPMENT";
                }

                cflags = cflags.Trim();

                if (!string.IsNullOrEmpty(cflags))
                {
                    var method = type.GetMethod("AddBuildProperty", new Type[] { typeof(string), typeof(string), typeof(string) });
                    method.Invoke(proj, new object[] { target, "OTHER_CFLAGS", cflags });
                }

                var dst = "";
                {
                    var method = type.GetMethod("WriteToString");
                    dst = (string)method.Invoke(proj, null);
                }

                File.WriteAllText(projPath, dst);
            }
        }

        private void CopyIntoDataDir(string buildDataPath)
        {
            // Check that the data folder exists
            if (!Directory.Exists(buildDataPath))
            {
                string errorMessage = "Failed to get the build's data folder. Make sure your build is the same name as your product name (In your project settings).";
                Debug.LogError(errorMessage);
                throw new Exception(errorMessage);
            }

            // Passport folder in the data folder
            string buildPassportPath = $"{buildDataPath}/ImmutableSDK/Runtime/Passport/";

            // Make sure it exists
            DirectoryInfo buildPassportInfo = new DirectoryInfo(buildPassportPath);
            if (!buildPassportInfo.Exists)
            {
                Directory.CreateDirectory(buildPassportPath);
            }
            else
            {
                // If the directory exists, clear it
                FileHelpers.ClearDirectory(buildPassportPath);
            }

            buildPassportPath = Path.GetFullPath(buildPassportPath);

            CopyFilesTo(buildPassportPath);
        }

        private void CopyFilesTo(string destinationPath)
        {
            Debug.Log("Copying Passport files...");

            // Find the location of the files
            string passportWebFilesDir = Path.GetFullPath("Packages/com.immutable.passport/Runtime/Resources");
            if (!Directory.Exists(passportWebFilesDir))
            {
                Debug.LogError("The Passport files directory doesn't exist!");
                return;
            }

            foreach (string dir in Directory.GetDirectories(passportWebFilesDir, "*", SearchOption.AllDirectories))
            {
                string dirToCreate = dir.Replace(passportWebFilesDir, destinationPath);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (string newPath in Directory.GetFiles(passportWebFilesDir, "*.*", SearchOption.AllDirectories))
            {
                if (!newPath.EndsWith(".meta"))
                {
                    File.Copy(newPath, newPath.Replace(passportWebFilesDir, destinationPath), true);
                }
            }
        }
    }
}

#endif