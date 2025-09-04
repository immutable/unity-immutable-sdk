using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Immutable.Passport.Core.Logging;
using System;
using System.IO;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Reflection;
using Immutable.Passport.Helpers;
using Immutable.Passport.Model;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Immutable.Passport
{
    /// <summary>
    /// Data structure for login information received from JavaScript
    /// </summary>
    [System.Serializable]
    public class LoginData
    {
        public string provider;
        public string email;
        public bool marketing_consent;

        public override string ToString()
        {
            return $"LoginData(provider: {provider}, email: {email}, marketing_consent: {marketing_consent})";
        }
    }

    /// <summary>
    /// PassportUI that follows the working WebViewTest pattern
    /// Key difference: Let Unity handle component lifecycle instead of manual initialization
    ///
    /// IMPORTANT: When setting up the PassportUI prefab in the editor:
    /// - Set RawImage component's width and height to 0 in the RectTransform
    /// - This ensures the UI is completely hidden before initialization
    ///
    /// NOTE: In the Unity Editor, the WebView GameObject must be selected for input to work.
    /// This is handled automatically when the WebView is shown.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class PassportUI : MonoBehaviour
    {
        private const string TAG = "[PassportUI]";

        // Static events for automatic cross-scene behavior
        /// <summary>
        /// Static event triggered when any PassportUI instance successfully completes login
        /// </summary>
        public static event Action OnLoginSuccessStatic;

        /// <summary>
        /// Static event triggered when any PassportUI instance fails login
        /// </summary>
        public static event Action<string> OnLoginFailureStatic;

        [Header("UI Settings")]
        public Button loginButton;

        [Header("Login Events")]
        [Tooltip("Unity Event triggered when login succeeds")]
        public UnityEvent OnLoginSuccess;

        [Tooltip("Unity Event triggered when login fails (with error message)")]
        public UnityEvent<string> OnLoginFailure;

        [Header("Debug Settings")]
        public bool enableRemoteDebugging = false;
        public uint remoteDebuggingPort = 9222;

        [Header("Cache Settings")]
        public bool clearCacheOnLogin = true;

        // Cross-platform WebView abstraction
        private IPassportWebView webView;

        private Passport _passportInstance;
        private RawImage rawImage;
        // Login completion source removed - OAuth handled by external browser
        private bool isInitialized = false;
        private bool pointerEnterTriggered = false;
        private GameObject bridgeWebViewGameObject;

        // Input management
        private Coroutine inputActivationCoroutine;

        /// <summary>
        /// Initialize PassportUI with the main Passport instance
        /// Following the working WebViewTest pattern
        /// </summary>
        public void Init(Passport passportInstance)
        {
            if (passportInstance == null)
            {
                PassportLogger.Error($"{TAG} Passport instance cannot be null");
                return;
            }

            _passportInstance = passportInstance;
            PassportLogger.Info($"{TAG} Initializing PassportUI with working WebViewTest pattern...");

            // Get RawImage component
            rawImage = GetComponent<RawImage>();

            // Ensure UI starts completely hidden
            rawImage.enabled = false;

            // Also set size to zero as additional safeguard
            var rectTransform = rawImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = Vector2.zero;
                PassportLogger.Info($"{TAG} RawImage size set to zero for startup hiding");
            }

            if (rawImage == null)
            {
                PassportLogger.Error($"{TAG} RawImage component not found!");
                return;
            }

            // Create cross-platform WebView
            CreateWebView();

            // Set up login button
            SetupLoginButton();

            // Hide initially
            HideLoginUI(logMessage: false);

            PassportLogger.Info($"{TAG} PassportUI initialized successfully");
        }

        /// <summary>
        /// Create cross-platform WebView using the appropriate implementation for the current platform
        /// </summary>
        private void CreateWebView()
        {
            try
            {
                PassportLogger.Info($"{TAG} Creating cross-platform WebView...");

                // Dispose existing WebView if any
                if (webView != null)
                {
                    PassportLogger.Warn($"{TAG} WebView already exists! Disposing previous instance.");
                    webView.Dispose();
                    webView = null;
                }

                // Create platform-specific WebView
                webView = CreatePlatformWebView();
                if (webView == null)
                {
                    throw new NotSupportedException("WebView not supported on this platform");
                }

                // Configure WebView
                var config = new PassportWebViewConfig
                {
                    EnableRemoteDebugging = enableRemoteDebugging,
                    RemoteDebuggingPort = remoteDebuggingPort,
                    ClearCacheOnInit = clearCacheOnLogin,
                    InitialUrl = "about:blank"
                };

                // Initialize WebView
                webView.Initialize(config);

                // Subscribe to events
                webView.OnLoadFinished += OnWebViewLoadFinished;
                webView.OnLoadStarted += OnWebViewLoadStarted;

                // Register JavaScript methods
                webView.RegisterJavaScriptMethod("HandleLoginData", HandleLoginData);

                isInitialized = true;
                PassportLogger.Info($"{TAG} Cross-platform WebView created successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to create WebView: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Factory method to create the appropriate WebView implementation for the current platform
        /// </summary>
        private IPassportWebView CreatePlatformWebView()
        {
#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_EDITOR && UNITY_EDITOR_WIN))
            PassportLogger.Info($"{TAG} Creating Windows WebView (UWB)");
            return new WindowsPassportWebView(rawImage, this);
#elif UNITY_IOS || UNITY_EDITOR_OSX
            PassportLogger.Info($"{TAG} Creating iOS WebView (WKWebView)");
            return new iOSPassportWebView(rawImage, this);
#elif UNITY_ANDROID
            PassportLogger.Info($"{TAG} Creating Android WebView");
            return new AndroidPassportWebView(rawImage, this);
#elif UNITY_STANDALONE_OSX
            PassportLogger.Info($"{TAG} Creating macOS WebView (WKWebView)");
            // TODO: Implement macOS WebView
            throw new NotImplementedException("macOS WebView not yet implemented");
#else
            PassportLogger.Error($"{TAG} WebView not supported on this platform");
            return null;
#endif
        }

        /// <summary>
        /// Event handler for WebView load finished
        /// </summary>
        private void OnWebViewLoadFinished()
        {
            string url = webView?.CurrentUrl ?? "";
            PassportLogger.Info($"{TAG} WebView load finished: {url}");

            // Loading UI removed
            if (!url.StartsWith("about:blank"))
            {
                PassportLogger.Info($"{TAG} Content loaded");
            }

            // Input activation is now handled by the WebView wrapper
            PassportLogger.Info($"{TAG} Input activation delegated to WebView wrapper");
        }

        /// <summary>
        /// Event handler for WebView load started
        /// </summary>
        private void OnWebViewLoadStarted()
        {
            string url = webView?.CurrentUrl ?? "";
            PassportLogger.Info($"{TAG} WebView loading started: {url}");
            // OAuth callbacks handled by external browser for security - no interception needed
        }

        /// <summary>
        /// Handles login data received from JavaScript
        /// Called when the mock login page submits login information
        /// </summary>
        /// <param name="jsonData">JSON string containing login data</param>
        private async void HandleLoginData(string jsonData)
        {
            try
            {
                PassportLogger.Info($"{TAG} Received login data from JavaScript: {jsonData}");

                // Parse the JSON data
                LoginData loginData = JsonUtility.FromJson<LoginData>(jsonData);
                PassportLogger.Info($"{TAG} Parsed login data: {loginData}");

                // For now, just log the data - in the future this could trigger OAuth flow
                PassportLogger.Info($"{TAG} Login attempt - Provider: {loginData.provider}, Email: {loginData.email}, Marketing Consent: {loginData.marketing_consent}");

                // Login(bool useCachedSession = false, DirectLoginOptions directLoginOptions = null)
                DirectLoginMethod loginMethod;
                switch (loginData.provider)
                {
                    case "email":
                        loginMethod = DirectLoginMethod.Email;
                        break;
                    case "google":
                        loginMethod = DirectLoginMethod.Google;
                        break;
                    case "apple":
                        loginMethod = DirectLoginMethod.Apple;
                        break;
                    case "facebook":
                        loginMethod = DirectLoginMethod.Facebook;
                        break;
                    default:
                        PassportLogger.Error($"{TAG} Invalid login provider: {loginData.provider}");
                        return;
                }
                var loginOptions = new DirectLoginOptions(loginMethod);
                if (loginData.email != null)
                {
                    loginOptions.email = loginData.email;
                }

                // Perform the login and handle the result
                PassportLogger.Info($"{TAG} Starting login with provider: {loginData.provider}");
                bool loginSuccess = await _passportInstance.Login(false, loginOptions);

                if (loginSuccess)
                {
                    PassportLogger.Info($"{TAG} ✅ Login successful!");

                    // Trigger events
                    OnLoginSuccess?.Invoke();
                    OnLoginSuccessStatic?.Invoke();

                    // Hide the WebView after successful login
                    HideLoginUI();
                }
                else
                {
                    string errorMessage = "Login failed - authentication was not completed";
                    PassportLogger.Error($"{TAG} ❌ {errorMessage}");

                    // Trigger failure events
                    OnLoginFailure?.Invoke(errorMessage);
                    OnLoginFailureStatic?.Invoke(errorMessage);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Login failed with exception: {ex.Message}";

                // Check if this is a user cancellation vs a real error
                if (ex.Message.Contains("the connection was disabled"))
                {
                    errorMessage = "Login cancelled by user or connection was disabled";
                    PassportLogger.Warn($"{TAG} {errorMessage}");
                }
                else if (ex.Message.Contains("Uri was missing state and/or code"))
                {
                    errorMessage = "OAuth callback error - this usually means the user cancelled login or there was a network issue";
                    PassportLogger.Warn($"{TAG} {errorMessage}");
                }
                else
                {
                    PassportLogger.Error($"{TAG} Failed to handle login data: {errorMessage}");
                }

                // Trigger failure events for exceptions too
                OnLoginFailure?.Invoke(errorMessage);
                OnLoginFailureStatic?.Invoke(errorMessage);
            }
        }

        private void SetupLoginButton()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
                loginButton.onClick.AddListener(OnLoginButtonClicked);
                PassportLogger.Info($"{TAG} Login button configured");
            }
        }

        private async void OnLoginButtonClicked()
        {
            await ShowLoginUI();
        }

        /// <summary>
        /// Show the login UI and start the authentication flow
        /// </summary>
        public async UniTask<bool> ShowLoginUI()
        {
            if (!isInitialized)
            {
                PassportLogger.Error($"{TAG} PassportUI not initialized");
                return false;
            }

            if (_passportInstance == null)
            {
                PassportLogger.Error($"{TAG} Passport instance is null");
                return false;
            }

            try
            {
                PassportLogger.Info($"{TAG} Starting login flow...");

                // Cache clearing is now handled by WebView configuration during initialization
                PassportLogger.Info($"{TAG} Cache clearing configured: {clearCacheOnLogin}");

                // Wait for main Passport bridge to be ready
                await WaitForPassportReady();
                PassportLogger.Info($"{TAG} Passport bridge ready - waiting for login form submission");

                // CRITICAL: Initialize deep link system for OAuth callback handling
                PassportLogger.Info($"{TAG} 🔗 Initializing deep link system for OAuth callbacks");
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                try
                {
                    // Get the PassportImpl instance to access OnDeepLinkActivated
                    var passportType = typeof(Passport);
                    var implField = passportType.GetField("_passportImpl",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (implField?.GetValue(_passportInstance) is PassportImpl passportImpl)
                    {
                        // Initialize deep link system with the redirect URI and PassportImpl's callback
                        var redirectUri = "immutablerunner://callback";
                        WindowsDeepLink.Initialise(redirectUri, passportImpl.OnDeepLinkActivated);
                        PassportLogger.Info($"{TAG} ✅ Deep link system initialized for protocol: {redirectUri}");
                    }
                    else
                    {
                        PassportLogger.Error($"{TAG} Could not access PassportImpl for deep link initialization");
                    }
                }
                catch (Exception ex)
                {
                    PassportLogger.Error($"{TAG} Failed to initialize deep link system: {ex.Message}");
                }
#endif

                // TESTING: Load local test page instead of auth URL
                PassportLogger.Info($"{TAG} 🧪 Loading local test page for development");
                string testPageUrl = "http://localhost:8080";
                PassportLogger.Info($"{TAG} Test page URL: {testPageUrl}");

                // Show the WebView
                webView.Show();
                PassportLogger.Info($"{TAG} WebView shown");

                // Navigate to test page
                webView.LoadUrl(testPageUrl);
                PassportLogger.Info($"{TAG} Navigated to test page: {testPageUrl}");

                // Test page loaded for development
                PassportLogger.Info($"{TAG} 🧪 Test page loaded in WebView for development and testing.");
                PassportLogger.Info($"{TAG} Login data will be captured and logged from the test page.");

                // Return true since we successfully started the OAuth flow
                // The actual authentication completion is handled by the deep link system
                return true;
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Login flow failed: {ex.Message}");
                HideLoginUI();
                return false;
            }
        }

        /// <summary>
        /// Hide the login UI
        /// </summary>
        public void HideLoginUI(bool logMessage = true)
        {
            // Hide the WebView
            if (webView != null)
            {
                webView.Hide();
                if (logMessage)
                {
                    PassportLogger.Info($"{TAG} Login UI hidden");
                }
            }

            // CRITICAL: Re-enable the bridge WebView GameObject now that UI is hidden
            if (bridgeWebViewGameObject != null && !bridgeWebViewGameObject.activeSelf)
            {
                bridgeWebViewGameObject.SetActive(true);
                PassportLogger.Info($"{TAG} ✅ Re-enabled bridge WebView {bridgeWebViewGameObject.name} - UI WebView is now hidden");
                bridgeWebViewGameObject = null; // Clear reference
            }

            // Reset pointer enter flag for next login
            pointerEnterTriggered = false;
        }

        /// <summary>
        /// Clean up WebView resources when the component is destroyed
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                // Clean up event subscription
                Application.focusChanged -= OnApplicationFocusChanged;

                // Dispose of the WebView
                if (webView != null)
                {
                    webView.Dispose();
                    webView = null;
                    PassportLogger.Info($"{TAG} WebView disposed on destroy");
                }

                // Stop any running coroutines
                if (inputActivationCoroutine != null)
                {
                    StopCoroutine(inputActivationCoroutine);
                    inputActivationCoroutine = null;
                    PassportLogger.Info($"{TAG} Stopped input activation coroutine during cleanup");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error during cleanup: {ex.Message}");
            }
        }

        private async UniTask WaitForPassportReady()
        {
            PassportLogger.Info($"{TAG} Waiting for main Passport bridge to be ready...");

            int attempts = 0;
            while (attempts < 150) // 15 second timeout
            {
                try
                {
                    if (_passportInstance?.Environment != null)
                    {
                        PassportLogger.Info($"{TAG} Main Passport bridge is ready");
                        return;
                    }
                }
                catch
                {
                    // Continue waiting
                }

                await UniTask.Delay(100);
                attempts++;
            }

            throw new TimeoutException("Passport bridge not ready after 15 seconds");
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
            {
                PassportLogger.Info($"{TAG} Application regained focus - WebView input should now work");
            }
            else
            {
                PassportLogger.Info($"{TAG} Application lost focus");
            }
        }
    }
}
