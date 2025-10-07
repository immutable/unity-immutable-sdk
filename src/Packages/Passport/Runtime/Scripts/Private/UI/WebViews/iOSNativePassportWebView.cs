#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Immutable.Passport.Core.Logging;

namespace Immutable.Passport
{
    /// <summary>
    /// iOS native WKWebView implementation of IPassportWebView
    /// Uses native iOS WebKit framework for optimal performance
    /// </summary>
    public class iOSNativePassportWebView : IPassportWebView
    {
        private const string TAG = "[iOSNativePassportWebView]";

        // P/Invoke declarations for native iOS plugin
        [DllImport("__Internal")]
        private static extern IntPtr PassportWebView_Create(string gameObjectName);

        [DllImport("__Internal")]
        private static extern void PassportWebView_Destroy(IntPtr webViewPtr);

        [DllImport("__Internal")]
        private static extern void PassportWebView_LoadURL(IntPtr webViewPtr, string url);

        [DllImport("__Internal")]
        private static extern void PassportWebView_Show(IntPtr webViewPtr);

        [DllImport("__Internal")]
        private static extern void PassportWebView_Hide(IntPtr webViewPtr);

        [DllImport("__Internal")]
        private static extern void PassportWebView_SetFrame(IntPtr webViewPtr, float x, float y, float width, float height);

        [DllImport("__Internal")]
        private static extern void PassportWebView_ExecuteJavaScript(IntPtr webViewPtr, string script);

        [DllImport("__Internal")]
        private static extern void PassportWebView_SetCustomURLScheme(IntPtr webViewPtr, string urlScheme);

        // Callback delegates (for Phase 4, but defined now)
        private delegate void OnLoadFinishedDelegate(string url);
        private delegate void OnJavaScriptMessageDelegate(string method, string data);
        private delegate void OnURLChangedDelegate(string url);

        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnLoadFinishedCallback(OnLoadFinishedDelegate callback);

        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnJavaScriptMessageCallback(OnJavaScriptMessageDelegate callback);

        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnURLChangedCallback(OnURLChangedDelegate callback);

        // Instance variables
        private IntPtr webViewPtr;
        private PassportWebViewConfig config;
        private bool isInitialized = false;
        private bool isVisible = false;

        // Events (IPassportWebView interface)
        public event Action<string> OnJavaScriptMessage;
        public event Action OnLoadFinished;
        public event Action OnLoadStarted;

        // Properties (IPassportWebView interface)
        public bool IsVisible => isVisible;
        public string CurrentUrl { get; private set; }

        public void Initialize(PassportWebViewConfig config)
        {
            if (isInitialized)
            {
                PassportLogger.Warn($"{TAG} Already initialized, skipping");
                return;
            }

            this.config = config ?? new PassportWebViewConfig();

            try
            {
                PassportLogger.Info($"{TAG} Initializing iOS native WebView...");

                // Create native WebView
                webViewPtr = PassportWebView_Create("PassportWebView");

                if (webViewPtr == IntPtr.Zero)
                {
                    throw new Exception("Failed to create native WebView (returned null pointer)");
                }

                // Configure custom URL scheme if provided
                if (!string.IsNullOrEmpty(config.CustomURLScheme))
                {
                    PassportWebView_SetCustomURLScheme(webViewPtr, config.CustomURLScheme);
                    PassportLogger.Info($"{TAG} Custom URL scheme set: {config.CustomURLScheme}");
                }

                // Set up callbacks (Phase 4 will implement these fully)
                PassportWebView_SetOnLoadFinishedCallback(OnLoadFinishedCallback);
                PassportWebView_SetOnJavaScriptMessageCallback(OnJavaScriptMessageCallback);
                PassportWebView_SetOnURLChangedCallback(OnURLChangedCallback);

                isInitialized = true;
                PassportLogger.Info($"{TAG} iOS native WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize: {ex.Message}");
                throw;
            }
        }

