#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX

using Immutable.Browser.Core;
using UnityEngine;
using System.IO;

namespace Immutable.Browser.Gree
{
    public class GreeBrowserClient : IWebBrowserClient
    {
        private const string TAG = "[GreeBrowserClient]";

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

            webViewObject.LoadURL(GameBridge.GetFilePath());
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