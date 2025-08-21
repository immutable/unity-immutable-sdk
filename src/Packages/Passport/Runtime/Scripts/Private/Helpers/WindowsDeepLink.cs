#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEditor;
using UnityEngine;
using Immutable.Passport.Core.Logging;

#nullable enable
namespace Immutable.Passport.Helpers
{
    public class WindowsDeepLink : MonoBehaviour
    {
        private const string REGISTRY_DEEP_LINK_NAME = "deeplink";

        private static WindowsDeepLink? _instance;
        private Action<string>? _callback;
        private string? _protocolName;

        // P/Invoke declarations
        private const uint HKEY_CURRENT_USER = 0x80000001;
        private const uint KEY_READ = 0x20019;
        private const uint KEY_WRITE = 0x20006;
        private const uint KEY_READ_WRITE = KEY_READ | KEY_WRITE;
        private const uint REG_SZ = 1;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegCreateKeyEx(
            UIntPtr hKey,
            string lpSubKey,
            int reserved,
            string? lpClass,
            uint dwOptions,
            uint samDesired,
            IntPtr lpSecurityAttributes,
            out UIntPtr phkResult,
            out uint lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegSetValueEx(
            UIntPtr hKey,
            string? lpValueName,
            int reserved,
            uint dwType,
            string lpData,
            uint cbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(UIntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegDeleteTree(UIntPtr hKey, string lpSubKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegOpenKeyEx(
            UIntPtr hKey,
            string subKey,
            uint options,
            uint samDesired,
            out UIntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegQueryValueEx(
            UIntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            byte[] lpData,
            ref uint lpcbData);

        /// <summary>
        /// Initialises the Windows deep link handler for a given protocol.
        /// </summary>
        /// <param name="redirectUri">The redirect URI containing the protocol to handle (e.g. "immutable://")</param>
        /// <param name="callback">Callback to invoke when a deep link is received</param>
        public static void Initialise(string redirectUri, Action<string> callback)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(WindowsDeepLink)).AddComponent<WindowsDeepLink>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            if (string.IsNullOrEmpty(redirectUri)) return;

            // Extract protocol name from URI (e.g. "immutable" from "immutable://")
            var protocolName = redirectUri.Split(new[] { "://" }, StringSplitOptions.None)[0];
            _instance._protocolName = protocolName;
            _instance._callback = callback;

            // Register protocol and create handler script
            RegisterProtocol(protocolName);
            CreateCommandScript(protocolName);
        }

        private static void CreateCommandScript(string protocolName)
        {
            // Get path for the command script file
            var cmdPath = GetGameExecutablePath(".cmd");

#if UNITY_EDITOR_WIN
            // Get Unity project and executable paths
            var projectPath = Application.dataPath.Replace("/Assets", "").Replace("/", "\\");
            var unityExe = EditorApplication.applicationPath.Replace("/", "\\");

            // Get path for the log file
            var logPath = Path.Combine(Application.persistentDataPath, "deeplink_logs.txt").Replace("/", "\\");

            string[] scriptLines =
            {
                "@echo off",
                // Store deeplink URI in registry
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{REGISTRY_DEEP_LINK_NAME}\" /t REG_SZ /d %1 /f >nul 2>&1",
                "setlocal",
                "",
                $"set \"PROJECT_PATH={projectPath}\"",
                $"set \"LOG_PATH={logPath}\"",
                "",
                // Create log file
                "echo [%date% %time%] Script started > \"%LOG_PATH%\"",
                "echo [%date% %time%] Project path: %PROJECT_PATH% >> \"%LOG_PATH%\"",
                $"echo [%date% %time%] Unity exe: {unityExe} >> \"%LOG_PATH%\"",
                "",
                // Find running Unity instance with matching project path
                "for /f \"tokens=2 delims==\" %%A in ('wmic process where \"name='Unity.exe'\" get ProcessId /value') do (",
                "    echo [%date% %time%] Checking Unity process ID: %%A >> \"%LOG_PATH%\"",
                "    setlocal EnableDelayedExpansion",
                "    set \"cmdline=\"",
                "    for /f \"tokens=*\" %%B in ('wmic process where \"ProcessId=%%A\" get CommandLine /value') do (",
                "        if not \"%%B\"==\"\" set \"cmdline=!cmdline!%%B\"",
                "    )",
                "    echo [%date% %time%] Raw command line: !cmdline! >> \"%LOG_PATH%\"",
                "    echo !cmdline! | findstr /I /C:\"-projectPath\" | findstr /I /C:\"%PROJECT_PATH%\" >nul",
                "    if not errorlevel 1 (",
                "        echo [%date% %time%] Found matching Unity process ID: %%A >> \"%LOG_PATH%\"",
                "        echo [%date% %time%] Command line: !cmdline! >> \"%LOG_PATH%\"",
                "        powershell -NoProfile -ExecutionPolicy Bypass -Command ^",
                "            \"$ErrorActionPreference = 'Continue';\" ^",
                "            \"$wshell = New-Object -ComObject wscript.shell;\" ^",
                $"            \"Add-Content -Path \\\"{logPath}\\\" -Value ('[' + (Get-Date) + '] Attempting to activate process ID: ' + %%A);\" ^",
                "            \"Start-Sleep -Milliseconds 100;\" ^",
                "            \"$result = $wshell.AppActivate(%%A);\" ^",
                $"            \"Add-Content -Path \\\"{logPath}\\\" -Value ('[' + (Get-Date) + '] AppActivate result: ' + $result);\" ^",
                $"            \"if (-not $result) {{ Add-Content -Path \\\"{logPath}\\\" -Value ('[' + (Get-Date) + '] Failed to activate window') }}\" ^",
                "        >nul 2>&1",
                "        if errorlevel 1 echo [%date% %time%] PowerShell error: %errorlevel% >> \"%LOG_PATH%\"",
                "        echo [%date% %time%] Unity activated, self-deleting >> \"%LOG_PATH%\"",
                $"        del \"%~f0\" >nul 2>&1",
                "        endlocal",
                "        exit /b 0",
                "    )",
                "    endlocal",
                ")",
                "",
                // Exit if Unity instance found
                "if %errorlevel% equ 0 exit /b 0",
                "",
                // Start new Unity instance if none found
                $"echo [%date% %time%] Starting new Unity instance >> \"%LOG_PATH%\"",
                $"start \"\" \"{unityExe}\" -projectPath \"%PROJECT_PATH%\" >nul 2>&1",
                "",
                // Self-delete the batch file when done
                "echo [%date% %time%] Script completed, self-deleting >> \"%LOG_PATH%\"",
                $"del \"%~f0\" >nul 2>&1"
            };
            
            File.WriteAllLines(cmdPath, scriptLines);
            PassportLogger.Debug($"Writing script to {cmdPath}");
            PassportLogger.Debug($"Writing logs to {logPath}");

#else
            // Get game executable path and name
            string pathToUnityGame = GetGameExecutablePath(".exe");
            string gameExeName = Path.GetFileName(pathToUnityGame);

            File.WriteAllLines(cmdPath, new[]
            {
                "@echo off",
                // Store deeplink URI in registry
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{REGISTRY_DEEP_LINK_NAME}\" /t REG_SZ /d %1 /f >nul 2>&1",
                // Check if game is already running
                $"tasklist /FI \"IMAGENAME eq {gameExeName}\" >nul 2>&1",
                "if %ERRORLEVEL%==0 (",
                // Bring existing game window to foreground
                "    powershell -NoProfile -ExecutionPolicy Bypass -Command ^",
                "        \"$ErrorActionPreference = 'SilentlyContinue';\" ^",
                "        \"$wshell = New-Object -ComObject wscript.shell;\" ^",
                "        \"$process = Get-Process -Name '" + Path.GetFileNameWithoutExtension(gameExeName) + "' -ErrorAction SilentlyContinue;\" ^",
                "        \"if ($process) { $wshell.AppActivate($process.Id) | Out-Null }\" ^",
                "    >nul 2>&1 3>&1 4>&1 5>&1",
                "    exit /b 0",
                ") else (",
                // Start new game instance if not running
                $"    start \"\" /b \"{pathToUnityGame}\" %1 >nul 2>&1 3>&1 4>&1 5>&1",
                ")"
            });
#endif
        }

        private static void RegisterProtocol(string protocolName)
        {
            PassportLogger.Debug($"Register protocol: {protocolName}");
            
            UIntPtr hKey;
            uint disposition;
            // Create registry key for the protocol
            var result = RegCreateKeyEx(
                (UIntPtr)HKEY_CURRENT_USER,
                $@"Software\Classes\{protocolName}",
                0,
                null,
                0,
                KEY_READ | KEY_WRITE,
                IntPtr.Zero,
                out hKey,
                out disposition);

            if (result != 0)
            {                
                throw new Exception($"Failed to create PKCE registry key. Error code: {result}");
            }

            // Set the default value for the protocol key to Application.productName
            // This is often used by Windows as the display name for the protocol
            var appProductName = Application.productName;
            var productNameDataSize = (uint)((appProductName.Length + 1) * Marshal.SystemDefaultCharSize); 
            var setDefaultResult = RegSetValueEx(hKey, null, 0, REG_SZ, appProductName, productNameDataSize);

            if (setDefaultResult != 0)
            {
                PassportLogger.Warn($"Failed to set default display name for protocol '{protocolName}'. Error code: {setDefaultResult}");
            }

            // Set URL Protocol value
            RegSetValueEx(hKey, "URL Protocol", 0, REG_SZ, string.Empty, (uint)(1 * Marshal.SystemDefaultCharSize)); 

            // Create command subkey
            UIntPtr commandKey;
            result = RegCreateKeyEx(
                hKey,
                @"shell\open\command",
                0,
                null,
                0,
                KEY_READ | KEY_WRITE,
                IntPtr.Zero,
                out commandKey,
                out disposition);

            if (result != 0)
            {              
                RegCloseKey(hKey);
                throw new Exception($"Failed to create PKCE command registry key. Error code: {result}");
            }

            // Set command to launch the script with the URI parameter
            var scriptLocation = GetGameExecutablePath(".cmd");
            string command = $"\"{scriptLocation}\" \"%1\"";
            uint commandSize = (uint)((command.Length + 1) * 2);
            
            result = RegSetValueEx(commandKey, "", 0, REG_SZ, command, commandSize);
            if (result != 0)
            {
                RegCloseKey(commandKey);
                RegCloseKey(hKey);
                throw new Exception($"Failed to set PKCE command. Error code: {result}");
            }

            // Clean up registry handles
            RegCloseKey(commandKey);
            RegCloseKey(hKey);
        }
        
        private static string GetGameExecutablePath(string suffix)
        {
#if !UNITY_EDITOR_WIN
            if (suffix == ".exe")
            {
                // Get the path of the currently running executable
                try
                {
                    var process = System.Diagnostics.Process.GetCurrentProcess();
                    if (process?.MainModule?.FileName != null)
                    {
                        return process.MainModule.FileName.Replace("/", "\\");
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    PassportLogger.Warn($"Failed to get process MainModule: {ex.Message}. Using fallback method.");
                }
                catch (System.InvalidOperationException ex)
                {
                    PassportLogger.Warn($"Process inaccessible: {ex.Message}. Using fallback method.");
                }

                // Fallback: Use command line args
                var args = System.Environment.GetCommandLineArgs();
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    var exePath = Path.GetFullPath(args[0]);
                    if (File.Exists(exePath))
                    {
                        return exePath.Replace("/", "\\");
                    }
                }
            }
#endif
            // For the editor, or for .cmd files in a build, use persistentDataPath as it's a writable location
            var fileName = Application.productName + suffix;
            return Path.Combine(Application.persistentDataPath, fileName).Replace("/", "\\");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Only handle deeplink when application regains focus
            if (!hasFocus) return;

            HandleDeeplink();
        }

        private void HandleDeeplink()
        {
            // Open registry key for the protocol
            var registryPath = $@"Software\Classes\{_protocolName}";
            UIntPtr hKey;
            var result = RegOpenKeyEx(
                (UIntPtr)HKEY_CURRENT_USER,
                registryPath,
                0,
                KEY_READ_WRITE,
                out hKey);

            if (result != 0)
            {
                PassportLogger.Error($"Failed to open registry key. Error code: {result}");
                return;
            }

            // Get size of deeplink data
            uint type = 0;
            uint dataSize = 0;
            result = RegQueryValueEx(hKey, REGISTRY_DEEP_LINK_NAME, IntPtr.Zero, ref type, null!, ref dataSize);

            if (result != 0)
            {
                RegCloseKey(hKey);
                PassportLogger.Warn($"Failed to get deeplink data size. Error code: {result}");
                return;
            }

            // Read deeplink data
            var data = new byte[dataSize];
            result = RegQueryValueEx(hKey, REGISTRY_DEEP_LINK_NAME, IntPtr.Zero, ref type, data, ref dataSize);

            var callbackInvoked = false;
            if (result == 0 && type == REG_SZ)
            {
                // Convert and validate URI
                var uri = System.Text.Encoding.Unicode.GetString(data, 0, (int)dataSize - 2); // Remove null terminator
                if (_protocolName != null && !uri.StartsWith(_protocolName))
                {
                    PassportLogger.Error($"Incorrect prefix uri {uri}");
                }
                else
                {
                    // Invoke callback with valid URI
                    _callback?.Invoke(uri);
                    callbackInvoked = true;
                }
            }
            else
            {
                PassportLogger.Warn($"Failed to get registry key. Error code: {result}");
            }

            // Clean up registry handle
            RegCloseKey(hKey);

            // Delete registry key if callback was invoked
            if (callbackInvoked)
            {
                result = RegDeleteTree((UIntPtr)HKEY_CURRENT_USER, registryPath);

                if (result != 0)
                {
                    PassportLogger.Warn($"Failed to delete registry key. Error code: {result}");
                }
                else
                {
                    PassportLogger.Debug("Successfully deleted registry key.");
                }
            }
            else
            {
                PassportLogger.Debug("Did not invoke callback so not deleting registry key.");
            }

            // Clean up command script
            // Note: Batch file will self-delete, no need to delete here
            // This prevents race condition where Unity deletes the file
            // while Windows is still trying to execute it
            var cmdPath = GetGameExecutablePath(".cmd");

            // Clean up instance
            Destroy(gameObject);
            _instance = null;
        }
    }
}
#endif
