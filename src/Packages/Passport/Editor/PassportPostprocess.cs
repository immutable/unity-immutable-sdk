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
            if (report.summary.result is BuildResult.Failed or BuildResult.Cancelled)
                return;

            BuildTarget buildTarget = report.summary.platform;

            string buildFullOutputPath = report.summary.outputPath;
            string buildAppName = Path.GetFileNameWithoutExtension(buildFullOutputPath);
            string buildOutputPath = Path.GetDirectoryName(buildFullOutputPath);

            Debug.Log("Copying passport browser files...");

            // Get the build's data folder
            string buildDataPath = Path.GetFullPath($"{buildOutputPath}/{buildAppName}_Data/");
            if (buildTarget == BuildTarget.StandaloneOSX)
            {
                buildDataPath =
                    Path.GetFullPath($"{buildOutputPath}/{buildAppName}.app/Contents/Resources/Data/");
            } else if (buildTarget == BuildTarget.Android)
            {
                buildDataPath = Path.GetFullPath($"{buildOutputPath}/{buildAppName}/unityLibrary/src/main/assets/");
            }

            // Check that the data folder exists
            if (!Directory.Exists(buildDataPath))
            {
                Debug.LogError(
                    "Failed to get the build's data folder. Make sure your build is the same name as your product name (In your project settings).");
                return;
            }

            // Passport folder in the data folder
            string buildPassportPath = $"{buildDataPath}/Passport/";

            // Make sure it exists
            DirectoryInfo buildPassportInfo = new(buildPassportPath);
            if (!buildPassportInfo.Exists)
            {
                Directory.CreateDirectory(buildPassportPath);
            }
            else
            {
                // If the directory exists, clear it
                foreach (FileInfo fileInfo in buildPassportInfo.EnumerateFiles())
                {
                    fileInfo.Delete();
                }

                foreach (DirectoryInfo directoryInfo in buildPassportInfo.EnumerateDirectories())
                {
                    directoryInfo.Delete(true);
                }
            }

            buildPassportPath = Path.GetFullPath(buildPassportPath);

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
                string dirToCreate = dir.Replace(passportWebFilesDir, buildPassportPath);
                Directory.CreateDirectory(dirToCreate);
            }

            foreach (string newPath in Directory.GetFiles(passportWebFilesDir, "*.*", SearchOption.AllDirectories))
            {
                if (!newPath.EndsWith(".meta"))
                {
                    File.Copy(newPath, newPath.Replace(passportWebFilesDir, buildPassportPath), true);
                }
            }

            Debug.Log($"Sucessfully copied Passport web files");
        }
    }
}

#endif