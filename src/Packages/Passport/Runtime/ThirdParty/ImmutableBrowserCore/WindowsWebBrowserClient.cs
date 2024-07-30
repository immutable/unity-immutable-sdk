using System.IO;
using UnityEngine;
using Immutable.Browser.Core;
using Cysharp.Threading.Tasks;

namespace Immutable.Browser.Core
{
    public abstract class WindowsWebBrowserClient : IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        public abstract UniTask Init();

        public abstract void ExecuteJs(string js);

        protected void PostMessage(string message)
        {
            OnUnityPostMessage?.Invoke(message);
        }

        public void LaunchAuthURL(string url, string? redirectUri)
        {
            Application.OpenURL(url);
        }

        public abstract void Dispose();

        protected string GetBridgeFilePath()
        {
            string filePath = "";
#if UNITY_EDITOR
            filePath = Constants.SCHEME_FILE + Path.GetFullPath($"{Constants.PASSPORT_PACKAGE_RESOURCES_DIRECTORY}{Constants.PASSPORT_HTML_FILE_NAME}");
#elif UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            return filePath;
        }
    }
}