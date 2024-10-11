#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL

using Immutable.Browser.Core;
using Immutable.Passport.Core.Logging;
using UnityEngine;

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
            PassportLogger.Warn("Native Android and iOS WebViews cannot run in the Editor, so the macOS WebView is currently used to save your development time." +
                " Testing your game on an actual device or emulator is recommended to ensure proper functionality.");
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            GameObject webViewGameObject = new GameObject("WebViewObject");
            webViewObject = webViewGameObject.AddComponent<WebViewObject>();
#else
            webViewObject = new WebViewObject();
#endif
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
            PassportLogger.Debug($"{TAG} Console log: {message}");
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