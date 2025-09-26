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
    /// Matches the TypeScript LoginData interface from the web page
    /// </summary>
    [System.Serializable]
    public class LoginData
    {
        public string directLoginMethod;
        public string email;
        public string marketingConsentStatus;

        public override string ToString()
        {
            return $"LoginData(directLoginMethod: {directLoginMethod}, email: {email}, marketingConsentStatus: {marketingConsentStatus})";
        }
    }

    /// <summary>
    /// Cross-platform WebView UI component for Passport authentication.
    /// Automatically selects the appropriate WebView implementation based on the target platform:
    /// - Windows: Unity Web Browser (UWB) with Chromium CEF
    /// - iOS/Android: Vuplex WebView with embedded browser
    /// - macOS: Vuplex WebView with embedded browser
    ///
    /// INITIALIZATION OPTIONS:
    /// 1. Simple: Call InitializeWithPassport() - PassportUI handles both Passport.Init() and UI setup
    /// 2. Hybrid: Call InitializeWithPassport(passport) - Use existing Passport instance with UI
    /// 3. Traditional: Call Passport.Init() yourself, then call Init(passport) - for advanced control
    ///
    /// SETUP: When configuring the PassportUI prefab in the editor:
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

        [Header("Debug Settings (Windows WebView Only)")]
        [Tooltip("Enable remote debugging for the Windows WebView (Volt Unity Web Browser). Not used on other platforms.")]
        public bool enableRemoteDebugging = false;
        [Tooltip("Port for remote debugging on Windows WebView (Volt Unity Web Browser). Not used on other platforms.")]
        public uint remoteDebuggingPort = 9222;

        [Header("Cache Settings (Windows WebView Only)")]
        [Tooltip("Clear WebView cache on login for Windows WebView (Volt Unity Web Browser). Not used on other platforms.")]
        public bool clearCacheOnLogin = true;

        [Header("Passport Configuration")]
        [Tooltip("Passport client ID from the Immutable Developer Hub")]
        [SerializeField] private string clientId = "";

        [Tooltip("Passport environment (sandbox or production)")]
        [SerializeField] private string environment = "sandbox";

        [Tooltip("Redirect URI for authentication callbacks (configure in the Immutable Developer Hub)")]
        [SerializeField] private string redirectUri = "";

        [Tooltip("OAuth logout redirect URI for logout callbacks (configure in the Immutable Developer Hub)")]
        [SerializeField] private string logoutRedirectUri = "";

        [Header("WebView Settings")]
        // Internal base URL for Passport authentication - users don't need to modify this
        private const string webViewBaseUrl = "https://auth.immutable.com/im-embedded-login-prompt";

        [Tooltip("Width of the WebView in pixels. Set to 0 to use the RawImage's current width.")]
        [SerializeField] private int webViewWidth = 800;

        [Tooltip("Height of the WebView in pixels. Set to 0 to use the RawImage's current height.")]
        [SerializeField] private int webViewHeight = 600;

        /// <summary>
        /// Gets the complete WebView URL with the client ID automatically appended
        /// </summary>
        public string WebViewUrl
        {
            get
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    return webViewBaseUrl;
                }
                return $"{webViewBaseUrl}?isWebView=true&client_id={clientId}";
            }
        }

        /// <summary>
        /// Gets the base WebView URL (without client_id parameter) - read-only
        /// </summary>
        public string WebViewBaseUrl => webViewBaseUrl;

        /// <summary>
        /// Gets or sets the WebView width in pixels
        /// </summary>
        public int WebViewWidth
        {
            get => webViewWidth;
            set
            {
                if (webViewWidth != value)
                {
                    webViewWidth = value;
                    UpdateWebViewResolution();
                }
            }
        }

        /// <summary>
        /// Gets or sets the WebView height in pixels
        /// </summary>
        public int WebViewHeight
        {
            get => webViewHeight;
            set
            {
                if (webViewHeight != value)
                {
                    webViewHeight = value;
                    UpdateWebViewResolution();
                }
            }
        }

        // Cross-platform WebView abstraction
        private IPassportWebView webView;

        private Passport _passportInstance;
        private RawImage rawImage;
        // Login completion source removed - OAuth handled by external browser
        private bool isInitialized = false;
        private GameObject bridgeWebViewGameObject;

        // Input management
        // Note: Input coroutine removed - games should manage cursor state themselves

        /// <summary>
        /// Auto-initialize PassportUI when the component starts if clientId is configured.
        /// This enables "drag and drop" functionality - just configure the Inspector fields and it works.
        /// </summary>
        private async void Start()
        {
            if (!isInitialized && !string.IsNullOrEmpty(clientId))
            {
                try
                {
                    PassportLogger.Info($"{TAG} Auto-initializing PassportUI from Start()...");
                    if (Passport.Instance != null)
                    {
                        PassportLogger.Info($"{TAG} Auto-initialization: Passport already exists, setting up UI only");
                        await InitializeWithPassport(Passport.Instance);
                    }
                    else
                    {
                        PassportLogger.Info($"{TAG} Auto-initialization: Creating new Passport instance");
                        await InitializeWithPassport();
                    }
                }
                catch (Exception ex)
                {
                    PassportLogger.Error($"{TAG} Auto-initialization failed: {ex.Message}");
                }
            }
            else if (string.IsNullOrEmpty(clientId))
            {
                PassportLogger.Warn($"{TAG} Auto-initialization skipped - Client ID not configured in Inspector");
            }
            else if (isInitialized)
            {
                PassportLogger.Info($"{TAG} Auto-initialization skipped - Already initialized");
            }
        }

        /// <summary>
        /// Initialize Passport and PassportUI in one call using the configured settings.
        /// This is the simple initialization method that handles both Passport.Init() and UI setup.
        /// Uses the serialized fields (clientId, environment, redirectUri, logoutRedirectUri) from the Unity Inspector.
        /// </summary>
        /// <returns>UniTask that completes when initialization is finished</returns>
        public async UniTask InitializeWithPassport()
        {
            if (isInitialized)
            {
                PassportLogger.Warn($"{TAG} PassportUI is already initialized, skipping InitializeWithPassport()");
                return;
            }

            // Validate configuration
            if (string.IsNullOrEmpty(clientId))
            {
                PassportLogger.Error($"{TAG} Client ID is required but not set in the Inspector");
                return;
            }

            try
            {
                PassportLogger.Info($"{TAG} Initializing Passport with client ID: {clientId}");

                // Initialize Passport using the configured settings
                var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);

                // Initialize the UI with the created Passport instance
                Init(passport);

                PassportLogger.Info($"{TAG} Passport and PassportUI initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize Passport: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize PassportUI with an existing Passport instance in one call.
        /// Use this method when you already have a Passport instance from elsewhere in your code
        /// and just want to set up the UI component.
        /// </summary>
        /// <param name="passportInstance">The existing initialized Passport instance</param>
        /// <returns>UniTask that completes when UI initialization is finished</returns>
        public async UniTask InitializeWithPassport(Passport passportInstance)
        {
            if (isInitialized)
            {
                PassportLogger.Warn($"{TAG} PassportUI is already initialized, skipping InitializeWithPassport(passport)");
                return;
            }

            if (passportInstance == null)
            {
                PassportLogger.Error($"{TAG} Passport instance cannot be null");
                return;
            }

            try
            {
                PassportLogger.Info($"{TAG} Initializing PassportUI with existing Passport instance");

                // Initialize the UI with the provided Passport instance
                Init(passportInstance);

                // Wait a frame to ensure initialization is complete
                await UniTask.Yield();

                PassportLogger.Info($"{TAG} PassportUI initialized successfully with existing Passport instance");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize PassportUI with existing Passport: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize PassportUI with an existing Passport instance.
        /// Use this method if you want to control Passport.Init() yourself.
        /// Following the working WebViewTest pattern.
        /// </summary>
        /// <param name="passportInstance">The initialized Passport instance</param>
        public void Init(Passport passportInstance)
        {
            if (isInitialized)
            {
                PassportLogger.Warn($"{TAG} PassportUI is already initialized, skipping Init(passport)");
                return;
            }

            if (passportInstance == null)
            {
                PassportLogger.Error($"{TAG} Passport instance cannot be null");
                return;
            }

            _passportInstance = passportInstance;
            PassportLogger.Info($"{TAG} Initializing PassportUI with working WebViewTest pattern...");

            // Get RawImage component
            rawImage = GetComponent<RawImage>();

            // Set transparent background so unused areas don't show white
            rawImage.color = Color.clear;

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

            isInitialized = true;
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
                    InitialUrl = "about:blank",
                    Width = webViewWidth > 0 ? webViewWidth : (int)rawImage.rectTransform.rect.width,
                    Height = webViewHeight > 0 ? webViewHeight : (int)rawImage.rectTransform.rect.height
                };

                // Initialize WebView
                webView.Initialize(config);

                // Subscribe to events
                webView.OnLoadFinished += OnWebViewLoadFinished;
                webView.OnLoadStarted += OnWebViewLoadStarted;

                // Register JavaScript methods
                webView.RegisterJavaScriptMethod("HandleLoginData", HandleLoginData);
                webView.RegisterJavaScriptMethod("HandleLoginError", HandleLoginError);
                webView.RegisterJavaScriptMethod("HandleClose", (data) => HideLoginUI());

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
#elif UNITY_IOS && VUPLEX_WEBVIEW
            PassportLogger.Info($"{TAG} Creating iOS WebView (Vuplex)");
            return new iOSPassportWebView(rawImage);
#elif UNITY_ANDROID && VUPLEX_WEBVIEW
            PassportLogger.Info($"{TAG} Creating Android WebView (Vuplex)");
            return new AndroidVuplexWebView(rawImage);
#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && VUPLEX_WEBVIEW
            PassportLogger.Info($"{TAG} Creating MacOS WebView (Vuplex)");
            return new MacOSPassportWebView(rawImage);
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

                // Validate required login data
                if (string.IsNullOrEmpty(loginData.directLoginMethod))
                {
                    PassportLogger.Error($"{TAG} Login method is required but was null or empty");
                    return;
                }

                // For now, just log the data - in the future this could trigger OAuth flow
                PassportLogger.Info($"{TAG} Login attempt - Method: {loginData.directLoginMethod}, Email: {loginData.email}, Marketing Consent: {loginData.marketingConsentStatus}");

                // Login(bool useCachedSession = false, DirectLoginOptions directLoginOptions = null)
                DirectLoginMethod loginMethod;
                switch (loginData.directLoginMethod)
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
                        PassportLogger.Error($"{TAG} Invalid login method: {loginData.directLoginMethod}");
                        return;
                }
                var loginOptions = new DirectLoginOptions(loginMethod);

                // Validate email is provided for email login method
                if (loginMethod == DirectLoginMethod.Email)
                {
                    if (string.IsNullOrEmpty(loginData.email))
                    {
                        PassportLogger.Error($"{TAG} Email is required for email login method but was null or empty");
                        return;
                    }

                    loginOptions.email = loginData.email;
                }

                // Parse and set marketing consent status if provided
                if (!string.IsNullOrEmpty(loginData.marketingConsentStatus))
                {
                    switch (loginData.marketingConsentStatus.ToLower())
                    {
                        case "opted_in":
                            loginOptions.marketingConsentStatus = MarketingConsentStatus.OptedIn;
                            break;
                        case "unsubscribed":
                            loginOptions.marketingConsentStatus = MarketingConsentStatus.Unsubscribed;
                            break;
                        default:
                            PassportLogger.Warn($"{TAG} Unknown marketing consent status: {loginData.marketingConsentStatus}");
                            break;
                    }
                }

                // Perform the login and handle the result
                PassportLogger.Info($"{TAG} Starting login with method: {loginData.directLoginMethod}");
                bool loginSuccess = await _passportInstance.Login(false, loginOptions);

                if (loginSuccess)
                {
                    PassportLogger.Info($"{TAG} Login successful!");

                    // Trigger events
                    OnLoginSuccess?.Invoke();
                    OnLoginSuccessStatic?.Invoke();

                    // Hide the WebView after successful login
                    HideLoginUI();
                }
                else
                {
                    string errorMessage = "Login failed - authentication was not completed";
                    PassportLogger.Error($"{TAG} {errorMessage}");

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

        /// <summary>
        /// Handles error messages received from JavaScript
        /// Called when the login page encounters an error and sends error information
        /// </summary>
        /// <param name="jsonData">JSON string containing error data</param>
        private void HandleLoginError(string jsonData)
        {
            try
            {
                PassportLogger.Info($"{TAG} Received error data from JavaScript: {jsonData}");

                // Parse the JSON error data
                ErrorData errorData = JsonUtility.FromJson<ErrorData>(jsonData);
                PassportLogger.Error($"{TAG} Login page error: {errorData}");

                // Create user-friendly error message
                string errorMessage = !string.IsNullOrEmpty(errorData.message)
                    ? $"Login failed: {errorData.message}"
                    : "Login failed due to an unknown error";

                // Trigger failure events
                OnLoginFailure?.Invoke(errorMessage);
                OnLoginFailureStatic?.Invoke(errorMessage);

                // Hide the WebView after error
                HideLoginUI();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to handle error message from JavaScript: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                // Trigger failure events even if we can't parse the error
                OnLoginFailure?.Invoke("Login failed due to an error processing error message");
                OnLoginFailureStatic?.Invoke("Login failed due to an error processing error message");
            }
        }

        /// <summary>
        /// Update the WebView internal resolution when PassportUI dimensions change
        /// </summary>
        private void UpdateWebViewResolution()
        {
            if (webView != null && isInitialized && webViewWidth > 0 && webViewHeight > 0)
            {
                // For Windows UWB, update the internal resolution
                if (webView is WindowsPassportWebView windowsWebView)
                {
                    windowsWebView.UpdateUWBResolution(webViewWidth, webViewHeight);
                }
                // For other platforms (Vuplex), the RectTransform size is sufficient
                // as they don't have separate internal resolution properties
            }
        }

        /// <summary>
        /// Unity Update method to handle main thread operations
        /// </summary>
        private void Update()
        {
            // Check for pending resolution updates on Windows WebView
            if (webView is WindowsPassportWebView windowsWebView)
            {
                windowsWebView.UpdatePendingResolution();
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
                PassportLogger.Info($"{TAG} Initializing deep link system for OAuth callbacks");
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
                        PassportLogger.Info($"{TAG} Deep link system initialized for protocol: {redirectUri}");
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

                webView.Show();
                PassportLogger.Info($"{TAG} WebView shown");

                webView.LoadUrl(WebViewUrl);
                PassportLogger.Info($"{TAG} Navigated to configured URL: {WebViewUrl}");

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

                // Note: Input coroutine handling removed - games should manage cursor state themselves
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
