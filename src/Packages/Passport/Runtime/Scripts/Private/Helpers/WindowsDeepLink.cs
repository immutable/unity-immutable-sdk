#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using UnityEngine;

namespace Immutable.Passport.Helpers
{
    // .NET Framework must be used
    public class WindowsDeepLink : MonoBehaviour
    {
        private const string RegistryDeepLinkName = "deeplink";

        private static WindowsDeepLink? _instance;

        private Action<string>? _callback;
        private string? _protocolName;

        public static void Initialise(string? redirectUri, /*string logoutRedirectUri, */Action<string> callback)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(WindowsDeepLink)).AddComponent<WindowsDeepLink>();
            }

            if (string.IsNullOrEmpty(redirectUri)) return;

            var protocolName = redirectUri!.Split(new[] { "://" }, StringSplitOptions.None)[0];
            _instance._protocolName = protocolName;
            _instance._callback = callback;

            RegisterProtocol(protocolName);
            CreateCommandScript(protocolName);
        }

        // Force single instance must be enabled
        private static void CreateCommandScript(string protocolName)
        {
            var appPath = GetGameExecutablePath(".exe");
            var cmdPath = GetGameExecutablePath(".cmd");

            File.WriteAllLines(cmdPath, new List<string>
            {
                $"REG ADD \"HKCU\\Software\\Classes\\{protocolName}\" /v \"{RegistryDeepLinkName}\" /t REG_SZ /d %1 /f",
                $"start \"\" \"{appPath}\" %1"
            });
        }

        private static void RegisterProtocol(string protocolName)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{protocolName}");
            
            if (key == null)
            {
                throw new Exception($"Unable to create registry key for protocol '{protocolName}'.");
            }

            var applicationLocation = GetGameExecutablePath(".cmd");

            key.SetValue("URL Protocol", "");

            using var commandKey = key.CreateSubKey(@"shell\open\command");

            if (commandKey == null) throw new Exception("Unable to create registry sub key.");

            commandKey.SetValue("", $"\"{applicationLocation}\" \"%1\"");
        }

        private static string GetGameExecutablePath(string suffix)
        {
            // Get the current directory path where the game is running
            var exePath = Application.dataPath;

            // Remove the '/Data' part of the path to get the executable's directory
            exePath = exePath.Replace("/Data", "");
            exePath = exePath.Replace($"/{Application.productName}_Data", "");

            // Derive the game executable name from Application.productName (Unity auto-names the .exe based on project name)
            var exeName = Application.productName + suffix;

            return $"{exePath}/{exeName}".Replace("/", "\\");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;

            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{_protocolName}", writable: true);
            var value = key?.GetValue(RegistryDeepLinkName);

            if (value == null) return;

            key?.DeleteValue(RegistryDeepLinkName);

            var uri = (string)value;

            if (_protocolName != null && !uri.StartsWith(_protocolName))
            {
                Debug.LogError($"Incorrect prefix uri {uri}");
                return;
            }

            Destroy(gameObject);

            var cmdPath = GetGameExecutablePath(".cmd");
            File.Delete(cmdPath);

            _callback?.Invoke(uri);
        }
    }

}
#endif