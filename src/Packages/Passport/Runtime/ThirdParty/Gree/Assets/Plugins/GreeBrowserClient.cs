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
            webViewObject = new();
            webViewObject.Init(
                cb: InvokeOnUnityPostMessage,
                httpErr: InvokeOnPostMessageError,
                err: InvokeOnPostMessageError,
                auth: InvokeOnAuthPostMessage
            );
#if UNITY_ANDROID
            string filePath = Constants.SCHEME_FILE + ANDROID_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_OSX
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(MAC_EDITOR_RESOURCES_DIRECTORY) + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_STANDALONE_OSX
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + MAC_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#else
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            webViewObject.LoadURL(filePath);
        }

        private void InvokeOnPostMessageError(string id, string message)
        {
            OnPostMessageError.Invoke(id, message);
        }

        internal void InvokeOnAuthPostMessage(string message)
        {
            OnAuthPostMessage.Invoke(message);
        }

        internal void InvokeOnUnityPostMessage(string message)
        {
            OnUnityPostMessage?.Invoke(message);
        }

        public void ExecuteJs(string js)
        {
            webViewObject.EvaluateJS(js);
        }

        public void LaunchAuthURL(string url, string? redirectUri)
        {
            webViewObject.LaunchAuthURL(url, redirectUri);
        }
    }
}