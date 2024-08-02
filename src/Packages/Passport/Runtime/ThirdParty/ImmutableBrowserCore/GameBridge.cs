using System.IO;
using UnityEngine;

namespace Immutable.Browser.Core
{
    public static class GameBridge
    {
        private const string SCHEME_FILE = "file:///";
        private const string PASSPORT_DATA_DIRECTORY_NAME = "/ImmutableSDK/Runtime/Passport";
        private const string PASSPORT_HTML_FILE_NAME = "/index.html";
        private const string PASSPORT_PACKAGE_RESOURCES_DIRECTORY = "Packages/com.immutable.passport/Runtime/Resources";
        private const string ANDROID_DATA_DIRECTORY = "android_asset";
        private const string MAC_DATA_DIRECTORY = "/Resources/Data";
        private const string MAC_EDITOR_RESOURCES_DIRECTORY = "Packages/com.immutable.passport/Runtime/Resources";

        public static string GetFilePath()
        {
            string filePath = "";
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android device
            filePath = SCHEME_FILE + ANDROID_DATA_DIRECTORY + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_OSX
            // macOS editor
            filePath = SCHEME_FILE + Path.GetFullPath(MAC_EDITOR_RESOURCES_DIRECTORY) + PASSPORT_HTML_FILE_NAME;
#elif UNITY_STANDALONE_OSX
            // macOS
            filePath = SCHEME_FILE + Path.GetFullPath(Application.dataPath) + MAC_DATA_DIRECTORY + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
            filePath = filePath.Replace(" ", "%20");
#elif UNITY_IPHONE
            // iOS device
            filePath = Path.GetFullPath(Application.dataPath) + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_WIN
            // Windows editor
            filePath = SCHEME_FILE + Path.GetFullPath($"{PASSPORT_PACKAGE_RESOURCES_DIRECTORY}{PASSPORT_HTML_FILE_NAME}");
#else
            filePath = SCHEME_FILE + Path.GetFullPath(Application.dataPath) + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#endif
            return filePath;
        }
    }
}