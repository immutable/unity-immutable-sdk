using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace Immutable.Passport.Editor
{
    /// <summary>
    /// Automatically adds "Sign in with Apple" capability to Xcode project after Unity build
    /// This prevents having to manually add it in Xcode every time
    /// </summary>
    public class AppleSignInPostProcessor
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            UnityEngine.Debug.Log("[AppleSignInPostProcessor] Adding 'Sign in with Apple' capability to Xcode project...");

            // Paths
            string projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
            string entitlementsPath = pathToBuiltProject + "/Unity-iPhone/Unity-iPhone.entitlements";

            // Load Xcode project
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Get main target GUID
#if UNITY_2019_3_OR_NEWER
            string targetGuid = project.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
#else
            string targetGuid = project.TargetGuidByName(PBXProject.GetUnityTargetName());
            string unityFrameworkTargetGuid = targetGuid;
#endif

            // Add AuthenticationServices framework (required for Sign in with Apple)
            project.AddFrameworkToProject(unityFrameworkTargetGuid, "AuthenticationServices.framework", false);
            UnityEngine.Debug.Log("[AppleSignInPostProcessor] ✅ Added AuthenticationServices.framework");

            // Create or update entitlements file
            CreateOrUpdateEntitlements(entitlementsPath);

            // Add entitlements file to project
            string entitlementsRelativePath = "Unity-iPhone/Unity-iPhone.entitlements";
            project.AddFile(entitlementsRelativePath, "Unity-iPhone.entitlements");
            project.AddBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", entitlementsRelativePath);

            UnityEngine.Debug.Log("[AppleSignInPostProcessor] ✅ Added entitlements file to project");

            // Save the modified project
            project.WriteToFile(projectPath);

            UnityEngine.Debug.Log("[AppleSignInPostProcessor] ✅ 'Sign in with Apple' capability added successfully!");
            UnityEngine.Debug.Log("[AppleSignInPostProcessor] ⚠️  IMPORTANT: Make sure your Bundle ID has 'Sign in with Apple' enabled in Apple Developer Portal!");
        }

        /// <summary>
        /// Creates or updates the entitlements file with Sign in with Apple capability
        /// </summary>
        private static void CreateOrUpdateEntitlements(string entitlementsPath)
        {
            PlistDocument entitlements = new PlistDocument();

            // If entitlements file already exists, read it
            if (File.Exists(entitlementsPath))
            {
                entitlements.ReadFromFile(entitlementsPath);
                UnityEngine.Debug.Log("[AppleSignInPostProcessor] Found existing entitlements file");
            }
            else
            {
                UnityEngine.Debug.Log("[AppleSignInPostProcessor] Creating new entitlements file");
            }

            // Get or create root dictionary
            PlistElementDict rootDict = entitlements.root;

            // Add "Sign in with Apple" capability
            // Key: com.apple.developer.applesignin
            // Value: array with "Default"
            if (!rootDict.values.ContainsKey("com.apple.developer.applesignin"))
            {
                PlistElementArray signInWithApple = rootDict.CreateArray("com.apple.developer.applesignin");
                signInWithApple.AddString("Default");
                UnityEngine.Debug.Log("[AppleSignInPostProcessor] ✅ Added 'com.apple.developer.applesignin' entitlement");
            }
            else
            {
                UnityEngine.Debug.Log("[AppleSignInPostProcessor] 'com.apple.developer.applesignin' entitlement already exists");
            }

            // Write the entitlements file
            entitlements.WriteToFile(entitlementsPath);
            UnityEngine.Debug.Log($"[AppleSignInPostProcessor] ✅ Entitlements file saved: {entitlementsPath}");
        }
    }
}

