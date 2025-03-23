#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Immutable.Passport.Helpers
{
    public class WindowsDeepLink : MonoBehaviour
    {
        private const string RegistryDeepLinkName = "deeplink";
        private static WindowsDeepLink? _instance;
        private Action<string>? _callback;
        private string? _protocolName;

        // P/Invoke declarations
        private const uint HKEY_CURRENT_USER = 0x80000001;
        private const uint KEY_READ = 0x20019;
        private const uint KEY_WRITE = 0x20006;
        private const uint KEY_ALL_ACCESS = 0xF003F;
        private const uint REG_SZ = 1;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegCreateKeyEx(
            UIntPtr hKey,
            string lpSubKey,
            int Reserved,
            string lpClass,
            uint dwOptions,
            uint samDesired,
            IntPtr lpSecurityAttributes,
            out UIntPtr phkResult,
            out uint lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegSetValueEx(
            UIntPtr hKey,
            string lpValueName,
            int Reserved,
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

        public static void Initialise(string redirectUri, Action<string> callback)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(WindowsDeepLink)).AddComponent<WindowsDeepLink>();
            }

            if (string.IsNullOrEmpty(redirectUri)) return;

            var protocolName = redirectUri.Split(new[] { "://" }, StringSplitOptions.None)[0];
            _instance._protocolName = protocolName;
            _instance._callback = callback;

            RegisterProtocol(protocolName);
            CreateCommandScript(protocolName);
        }

        private static void CreateCommandScript(string protocolName)
        {
            var cmdPath = GetGameExecutablePath(".cmd");
#if UNITY_EDITOR_WIN
            var projectPath = Application.dataPath.Replace("/Assets", "").Replace("/", "\\"); // Get the Unity project root path

            string[] scriptLines =
            {
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{RegistryDeepLinkName}\" /t REG_SZ /d %1 /f",
                "@echo off",
                "setlocal",
                "",
                $"set \"PROJECT_PATH={projectPath}\"",
                "",
                ":: Get running Unity processes",
                "for /f \"tokens=2 delims==\" %%A in ('wmic process where \"name='Unity.exe'\" get ProcessId /value') do (",
                "    for /f \"delims=\" %%B in ('wmic process where \"ProcessId=%%A\" get CommandLine /value ^| findstr /I /C:\"-projectPath \\\"%PROJECT_PATH%\\\"\"') do (",
                "        powershell -NoProfile -ExecutionPolicy Bypass -Command ^",
                "            \"$sig = '[DllImport(\\\"user32.dll\\\")] public static extern bool SetForegroundWindow(IntPtr hWnd);';\" ^",
                "            \"$type = Add-Type -MemberDefinition $sig -Name User32 -Namespace Win32 -PassThru;\" ^",
                "            \"$process = Get-Process -Id %%A;\" ^",
                "            \"$type::SetForegroundWindow($process.MainWindowHandle);\"",
                "        exit /b 0",
                "    )",
                ")",
                "",
                ":: Exit script if Unity was found",
                "if %errorlevel% equ 0 exit /b 0",
                "",
                ":: If no running instance found, start Unity",
                "start \"\" \"C:\\Program Files\\Unity\\Hub\\Editor\\2021.3.26f1\\Editor\\Unity.exe\" -projectPath \"%PROJECT_PATH%\""
            };
            
            File.WriteAllLines(cmdPath, scriptLines);
            Debug.Log($"Writing script to {cmdPath}");
#else
            File.WriteAllLines(cmdPath, new[]
            {
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{RegistryDeepLinkName}\" /t REG_SZ /d %1 /f",
                $"start \"\" \"{GetGameExecutablePath(".exe")}\" %1"
            });
#endif
        }

        private static void RegisterProtocol(string protocolName)
        {
            Debug.Log($"Register protocol: {protocolName}");
            
            UIntPtr hKey;
            uint disposition;
            int result = RegCreateKeyEx(
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
                Debug.LogError($"Failed to create registry key. Error code: {result}");
                return;
            }
            
            Debug.Log($@"Created registry key: Software\Classes\{protocolName}");

            RegSetValueEx(hKey, "URL Protocol", 0, REG_SZ, string.Empty, 0);
            
            Debug.Log("Added URL Protocol");

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
                Debug.LogError($"Failed to create command registry key. Error code: {result}");
                RegCloseKey(hKey);
                return;
            }
            
            Debug.Log($@"Created registry key: shell\open\command");

            var scriptLocation = GetGameExecutablePath(".cmd");
            
            string command = $"\"{scriptLocation}\" \"%1\"";

            uint commandSize = (uint)((command.Length + 1) * 2);
            
            result = RegSetValueEx(commandKey, "", 0, REG_SZ, command, commandSize);
            if (result != 0)
            {
                Debug.LogError($"Failed to set command. Error code: {result}");
            }

            RegCloseKey(commandKey);
            RegCloseKey(hKey);
        }

        private static string GetGameExecutablePath(string suffix)
        {
            var exeName = Application.productName + suffix;
#if UNITY_EDITOR_WIN
            return $"{Application.persistentDataPath}/{exeName}".Replace("/", "\\");
#else
            var exePath = Application.dataPath.Replace("/Data", "").Replace($"/{Application.productName}_Data", "");
            
            return $"{exePath}/{exeName}".Replace("/", "\\");
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            HandleDeeplink();
        }

        private void HandleDeeplink()
        {
            string registryPath = $@"Software\Classes\{_protocolName}";

            UIntPtr hKey;
            int result = RegOpenKeyEx(
                (UIntPtr)HKEY_CURRENT_USER,
                registryPath,
                0,
                KEY_ALL_ACCESS,
                out hKey);

            if (result != 0)
            {
                Debug.LogError($"Failed to open registry key. Error code: {result}");
                return;
            }

            uint type = 0;
            uint dataSize = 1024;
            var data = new byte[dataSize];
            result = RegQueryValueEx(hKey, RegistryDeepLinkName, IntPtr.Zero, ref type, data, ref dataSize);

            var callbackInvoked = false;
            if (result == 0 && type == REG_SZ)
            {
                var uri = System.Text.Encoding.Unicode.GetString(data, 0, (int)dataSize - 2); // Remove null terminator
                if (_protocolName != null && !uri.StartsWith(_protocolName))
                {
                    Debug.LogError($"Incorrect prefix uri {uri}");
                }
                else
                {
                    _callback?.Invoke(uri);
                    callbackInvoked = true;
                }
            }
            else
            {
                Debug.LogError($"Failed to get registry key. Error code: {result}");
            }

            // Close registry key
            RegCloseKey(hKey);

            if (callbackInvoked)
            {
                // Delete registry key
                result = RegDeleteTree((UIntPtr)HKEY_CURRENT_USER, registryPath);

                if (result != 0)
                {
                    Debug.LogError($"Failed to delete registry key. Error code: {result}");
                }
                else
                {
                    Debug.Log("Successfully deleted registry key.");
                }
            }
            else
            {
                Debug.Log("Did not invoke callback so not deleting registry key.");
            }

            var scriptPath = GetGameExecutablePath(".cmd");
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            Destroy(gameObject);
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegQueryValueEx(
            UIntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            ref uint lpType,
            byte[] lpData,
            ref uint lpcbData);
    }
}
#endif
