/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using Immutable.Passport.Core.Logging;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using AOT;
#endif

using Callback = System.Action<string>;
using ErrorCallback = System.Action<string, string>;

namespace Immutable.Browser.Gree
{
#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    public class Singleton
    {
        private static Singleton _instance;
        public Callback onJS;
        public ErrorCallback onError;
        public ErrorCallback onHttpError;
        public Callback onAuth;
        public Callback onLog;

        public static Singleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Singleton();
                }
                return _instance;
            }
        }
    }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    public class WebViewObject : MonoBehaviour
#else
    public class WebViewObject
#endif
    {
        private const string TAG = "[WebViewObject]";
        Callback onJS;
        ErrorCallback onError;
        ErrorCallback onHttpError;
        Callback onAuth;
        Callback onLog;
#if UNITY_ANDROID && !UNITY_EDITOR
        class AndroidCallback : AndroidJavaProxy
        {
            private Action<string> callback;

            public AndroidCallback(Action<string> callback) : base("com.immutable.gree.unitywebview.WebViewCallback") 
            {
                this.callback = callback;
            }

            public void call(String message) {
                callback(message);
            }
        }

        AndroidJavaObject webView;
#else
        IntPtr webView;
#endif

#if UNITY_IPHONE && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern IntPtr _CImmutableWebViewPlugin_Init(string ua);
        [DllImport("__Internal")]
        private static extern int _CImmutableWebViewPlugin_Destroy(IntPtr instance);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_LoadURL(
            IntPtr instance, string url);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_EvaluateJS(
            IntPtr instance, string url);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_LaunchAuthURL(IntPtr instance, string url);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_SetDelegate(DelegateMessage callback);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_ClearCache(IntPtr instance, bool includeDiskFiles);
        [DllImport("__Internal")]
        private static extern void _CImmutableWebViewPlugin_ClearStorage(IntPtr instance);
#elif UNITY_STANDALONE_OSX || (UNITY_ANDROID && UNITY_EDITOR_OSX) || (UNITY_IPHONE && UNITY_EDITOR_OSX)
        [DllImport("ImmutableWebView")]
        private static extern IntPtr _CImmutableWebViewPlugin_Init(string ua);
        [DllImport("ImmutableWebView")]
        private static extern int _CImmutableWebViewPlugin_Destroy(IntPtr instance);
        [DllImport("ImmutableWebView")]
        private static extern void _CImmutableWebViewPlugin_LoadURL(
            IntPtr instance, string url);
        [DllImport("ImmutableWebView")]
        private static extern void _CImmutableWebViewPlugin_EvaluateJS(
            IntPtr instance, string url);
        [DllImport("ImmutableWebView")]
        private static extern void _CImmutableWebViewPlugin_LaunchAuthURL(IntPtr instance, string url, string redirectUri);
        [DllImport("ImmutableWebView")]
        private static extern void _CImmutableWebViewPlugin_SetDelegate(DelegateMessage callback);
#elif UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void _gree_unity_webview_init(string name);
        [DllImport("__Internal")]
        private static extern void _gree_unity_webview_loadURL(string name, string url);
        [DllImport("__Internal")]
        private static extern void _gree_unity_webview_evaluateJS(string name, string js);
        [DllImport("__Internal")]
        private static extern void _gree_unity_webview_destroy(string name);
        [DllImport("__Internal")]
        private static extern void _gree_unity_webview_launchAuthURL(string name, string url);
#endif

#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private delegate void DelegateMessage(string key, string message);

        [MonoPInvokeCallback(typeof(DelegateMessage))]
        private static void delegateMessageReceived(string key, string message)
        {
            if (key == "CallOnLog")
            {
                if (Singleton.Instance.onLog != null)
                {
                    Singleton.Instance.onLog(message);
                }
            }

            if (key == "CallFromJS")
            {
                if (Singleton.Instance.onJS != null)
                {
                    PassportLogger.Debug($"{TAG} onJS: " + message);
                    Singleton.Instance.onJS(message);
                }
                return;
            }

            if (key == "CallOnError" || key == "CallFromAuthCallbackError")
            {
                if (Singleton.Instance.onError != null)
                {
                    PassportLogger.Debug($"{TAG} onError: " + message);
                    Singleton.Instance.onError(key, message);
                }
                return;
            }

            if (key == "CallOnHttpError")
            {
                if (Singleton.Instance.onHttpError != null)
                {
                    PassportLogger.Debug($"{TAG} onHttpError: " + message);
                    Singleton.Instance.onHttpError(key, message);
                }
                return;
            }

            if (key == "CallFromAuthCallback")
            {
                if (Singleton.Instance.onAuth != null)
                {
                    PassportLogger.Debug($"{TAG} CallFromAuthCallback: " + message);
                    Singleton.Instance.onAuth(message);
                }
                return;
            }
        }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
        void Awake()
        {
            DontDestroyOnLoad(this);
        }
#endif
        public void handleMessage(string message)
        {
            var i = message.IndexOf(':', 0);
            if (i == -1)
                return;
            switch (message.Substring(0, i))
            {
                case "CallFromJS":
                    CallFromJS(message.Substring(i + 1));
                    break;
                case "CallOnError":
                    CallOnError("CallOnError", message.Substring(i + 1));
                    break;
                case "CallOnHttpError":
                    CallOnHttpError("CallOnHttpError", message.Substring(i + 1));
                    break;
            }
        }

        public void Init(
            Callback cb = null,
            ErrorCallback err = null,
            ErrorCallback httpErr = null,
            Callback auth = null,
            Callback log = null,
            string ua = "",
            // android
            int androidForceDarkMode = 0  // 0: follow system setting, 1: force dark off, 2: force dark on
            )
        {
            onJS = cb;
            onError = err;
            onHttpError = httpErr;
            onAuth = auth;
            onLog = log;
#if UNITY_WEBGL
#if !UNITY_EDITOR
            _gree_unity_webview_init(name);
#endif
#elif UNITY_WEBPLAYER
            Application.ExternalCall("unityWebView.init");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
            //TODO: UNSUPPORTED
            PassportLogger.Error("Webview is not supported on this platform.");
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX || (UNITY_ANDROID && UNITY_EDITOR_OSX)
            webView = _CImmutableWebViewPlugin_Init(ua);
            Singleton.Instance.onJS = ((message) => CallFromJS(message));
            Singleton.Instance.onError = ((id, message) => CallOnError(id, message));
            Singleton.Instance.onHttpError = ((id, message) => CallOnHttpError(id, message));
            Singleton.Instance.onAuth = ((message) => CallOnAuth(message));
            Singleton.Instance.onLog = ((message) => CallOnLog(message));
            _CImmutableWebViewPlugin_SetDelegate(delegateMessageReceived);
#elif UNITY_ANDROID
            webView = new AndroidJavaObject("com.immutable.gree.unitywebview.CWebViewPluginNoUi");
            webView.Call("Init", ua);
            webView.Call("setCallback", new AndroidCallback((message) => handleMessage(message)));
#else
            Logger.Error("Webview is not supported on this platform.");
#endif
        }

        public void LoadURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
#if UNITY_WEBGL
#if !UNITY_EDITOR
            _gree_unity_webview_loadURL(name, url);
#endif
#elif UNITY_WEBPLAYER
            Application.ExternalCall("unityWebView.loadURL", url);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
            //TODO: UNSUPPORTED
#elif UNITY_STANDALONE_OSX || UNITY_IPHONE || (UNITY_ANDROID && UNITY_EDITOR_OSX)
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_LoadURL(webView, url);
#elif UNITY_ANDROID
            if (webView == null)
                return;
            webView.Call("LoadURL", url);
#endif
        }

        public void EvaluateJS(string js)
        {
#if UNITY_WEBGL
#if !UNITY_EDITOR
            _gree_unity_webview_evaluateJS(name, js);
#endif
#elif UNITY_WEBPLAYER
            Application.ExternalCall("unityWebView.evaluateJS", js);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
            //TODO: UNSUPPORTED
#elif UNITY_STANDALONE_OSX || UNITY_IPHONE || (UNITY_ANDROID && UNITY_EDITOR_OSX)
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_EvaluateJS(webView, js);
#elif UNITY_ANDROID
            if (webView == null)
                return;
            webView.Call("EvaluateJS", js);
#endif
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
#if !IMMUTABLE_E2E_TESTING && (UNITY_STANDALONE_OSX || (UNITY_ANDROID && UNITY_EDITOR_OSX) || (UNITY_IPHONE && UNITY_EDITOR_OSX))
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_LaunchAuthURL(webView, url, redirectUri != null ? redirectUri : "");
#elif UNITY_IPHONE && !UNITY_EDITOR
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_LaunchAuthURL(webView, url);
#elif UNITY_WEBGL && !UNITY_EDITOR
            _gree_unity_webview_launchAuthURL(name, url);
#else
            UnityEngine.Application.OpenURL(url);
#endif
        }

        public void CallOnError(string id, string error)
        {
            if (onError != null)
            {
                onError(id, error);
            }
        }

        public void CallOnHttpError(string id, string error)
        {
            if (onHttpError != null)
            {
                onHttpError(id, error);
            }
        }

        public void CallOnAuth(string url)
        {
            if (onAuth != null)
            {
                onAuth(url);
            }
        }

        public void CallFromJS(string message)
        {
            if (onJS != null)
            {
#if !(UNITY_ANDROID && !UNITY_EDITOR)
#if UNITY_2018_4_OR_NEWER
                message = UnityWebRequest.UnEscapeURL(message.Replace("+", "%2B"));
#else // UNITY_2018_4_OR_NEWER
                message = WWW.UnEscapeURL(message.Replace("+", "%2B"));
#endif // UNITY_2018_4_OR_NEWER
#endif // !UNITY_ANDROID
                onJS(message);
            }
        }

        public void CallOnLog(string message)
        {
            if (onLog != null)
            {
                onLog(message);
            }
        }

        public void ClearCache(bool includeDiskFiles)
        {
#if UNITY_IPHONE && !UNITY_EDITOR
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_ClearCache(webView, includeDiskFiles);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (webView == null)
                return;
            webView.Call("ClearCache", includeDiskFiles);
#else
            throw new NotSupportedException();
#endif
        }

        public void ClearStorage()
        {
#if UNITY_IPHONE && !UNITY_EDITOR
            if (webView == IntPtr.Zero)
                return;
            _CImmutableWebViewPlugin_ClearStorage(webView);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (webView == null)
                return;
            webView.Call("ClearStorage");
#else
            throw new NotSupportedException();
#endif
        }
    }
}
