#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX

using Immutable.Browser.Core;
using UnityEngine;
using System.IO;

namespace Immutable.Browser.Gree
{
    public class GreeBrowserClient : IWebBrowserClient
    {
        private const string TAG = "[GreeBrowserClient]";
        private const string ANDROID_DATA_DIRECTORY = "android_asset";
        private const string MAC_DATA_DIRECTORY = "/Resources/Data";
        private const string MAC_EDITOR_RESOURCES_DIRECTORY = "Packages/com.immutable.passport/Runtime/Resources";
        private readonly WebViewObject webViewObject;
        public event OnUnityPostMessageDelegate OnUnityPostMessage;
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;

        public GreeBrowserClient()
        {
#if (UNITY_ANDROID && UNITY_EDITOR_OSX) || (UNITY_IPHONE && UNITY_EDITOR_OSX)
            Debug.LogWarning("Native Android and iOS WebViews cannot run in the Editor, so the macOS WebView is currently used to save your development time." + 
                " Testing your game on an actual device or emulator is recommended to ensure proper functionality.");
#endif
            webViewObject = new WebViewObject();
            webViewObject.Init(
                cb: InvokeOnUnityPostMessage,
                httpErr: InvokeOnPostMessageError,
                err: InvokeOnPostMessageError,
                auth: InvokeOnAuthPostMessage,
                log: InvokeOnLogMessage
            );
#if UNITY_ANDROID && !UNITY_EDITOR
            string filePath = Constants.SCHEME_FILE + ANDROID_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_OSX
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(MAC_EDITOR_RESOURCES_DIRECTORY) + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_STANDALONE_OSX
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + MAC_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
            filePath = filePath.Replace(" ", "%20");
#elif UNITY_IPHONE
            string filePath = Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#else
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            webViewObject.LoadURL(filePath);
        }

        private void InvokeOnPostMessageError(string id, string message)
        {
            if (OnPostMessageError != null)
            {
                OnPostMessageError.Invoke(id, message);
            }
        }

        internal void InvokeOnAuthPostMessage(string message)
        {
            if (OnAuthPostMessage != null)
            {
                OnAuthPostMessage.Invoke(message);
            }
        }

        internal void InvokeOnUnityPostMessage(string message)
        {
            if (OnUnityPostMessage != null)
            {
                OnUnityPostMessage.Invoke(message);
            }
        }

        internal void InvokeOnLogMessage(string message)
        {
            Debug.Log($"{TAG} InvokeOnLogMessage {message}");
        }

        public void ExecuteJs(string js)
        {
            webViewObject.EvaluateJS(js);
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            webViewObject.LaunchAuthURL(url, redirectUri);
        }

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            webViewObject.ClearCache(includeDiskFiles);
        }

        public void ClearStorage()
        {
            webViewObject.ClearStorage();
        }
#endif

    }
}

#endif