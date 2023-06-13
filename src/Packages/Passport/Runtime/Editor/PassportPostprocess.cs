#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Immutable.Passport.Editor {
    internal class PassportPostprocess : IPostprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report) {
            if (report.summary.result is BuildResult.Failed or BuildResult.Cancelled)
                return;

            BuildTarget buildTarget = report.summary.platform;

            string buildFullOutputPath = report.summary.outputPath;
            string buildAppName = Path.GetFileNameWithoutExtension(buildFullOutputPath);
            string buildOutputPath = Path.GetDirectoryName(buildFullOutputPath);

            Debug.Log("Copying passport HTML and JS files...");

            // Get the build's data folder
            string buildDataPath = Path.GetFullPath($"{buildOutputPath}/{buildAppName}_Data/");
            if (buildTarget == BuildTarget.StandaloneOSX) {
                buildDataPath =
                    Path.GetFullPath($"{buildOutputPath}/{buildAppName}.app/Contents/Resources/Data/");
            }

            // Check that the data folder exists
            if (!Directory.Exists(buildDataPath)) {
                Debug.LogError(
                    "Failed to get the build's data folder. Make sure your build is the same name as your product name (In your project settings).");
                return;
            }

            // Passport folder in the data folder
            string buildPassportPath = $"{buildDataPath}/Passport/";

            // Make sure it exists
            DirectoryInfo buildUwbInfo = new(buildPassportPath);
            if (!buildUwbInfo.Exists){
                Directory.CreateDirectory(buildPassportPath);
            } else {
                // If the directory exists, clear it
                foreach (FileInfo fileInfo in buildUwbInfo.EnumerateFiles()) {
                    fileInfo.Delete();
                }

                foreach (DirectoryInfo directoryInfo in buildUwbInfo.EnumerateDirectories()) {
                    directoryInfo.Delete(true);
                }
            }

            buildPassportPath = Path.GetFullPath(buildPassportPath);

            Debug.Log("Copying Passport files...");

            // Find the location of the files
            string engineFilesDir = Path.GetFullPath("Packages/com.immutable.passport/Runtime/Assets/Resources");
            if (!Directory.Exists(engineFilesDir)) {
                Debug.LogError("The Passport files directory doesn't exist!");
                return;
            }

            // Get all files that aren't .meta files
            string[] files = Directory.EnumerateFiles(engineFilesDir, "*.*", SearchOption.AllDirectories)
                .Where(fileType => !fileType.EndsWith(".meta"))
                .ToArray();

            int size = files.Length;
 
            // Copy files
            for (int i = 0; i < size; i++) {
                string file = files[i];
                string destFileName = Path.GetFileName(file);
                EditorUtility.DisplayProgressBar("Copying Passport Files", $"Copying {destFileName}", i / size);

                File.Copy(file, $"{buildPassportPath}{destFileName}", true);

                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"Sucessfully copied {size} Passport files");
        }
    }
}

#endif