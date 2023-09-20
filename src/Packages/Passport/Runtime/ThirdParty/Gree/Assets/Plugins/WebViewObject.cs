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

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Rendering;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using AOT;
#endif

using Callback = System.Action<string>;

#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
public class Singleton 
{
    private static Singleton _instance;
    public Callback onJS;
    public Callback onError;
    public Callback onHttpError;

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

public class WebViewObject
{
    private const string TAG = "[WebViewObject]";
    Callback onJS;
    Callback onError;
    Callback onHttpError;
#if UNITY_ANDROID
    class AndroidCallback : AndroidJavaProxy
    {
        private Action<string> callback;

        public AndroidCallback(Action<string> callback) : base("net.gree.unitywebview.WebViewCallback") 
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

#if UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern IntPtr _CWebViewPlugin_Init(string ua);
    [DllImport("__Internal")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("__Internal")]
    private static extern void _CWebViewPlugin_SetDelegate(DelegateMessage callback);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [DllImport("WebView")]
    private static extern IntPtr _CWebViewPlugin_Init(string ua);
    [DllImport("WebView")]
    private static extern int _CWebViewPlugin_Destroy(IntPtr instance);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_LoadURL(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_EvaluateJS(
        IntPtr instance, string url);
    [DllImport("WebView")]
    private static extern void _CWebViewPlugin_SetDelegate(DelegateMessage callback);
#elif UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_init();
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_loadURL(string url);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_evaluateJS(string js);
    [DllImport("__Internal")]
    private static extern void _gree_unity_webview_destroy();
#endif

#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    private delegate void DelegateMessage(string key, string message);
 
    [MonoPInvokeCallback(typeof(DelegateMessage))] 
    private static void delegateMessageReceived(string key, string message) {
        if (key == "CallFromJS") {
            if (Singleton.Instance.onJS != null) {
                Debug.Log($"{TAG} ==== onJS callback running message: " + message);
                Singleton.Instance.onJS(message);
            }
            return;
        }

        if (key == "CallOnError") {
            if (Singleton.Instance.onError != null) {
                Debug.Log($"{TAG} ==== onError callback running message: " + message);
                Singleton.Instance.onError(message);
            }
            return;
        }

        if (key == "CallOnHttpError") {
            if (Singleton.Instance.onHttpError != null) {
                Debug.Log($"{TAG} ==== onHttpError callback running message: " + message);
                Singleton.Instance.onHttpError(message);
            }
            return;
        }

        Debug.Log($"{TAG} delegateMessageReceived unsupported key " + key);
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
                CallOnError(message.Substring(i + 1));
                break;
            case "CallOnHttpError":
                CallOnHttpError(message.Substring(i + 1));
                break;
        }
    }

    public void Init(
        Callback cb = null,
        Callback err = null,
        Callback httpErr = null,
        string ua = "",
        // android
        int androidForceDarkMode = 0  // 0: follow system setting, 1: force dark off, 2: force dark on
        )
    {
        onJS = cb;
        onError = err;
        onHttpError = httpErr;
#if UNITY_WEBGL
    #if !UNITY_EDITOR
        _gree_unity_webview_init();
    #endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.init");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
        Debug.LogError("Webview is not supported on this platform.");
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        webView = _CWebViewPlugin_Init(ua);
        Singleton.Instance.onJS = ((message) => CallFromJS(message));
        Singleton.Instance.onError = ((message) => CallOnError(message));
        Singleton.Instance.onHttpError = ((message) => CallOnHttpError(message));
        _CWebViewPlugin_SetDelegate(delegateMessageReceived);
#elif UNITY_ANDROID
        webView = new AndroidJavaObject("net.gree.unitywebview.CWebViewPluginNoUi");
        webView.Call("Init", ua);
        webView.Call("setCallback", new AndroidCallback((message) => handleMessage(message)));
#else
        Debug.LogError("Webview is not supported on this platform.");
#endif
    }

    public void LoadURL(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;
#if UNITY_WEBGL
#if !UNITY_EDITOR
        _gree_unity_webview_loadURL(url);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.loadURL", url);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_LoadURL(webView, url);
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
        _gree_unity_webview_evaluateJS(js);
#endif
#elif UNITY_WEBPLAYER
        Application.ExternalCall("unityWebView.evaluateJS", js);
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX
        //TODO: UNSUPPORTED
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IPHONE
        if (webView == IntPtr.Zero)
            return;
        _CWebViewPlugin_EvaluateJS(webView, js);
#elif UNITY_ANDROID
        if (webView == null)
            return;
        webView.Call("EvaluateJS", js);
#endif
    }

    public void CallOnError(string error)
    {
        if (onError != null)
        {
            onError(error);
        }
    }

    public void CallOnHttpError(string error)
    {
        if (onHttpError != null)
        {
            onHttpError(error);
        }
    }

    public void CallFromJS(string message)
    {
        if (onJS != null)
        {
#if !UNITY_ANDROID
#if UNITY_2018_4_OR_NEWER
            message = UnityWebRequest.UnEscapeURL(message);
#else // UNITY_2018_4_OR_NEWER
            message = WWW.UnEscapeURL(message);
#endif // UNITY_2018_4_OR_NEWER
#endif // !UNITY_ANDROID
            onJS(message);
        }
    }
}