#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Immutable.Passport.Editor
{
    class PassportAndroidProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 0; } }
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            Debug.Log("MyCustomBuildProcessor.OnPostGenerateGradleAndroidProject at path " + path);

            // Find the location of the files
            string passportWebFilesDir = Path.GetFullPath("Packages/com.immutable.passport/Runtime/Resources");
            if (!Directory.Exists(passportWebFilesDir))
            {
                Debug.LogError("The Passport files directory doesn't exist!");
                return;
            }

            FileHelpers.CopyDirectory(passportWebFilesDir, $"{path}/src/main/assets/ImmutableSDK/Runtime/Passport");
            Debug.Log($"Sucessfully copied Passport files");
        }
    }
}

#endif