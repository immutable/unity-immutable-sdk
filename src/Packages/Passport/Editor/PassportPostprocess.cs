#if UNITY_EDITOR

using System.IO;
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
            Debug.Log("Passport post-processing...");

            if (report.summary.result is BuildResult.Failed or BuildResult.Cancelled)
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
            }

            // Copy passport files to data directory for these target
            // For other platforms, check the pre process file
            if (buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.StandaloneOSX)
            {
                CopyIntoDataDir(buildDataPath);
                Debug.Log($"Sucessfully copied Passport files");
            }
        }

        private void CopyIntoDataDir(string buildDataPath)
        {
            // Check that the data folder exists
            if (!Directory.Exists(buildDataPath))
            {
                Debug.LogError(
                    "Failed to get the build's data folder. Make sure your build is the same name as your product name (In your project settings).");
                return;
            }

            // Passport folder in the data folder
            string buildPassportPath = $"{buildDataPath}/ImmutableSDK/Runtime/Passport/";

            // Make sure it exists
            DirectoryInfo buildPassportInfo = new(buildPassportPath);
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