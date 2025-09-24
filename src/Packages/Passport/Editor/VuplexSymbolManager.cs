using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Immutable.Passport.Editor
{
    /// <summary>
    /// Automatically manages VUPLEX_WEBVIEW scripting define symbol
    /// based on whether Vuplex WebView is present in the project
    /// </summary>
    [InitializeOnLoad]
    public static class VuplexSymbolManager
    {
        private const string VUPLEX_SYMBOL = "VUPLEX_WEBVIEW";
        
        static VuplexSymbolManager()
        {
            // Run on editor startup and after assembly reload
            EditorApplication.delayCall += CheckAndUpdateVuplexSymbol;
        }
        
        private static void CheckAndUpdateVuplexSymbol()
        {
            try
            {
                // Check if Vuplex WebView assembly exists
                bool vuplexExists = DoesVuplexAssemblyExist();
                
                // Get current build target group
                var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                
                // Get current scripting define symbols
                var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                var symbolList = currentSymbols.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
                
                bool symbolExists = symbolList.Contains(VUPLEX_SYMBOL);
                
                // Update symbol if needed
                if (vuplexExists && !symbolExists)
                {
                    // Add symbol
                    symbolList.Add(VUPLEX_SYMBOL);
                    var newSymbols = string.Join(";", symbolList);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
                    Debug.Log($"[VuplexSymbolManager] Added {VUPLEX_SYMBOL} symbol - Vuplex WebView detected");
                }
                else if (!vuplexExists && symbolExists)
                {
                    // Remove symbol
                    symbolList.Remove(VUPLEX_SYMBOL);
                    var newSymbols = string.Join(";", symbolList);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
                    Debug.Log($"[VuplexSymbolManager] Removed {VUPLEX_SYMBOL} symbol - Vuplex WebView not found");
                }
                
                // Log current status (only once per session to avoid spam)
                if (!SessionState.GetBool("VuplexSymbolManager.LoggedStatus", false))
                {
                    if (vuplexExists)
                    {
                        Debug.Log($"[VuplexSymbolManager] Vuplex WebView available, {VUPLEX_SYMBOL} symbol active");
                    }
                    else
                    {
                        Debug.Log($"[VuplexSymbolManager] Vuplex WebView not found");
                    }
                    SessionState.SetBool("VuplexSymbolManager.LoggedStatus", true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VuplexSymbolManager] Error managing Vuplex symbol: {ex.Message}");
            }
        }
        
        private static bool DoesVuplexAssemblyExist()
        {
            // Check for Vuplex assembly in multiple ways
            
            // Method 1: Check compiled assemblies
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Any(a => a.GetName().Name == "Vuplex.WebView"))
            {
                return true;
            }
            
            // Method 2: Check for Vuplex types
            var vuplexType = System.Type.GetType("Vuplex.WebView.CanvasWebViewPrefab, Vuplex.WebView");
            if (vuplexType != null)
            {
                return true;
            }
            
            // Method 3: Check for Vuplex assets in project
            var vuplexAssets = AssetDatabase.FindAssets("CanvasWebViewPrefab");
            foreach (var guid in vuplexAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Vuplex"))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
