using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Immutable.Passport.Editor
{
    /// <summary>
    /// Automatically manages UWB_WEBVIEW scripting define symbol
    /// based on whether UnityWebBrowser is present in the project.
    /// </summary>
    [InitializeOnLoad]
    public static class UwbSymbolManager
    {
        private const string UWB_SYMBOL = "UWB_WEBVIEW";

        static UwbSymbolManager()
        {
            // Run on editor startup and after assembly reload
            EditorApplication.delayCall += CheckAndUpdateUwbSymbol;
        }

        private static void CheckAndUpdateUwbSymbol()
        {
            try
            {
                // Check if UnityWebBrowser assembly exists
                bool uwbExists = DoesUwbAssemblyExist();

                // Get current build target group
                var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

                // Get current scripting define symbols
                var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                var symbolList = currentSymbols.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();

                bool symbolExists = symbolList.Contains(UWB_SYMBOL);

                // Update symbol if needed
                if (uwbExists && !symbolExists)
                {
                    // Add symbol
                    symbolList.Add(UWB_SYMBOL);
                    var newSymbols = string.Join(";", symbolList);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
                    Debug.Log($"[UwbSymbolManager] Added {UWB_SYMBOL} symbol - UnityWebBrowser detected");
                }
                else if (!uwbExists && symbolExists)
                {
                    // Remove symbol
                    symbolList.Remove(UWB_SYMBOL);
                    var newSymbols = string.Join(";", symbolList);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
                    Debug.Log($"[UwbSymbolManager] Removed {UWB_SYMBOL} symbol - UnityWebBrowser not found");
                }

                // Log current status (only once per session to avoid spam)
                if (!SessionState.GetBool("UwbSymbolManager.LoggedStatus", false))
                {
                    if (uwbExists)
                    {
                        Debug.Log($"[UwbSymbolManager] UnityWebBrowser available, {UWB_SYMBOL} symbol active");
                    }
                    else
                    {
                        Debug.Log($"[UwbSymbolManager] UnityWebBrowser not found");
                    }

                    SessionState.SetBool("UwbSymbolManager.LoggedStatus", true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UwbSymbolManager] Error managing UWB symbol: {ex.Message}");
            }
        }

        private static bool DoesUwbAssemblyExist()
        {
            // Check for UnityWebBrowser in multiple ways

            // Method 1: Check compiled assemblies by name
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Any(a => a.GetName().Name == "VoltstroStudios.UnityWebBrowser"))
            {
                return true;
            }

            // Method 2: Check for a well-known UWB type
            var uwbType = System.Type.GetType("VoltstroStudios.UnityWebBrowser.Core.WebBrowserClient, VoltstroStudios.UnityWebBrowser");
            if (uwbType != null)
            {
                return true;
            }

            // Method 3: Check for UWB assets in project (by name hint)
            var uwbAssets = AssetDatabase.FindAssets("UnityWebBrowser t:ScriptableObject");
            foreach (var guid in uwbAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("UnityWebBrowser"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}


