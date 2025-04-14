#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Immutable.Passport.Core.Logging;

#nullable enable
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
        private const uint KEY_READ_WRITE = KEY_READ | KEY_WRITE;
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
                DontDestroyOnLoad(_instance.gameObject);
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
            var unityExe = EditorApplication.applicationPath.Replace("/", "\\");

            string[] scriptLines =
            {
                "@echo off",
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{RegistryDeepLinkName}\" /t REG_SZ /d %1 /f >nul 2>&1",
                "setlocal",
                "",
                $"set \"PROJECT_PATH={projectPath}\"",
                "",
                ":: Get running Unity processes",
                "for /f \"tokens=2 delims==\" %%A in ('wmic process where \"name='Unity.exe'\" get ProcessId /value 2^>nul') do (",
                "    for /f \"delims=\" %%B in ('wmic process where \"ProcessId=%%A\" get CommandLine /value 2^>nul ^| findstr /I /C:\"-projectPath \\\"%PROJECT_PATH%\\\"\" 2^>nul') do (",
                "        powershell -NoProfile -ExecutionPolicy Bypass -Command ^",
                "            \"$sig = '[DllImport(\\\"user32.dll\\\")] public static extern bool SetForegroundWindow(IntPtr hWnd);';\" ^",
                "            \"$type = Add-Type -MemberDefinition $sig -Name User32 -Namespace Win32 -PassThru;\" ^",
                "            \"$process = Get-Process -Id %%A;\" ^",
                "            \"$type::SetForegroundWindow($process.MainWindowHandle);\" ^",
                "        >nul 2>&1",
                "        exit /b 0",
                "    )",
                ")",
                "",
                ":: Exit script if Unity was found",
                "if %errorlevel% equ 0 exit /b 0",
                "",
                ":: If no running instance found, start Unity",
                $"start \"\" \"{unityExe}\" -projectPath \"%PROJECT_PATH%\" >nul 2>&1"
            };

            File.WriteAllLines(cmdPath, scriptLines);
            PassportLogger.Debug($"Writing script to {cmdPath}");

#else

            string pathToUnityGame = GetGameExecutablePath(".exe");
            string gameExeName = Path.GetFileName(pathToUnityGame);

            File.WriteAllLines(cmdPath, new[]
            {
                "@echo off",
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{RegistryDeepLinkName}\" /t REG_SZ /d %1 /f >nul 2>&1",
                $"tasklist /FI \"IMAGENAME eq {gameExeName}\" 2>NUL | find /I \"{gameExeName}\" >NUL",
                "if %ERRORLEVEL%==0 (",
                "    powershell -NoProfile -ExecutionPolicy Bypass -Command ^",
                "        \"$ErrorActionPreference = 'SilentlyContinue';\" ^",
                "        \"$wshell = New-Object -ComObject wscript.shell;\" ^",
                "        \"$process = Get-Process -Name '" + Path.GetFileNameWithoutExtension(gameExeName) + "' -ErrorAction SilentlyContinue;\" ^",
                "        \"if ($process) { $wshell.AppActivate($process.Id) | Out-Null }\" ^",
                "    >nul 2>&1 3>&1 4>&1 5>&1",
                "    exit /b 0",
                ") else (",
                $"    start \"\" /b \"{pathToUnityGame}\" %1 >nul 2>&1",
                ")"
            });
#endif
        }

        private static void RegisterProtocol(string protocolName)
        {
            PassportLogger.Debug($"Register protocol: {protocolName}");

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
                throw new Exception($"Failed to create PKCE registry key. Error code: {result}");
            }

            RegSetValueEx(hKey, "URL Protocol", 0, REG_SZ, string.Empty, 2);

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

            RegCloseKey(commandKey);
            RegCloseKey(hKey);
        }

        private static string GetGameExecutablePath(string suffix)
        {
            var exeName = Application.productName + suffix;
#if UNITY_EDITOR_WIN
            return Path.Combine(Application.persistentDataPath, exeName).Replace("/", "\\");
#else
            var exePath = Application.dataPath.Replace("/Data", "").Replace($"/{Application.productName}_Data", "");
            
            return Path.Combine(exePath, exeName).Replace("/", "\\");
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
                KEY_READ_WRITE,
                out hKey);

            if (result != 0)
            {
                PassportLogger.Error($"Failed to open registry key. Error code: {result}");
                return;
            }

            uint type = 0;
            uint dataSize = 0;
            result = RegQueryValueEx(hKey, RegistryDeepLinkName, IntPtr.Zero, ref type, null!, ref dataSize);

            if (result != 0)
            {
                RegCloseKey(hKey);
                PassportLogger.Warn($"Failed to get deeplink data size. Error code: {result}");
                return;
            }

            var data = new byte[dataSize];
            result = RegQueryValueEx(hKey, RegistryDeepLinkName, IntPtr.Zero, ref type, data, ref dataSize);

            var callbackInvoked = false;
            if (result == 0 && type == REG_SZ)
            {
                var uri = System.Text.Encoding.Unicode.GetString(data, 0, (int)dataSize - 2); // Remove null terminator
                if (_protocolName != null && !uri.StartsWith(_protocolName))
                {
                    PassportLogger.Error($"Incorrect prefix uri {uri}");
                }
                else
                {
                    _callback?.Invoke(uri);
                    callbackInvoked = true;
                }
            }
            else
            {
                PassportLogger.Warn($"Failed to get registry key. Error code: {result}");
            }

            // Close registry key
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

            // Delete command prompt script
            var cmdPath = GetGameExecutablePath(".cmd");
            if (File.Exists(cmdPath))
            {
                try
                {
                    File.Delete(cmdPath);
                }
                catch (Exception ex)
                {
                    PassportLogger.Warn($"Failed to delete script: {ex.Message}");
                }
            }

            Destroy(gameObject);
            _instance = null;
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

