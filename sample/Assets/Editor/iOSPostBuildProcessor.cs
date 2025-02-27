using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Linq;
using UnityEditor.iOS.Xcode;

public class iOSPostBuildProcessor
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS && IsCommandLineBuild())
        {
            Debug.Log("Command-line iOS build detected. Modifying Info.plist and Xcode project...");
            ModifyInfoPlist(pathToBuiltProject);
            ModifyXcodeProject(pathToBuiltProject, GetBundleIdentifierFromArgs());
        }
        else
        {
            Debug.Log("Skipping Info.plist modification (not an iOS command-line build).");
        }
    }

    private static bool IsCommandLineBuild()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        return args.Contains("--ciBuild"); // Check for the --ciBuild flag
    }

    private static void ModifyInfoPlist(string pathToBuiltProject)
    {
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");

        if (!File.Exists(plistPath))
        {
            Debug.LogError("Info.plist not found!");
            return;
        }

        // Load the Info.plist
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Get the root dictionary
        PlistElementDict rootDict = plist.root;

        // Add App Transport Security Settings
        PlistElementDict atsDict = rootDict.CreateDict("NSAppTransportSecurity");
        atsDict.SetBoolean("NSAllowsArbitraryLoads", true);

        // Save the modified Info.plist
        plist.WriteToFile(plistPath);

        Debug.Log("Successfully updated Info.plist with NSAllowsArbitraryLoads set to YES.");
    }

    private static void ModifyXcodeProject(string pathToBuiltProject, string bundleIdentifier)
    {
        string pbxprojPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject pbxProject = new PBXProject();
        pbxProject.ReadFromFile(pbxprojPath);

        string targetGuid = pbxProject.GetUnityMainTargetGuid(); // Unity 2019+
        pbxProject.SetBuildProperty(targetGuid, "PRODUCT_BUNDLE_IDENTIFIER", bundleIdentifier);

        pbxProject.WriteToFile(pbxprojPath);
        Debug.Log($"Updated Xcode project with bundle identifier: {bundleIdentifier}");
    }

    private static string GetBundleIdentifierFromArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--bundleIdentifier" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return "com.immutable.Immutable-Sample"; // Default fallback
    }
}