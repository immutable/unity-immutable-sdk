using System.IO;
using UnityEngine;

namespace Immutable.Browser.Core
{
    public static class GameBridge
    {
        public static string GetFilePath()
        {
            string filePath = "";
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android device
            filePath = Constants.SCHEME_FILE + ANDROID_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_OSX
            // macOS editor
            filePath = Constants.SCHEME_FILE + Path.GetFullPath(MAC_EDITOR_RESOURCES_DIRECTORY) + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_STANDALONE_OSX
            // macOS
            filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + MAC_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
            filePath = filePath.Replace(" ", "%20");
#elif UNITY_IPHONE
            // iOS device
            filePath = Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_WIN
            // Windows editor
            filePath = Constants.SCHEME_FILE + Path.GetFullPath($"{Constants.PASSPORT_PACKAGE_RESOURCES_DIRECTORY}{Constants.PASSPORT_HTML_FILE_NAME}");
#else
            filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            return filePath;
        }
    }
}