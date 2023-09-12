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
        private readonly WebViewObject webViewObject;
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        public GreeBrowserClient()
        {
            webViewObject = new();
            webViewObject.Init(
                cb: _cb,
                httpErr: (msg) =>
                {
                    Debug.LogError($"{TAG} http err: {msg}");
                },
                err: (msg) =>
                {
                    Debug.LogError($"{TAG} err: {msg}");
                }
            );
#if UNITY_ANDROID
            string filePath = Constants.SCHEME_FILE + ANDROID_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + MAC_DATA_DIRECTORY + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#else
            string filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            webViewObject.LoadURL(filePath);
        }

        private void _cb(string msg)
        {
            Debug.Log($"Received call from browser: {msg}");
            InvokeOnUnityPostMessage(msg);
        }

        internal void InvokeOnUnityPostMessage(string message)
        {
            OnUnityPostMessage?.Invoke(message);
        }

        public void ExecuteJs(string js)
        {
            webViewObject.EvaluateJS(js);
        }
    }
}