        public void LoadUrl(string url)
        {
            if (!isInitialized)
            {
                PassportLogger.Error($"{TAG} Cannot load URL - WebView not initialized");
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                PassportLogger.Error($"{TAG} Cannot load empty URL");
                return;
            }

            CurrentUrl = url;
            PassportWebView_LoadURL(webViewPtr, url);
            PassportLogger.Info($"{TAG} Loading URL: {url}");
        }

        public void Show()
        {
            if (!isInitialized)
            {
                PassportLogger.Error($"{TAG} Cannot show - WebView not initialized");
                return;
            }

            try
            {
                // Calculate frame based on config
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;

                float width = config.Width > 0 ? config.Width : screenWidth;
                float height = config.Height > 0 ? config.Height : screenHeight;

                // Center the WebView
                float x = (screenWidth - width) / 2f;
                float y = (screenHeight - height) / 2f;

                // Set frame
                PassportWebView_SetFrame(webViewPtr, x, y, width, height);

                // Show WebView
                PassportWebView_Show(webViewPtr);

                isVisible = true;
                PassportLogger.Info($"{TAG} WebView shown at ({x}, {y}) with size ({width}, {height})");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to show WebView: {ex.Message}");
            }
        }

        public void Hide()
        {
            if (!isInitialized)
            {
                PassportLogger.Warn($"{TAG} Cannot hide - WebView not initialized");
                return;
            }

            try
            {
                PassportWebView_Hide(webViewPtr);
                isVisible = false;
                PassportLogger.Info($"{TAG} WebView hidden");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to hide WebView: {ex.Message}");
            }
        }

        public void ExecuteJavaScript(string js)
        {
            if (!isInitialized)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - WebView not ready");
                return;
            }

            if (string.IsNullOrEmpty(js))
            {
                PassportLogger.Warn($"{TAG} Cannot execute empty JavaScript");
                return;
            }

            try
            {
                PassportWebView_ExecuteJavaScript(webViewPtr, js);
                PassportLogger.Debug($"{TAG} Executing JavaScript: {js}");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to execute JavaScript: {ex.Message}");
            }
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            // Phase 4: Will implement JavaScript message handling
            // For now, just log that this was called
            PassportLogger.Info($"{TAG} RegisterJavaScriptMethod called for: {methodName} (Phase 4 feature)");

            // Store handler for when Phase 4 is implemented
            OnJavaScriptMessage += (message) => {
                // Parse message format: "methodName:data"
                var parts = message.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && parts[0] == methodName)
                {
                    handler?.Invoke(parts[1]);
                }
            };
        }

        public void Dispose()
        {
            if (webViewPtr != IntPtr.Zero)
            {
                PassportLogger.Info($"{TAG} Disposing iOS native WebView");
                PassportWebView_Destroy(webViewPtr);
                webViewPtr = IntPtr.Zero;
            }

            isInitialized = false;
            isVisible = false;
            CurrentUrl = null;

            PassportLogger.Info($"{TAG} iOS native WebView disposed");
        }

        // Callback implementations (Phase 4 will make these functional)
        [AOT.MonoPInvokeCallback(typeof(OnLoadFinishedDelegate))]
        private static void OnLoadFinishedCallback(string url)
        {
            PassportLogger.Info($"[iOSNativePassportWebView] Load finished: {url} (Phase 4 feature)");
            // Phase 4: Will trigger OnLoadFinished event
        }

        [AOT.MonoPInvokeCallback(typeof(OnJavaScriptMessageDelegate))]
        private static void OnJavaScriptMessageCallback(string method, string data)
        {
            PassportLogger.Info($"[iOSNativePassportWebView] JS Message - Method: {method}, Data: {data} (Phase 4 feature)");
            // Phase 4: Will trigger OnJavaScriptMessage event
        }

        [AOT.MonoPInvokeCallback(typeof(OnURLChangedDelegate))]
        private static void OnURLChangedCallback(string url)
        {
            PassportLogger.Info($"[iOSNativePassportWebView] URL changed: {url} (Phase 4 feature)");
            // Phase 4: Will handle OAuth callback URLs
        }
    }
}
#endif
