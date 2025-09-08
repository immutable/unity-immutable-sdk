using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if UNITY_IOS || UNITY_EDITOR_OSX
using Immutable.Browser.Gree;
#endif

namespace Immutable.Passport
{
#if UNITY_IOS || UNITY_EDITOR_OSX
    /// <summary>
    /// iOS implementation of IPassportWebView using Gree WebView (WKWebView)
    /// Wraps Gree WebViewObject in a clean, platform-agnostic interface
    /// </summary>
    public class iOSPassportWebView : IPassportWebView
    {
        private const string TAG = "[iOSPassportWebView]";

        // Gree WebView components
        private WebViewObject webViewObject;
        private GameObject webViewGameObject;

        // Configuration and state
        private RawImage targetRawImage;
        private MonoBehaviour coroutineRunner;
        private PassportWebViewConfig config;
        private bool isInitialized = false;
        private bool isVisible = false;
        private string currentUrl = "";

        // Events
        public event Action<string> OnJavaScriptMessage;
        public event Action OnLoadFinished;
        public event Action OnLoadStarted;

        // Properties
        public bool IsVisible => isVisible;
        public string CurrentUrl => currentUrl;

        /// <summary>
        /// Constructor for iOS PassportWebView
        /// </summary>
        /// <param name="targetRawImage">RawImage component to display WebView content (not used on iOS - native overlay)</param>
        /// <param name="coroutineRunner">MonoBehaviour to run coroutines (for consistency with other platforms)</param>
        public iOSPassportWebView(RawImage targetRawImage, MonoBehaviour coroutineRunner)
        {
            this.targetRawImage = targetRawImage ?? throw new ArgumentNullException(nameof(targetRawImage));
            this.coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));

            PassportLogger.Info($"{TAG} iOS WebView wrapper created");
        }

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
                PassportLogger.Info($"{TAG} Initializing iOS WebView with Gree WebView...");

                CreateWebViewObject();
                ConfigureWebView();

                isInitialized = true;
                PassportLogger.Info($"{TAG} iOS WebView initialized successfully");
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

            try
            {
                PassportLogger.Info($"{TAG} Loading URL: {url}");
                currentUrl = url;

                // iOS implementation: Use LaunchAuthURL to open in external browser (Safari/SFSafariViewController)
                // This follows the same pattern as the main Passport SDK for iOS authentication
                webViewObject.LaunchAuthURL(url, "immutablerunner://callback");
                PassportLogger.Info($"{TAG} URL launched in external browser: {url}");

                // Trigger load started event
                OnLoadStarted?.Invoke();

                // For iOS, we consider the load "finished" immediately since it opens externally
                OnLoadFinished?.Invoke();
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to load URL: {ex.Message}");
            }
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
                PassportLogger.Info($"{TAG} Showing WebView (iOS uses external browser)");

                // iOS implementation: WebView is always "shown" since we use external browser
                // The actual display happens when LoadUrl calls LaunchAuthURL
                isVisible = true;

                PassportLogger.Info($"{TAG} WebView shown successfully");
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
                PassportLogger.Info($"{TAG} Hiding WebView (iOS external browser will close automatically)");

                // iOS implementation: External browser closes automatically after auth
                // No explicit hide needed
                isVisible = false;

                PassportLogger.Info($"{TAG} WebView hidden successfully");
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
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - WebView not initialized");
                return;
            }

            try
            {
                PassportLogger.Debug($"{TAG} Executing JavaScript: {js.Substring(0, Math.Min(100, js.Length))}...");
                webViewObject.EvaluateJS(js);
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to execute JavaScript: {ex.Message}");
            }
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            if (!isInitialized)
            {
                PassportLogger.Error($"{TAG} Cannot register JavaScript method - WebView not initialized");
                return;
            }

            try
            {
                PassportLogger.Info($"{TAG} Registering JavaScript method: {methodName}");

                // iOS implementation: Since we use external browser (Safari/SFSafariViewController),
                // JavaScript methods are handled through deep links and auth callbacks
                // The login data will be captured via the auth callback when the external browser
                // redirects back to the app with immutablerunner://callback

                PassportLogger.Info($"{TAG} JavaScript method '{methodName}' registered (handled via deep link callbacks on iOS)");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to register JavaScript method: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                PassportLogger.Info($"{TAG} Disposing iOS WebView");

                // Destroy WebView GameObject
                if (webViewGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(webViewGameObject);
                    webViewGameObject = null;
                }

                // Clear references
                webViewObject = null;
                targetRawImage = null;
                coroutineRunner = null;

                isInitialized = false;
                isVisible = false;

                PassportLogger.Info($"{TAG} iOS WebView disposed successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error during disposal: {ex.Message}");
            }
        }

    #region Private Implementation

        private void CreateWebViewObject()
        {
            PassportLogger.Info($"{TAG} Creating Gree WebViewObject...");

            // Create GameObject for WebView
            webViewGameObject = new GameObject("PassportUI_iOS_WebView");
            UnityEngine.Object.DontDestroyOnLoad(webViewGameObject);

            webViewObject = new WebViewObject();

            PassportLogger.Info($"{TAG} Gree WebViewObject created successfully");
        }

        private void ConfigureWebView()
        {
            PassportLogger.Info($"{TAG} Configuring Gree WebView...");

            // Initialize WebViewObject with callbacks
            webViewObject.Init(
                cb: OnWebViewMessage,
                httpErr: OnWebViewHttpError,
                err: OnWebViewError,
                auth: OnWebViewAuth,
                log: OnWebViewLog
            );

            // Configure WebView settings

            // Clear cache if requested
            if (config.ClearCacheOnInit)
            {
                webViewObject.ClearCache(true);
                PassportLogger.Info($"{TAG} WebView cache cleared");
            }

            PassportLogger.Info($"{TAG} Gree WebView configured successfully");
        }

    #endregion

    #region WebView Event Handlers

        private void OnWebViewMessage(string message)
        {
            try
            {
                PassportLogger.Debug($"{TAG} WebView message: {message}");

                // Parse message to see if it's a JavaScript method call
                if (message.Contains("method") && message.Contains("data"))
                {
                    // This is a JavaScript method call from our registered methods
                    OnJavaScriptMessage?.Invoke(message);
                }
                else
                {
                    // Regular WebView message
                    OnJavaScriptMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error handling WebView message: {ex.Message}");
            }
        }

        private void OnWebViewHttpError(string id, string message)
        {
            PassportLogger.Error($"{TAG} WebView HTTP error [{id}]: {message}");
        }

        private void OnWebViewError(string id, string message)
        {
            PassportLogger.Error($"{TAG} WebView error [{id}]: {message}");
        }

        private void OnWebViewAuth(string message)
        {
            PassportLogger.Info($"{TAG} WebView auth message: {message}");
            // Auth messages could be login completion, etc.
            OnJavaScriptMessage?.Invoke(message);
        }

        private void OnWebViewLog(string message)
        {
            PassportLogger.Debug($"{TAG} WebView console log: {message}");
        }

    #endregion
    }
#endif
}
