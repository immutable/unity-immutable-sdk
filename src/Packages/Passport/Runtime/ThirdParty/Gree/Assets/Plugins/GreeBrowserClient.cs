using Immutable.Browser.Core;
using UnityEngine;
using System.IO;

namespace Immutable.Browser.Gree
{
    public class GreeBrowserClient : IWebBrowserClient
    {
        private const string TAG = "[GreeBrowserClient]";
        private const string SCHEME_FILE = "file:///";
        private const string ANDROID_DATA_DIRECTORY = "android_asset";
#pragma warning disable IDE0051
        private const string PASSPORT_DATA_DIRECTORY_NAME = "/Passport";
#pragma warning restore IDE0051
        private const string PASSPORT_HTML_FILE_NAME = "/index.html";
        private readonly WebViewObject _webViewObject;
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        public GreeBrowserClient()
        {
            _webViewObject = new();
            _webViewObject.Init(
                cb: _cb,
                ld: (msg) =>
                {
                    Debug.Log($"{TAG} loaded: {msg}");
                },
                httpErr: (msg) =>
                {
                    Debug.Log($"{TAG} http err: {msg}");
                },
                err: (msg) =>
                {
                    Debug.Log($"{TAG} err: {msg}");
                }
            );
#if UNITY_ANDROID
            string filePath = SCHEME_FILE + ANDROID_DATA_DIRECTORY + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#else
            string filePath = SCHEME_FILE + Path.GetFullPath(Application.dataPath) + PASSPORT_DATA_DIRECTORY_NAME + PASSPORT_HTML_FILE_NAME;
#endif
            _webViewObject.LoadURL(filePath);
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
            _webViewObject.EvaluateJS(js);
        }
    }
}