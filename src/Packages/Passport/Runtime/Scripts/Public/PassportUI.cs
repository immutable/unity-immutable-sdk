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

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Input;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltstroStudios.UnityWebBrowser.Shared.Popups;
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



#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Windows API calls for window focus
        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern System.IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetFocus(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(System.IntPtr hWnd);
#endif

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


#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
        private GameObject uwbGameObject;
        private WebBrowserUIFull webBrowserUI;
        private WebBrowserClient webBrowserClient;
#endif
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

            // Create WebView following VoltUnityWebBrowserAdapter pattern
            CreateWebViewInstance();

            // Set up login button
            SetupLoginButton();

            // Hide initially
            HideLoginUI(logMessage: false);

            PassportLogger.Info($"{TAG} PassportUI initialized successfully");
        }

        private void CreateWebViewInstance()
        {
#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
            try
            {
                PassportLogger.Info($"{TAG} Creating isolated UWB instance (following WebViewTest pattern)...");

                // Check if WebView already exists (prevent double creation)
                if (uwbGameObject != null)
                {
                    PassportLogger.Warn($"{TAG} WebView GameObject already exists! Destroying previous instance.");
                    DestroyImmediate(uwbGameObject);
                    uwbGameObject = null;
                }

                // Check for existing WebBrowser components in the scene
                var existingBrowsers = FindObjectsOfType<WebBrowserUIFull>();
                PassportLogger.Info($"{TAG} Found {existingBrowsers.Length} existing WebBrowserUIFull components in scene");
                for (int i = 0; i < existingBrowsers.Length; i++)
                {
                    PassportLogger.Info($"{TAG} Existing WebBrowser {i}: {existingBrowsers[i].gameObject.name}");
                }

                // Find or create Canvas (following VoltUnityWebBrowserAdapter pattern)
                Canvas canvas = FindOrCreateCanvas();

                // Create UWB GameObject as child of Canvas with unique name
                uwbGameObject = new GameObject("PassportUI_WebView");
                uwbGameObject.transform.SetParent(canvas.transform, false);

                // IMPORTANT: Start hidden to avoid blocking UI elements
                uwbGameObject.SetActive(false);

                // Add RectTransform for UI positioning
                RectTransform rectTransform = uwbGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                PassportLogger.Info($"{TAG} Adding WebBrowserUIFull component (separate from SDK bridge)");

                // Add WebBrowserUIFull component - LET UNITY HANDLE INITIALIZATION
                // This ensures this instance is completely isolated from the SDK's bridge instance
                webBrowserUI = uwbGameObject.AddComponent<WebBrowserUIFull>();

                // Configure input handling
                ConfigureInputHandler();

                // Get the browser client and configure for UI display
                webBrowserClient = webBrowserUI.browserClient;

                PassportLogger.Info($"{TAG} Configuring UI instance (headless=false, separate from SDK)");
                webBrowserClient.headless = false; // SDK uses headless=true, we need UI
                webBrowserClient.popupAction = PopupAction.OpenExternalWindow;

                // CRITICAL: Set initial URL to prevent loading voltstro.dev default
                webBrowserClient.initialUrl = "about:blank";
                PassportLogger.Info($"{TAG} Set initial URL to about:blank (preventing voltstro.dev default)");

                // Ensure complete isolation from SDK's UWB instance
                ConfigureIsolatedInstance();

                // Subscribe to events for display purposes only (no callback interception)
                webBrowserClient.OnLoadFinish += OnLoadFinishHandler;
                webBrowserClient.OnLoadStart += OnLoadStartHandler;
                // Note: Removed OnUrlChanged - we let OAuth redirects go to external browser for security

                // Enable JavaScript methods and register handlers
                SetupJavaScriptMethods();

                // Debug input setup
                PassportLogger.Info($"{TAG} Input Debug - RawImage: {(uwbGameObject.GetComponent<RawImage>() != null)}");
                PassportLogger.Info($"{TAG} Input Debug - InputHandler: {(webBrowserUI.inputHandler != null)} ({webBrowserUI.inputHandler?.GetType().Name})");
                PassportLogger.Info($"{TAG} Input Debug - EventSystem: {(FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)}");
                PassportLogger.Info($"{TAG} Input Debug - GraphicRaycaster: {(canvas.GetComponent<GraphicRaycaster>() != null)}");

                isInitialized = true;
                PassportLogger.Info($"{TAG} Isolated UWB instance created successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to create WebView instance: {ex.Message}");
            }
#else
            PassportLogger.Error($"{TAG} WebView not supported on this platform");
#endif
        }

        // Loading UI methods removed - was causing input interference

        private Canvas FindOrCreateCanvas()
        {
            // Try to find existing Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                PassportLogger.Info($"{TAG} Using existing Canvas: {canvas.name}");
                return canvas;
            }

            // Create new Canvas if none exists
            GameObject canvasObj = new GameObject("PassportUI_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Add EventSystem if missing
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            PassportLogger.Info($"{TAG} Created new Canvas: {canvasObj.name}");
            return canvas;
        }

#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
        private void ConfigureInputHandler()
        {
            try
            {
                if (webBrowserUI.inputHandler == null)
                {
                    PassportLogger.Info($"{TAG} Loading WebBrowser Input Handler asset...");

                    // Load the BrowserInput asset from SDK's Resources folder
                    var inputHandlerAsset = Resources.Load<WebBrowserOldInputHandler>("BrowserInput");

                    if (inputHandlerAsset != null)
                    {
                        PassportLogger.Info($"{TAG} Using SDK BrowserInput asset: {inputHandlerAsset.GetType().Name}");
                        webBrowserUI.inputHandler = inputHandlerAsset;
                    }
                    else
                    {
                        // Fallback: Create programmatically if asset not found in SDK
                        PassportLogger.Warn($"{TAG} BrowserInput asset not found in SDK Resources, creating programmatically...");

#if ENABLE_INPUT_SYSTEM
                        // Use new Input System if available
                        PassportLogger.Info($"{TAG} Using New Input System (Input System Package)");
                        var inputHandler = ScriptableObject.CreateInstance<WebBrowserInputSystemHandler>();
                        PassportLogger.Info($"{TAG} Input Handler created: WebBrowserInputSystemHandler");
#else
                        // Fall back to legacy Input Manager
                        PassportLogger.Info($"{TAG} Using Legacy Input Manager (Old Input System)");
                        var inputHandler = ScriptableObject.CreateInstance<WebBrowserOldInputHandler>();
                        PassportLogger.Info($"{TAG} Input Handler created: WebBrowserOldInputHandler");
#endif
                        webBrowserUI.inputHandler = inputHandler;
                    }

                    PassportLogger.Info($"{TAG} Input Handler assigned to WebBrowserUIFull");
                }
                else
                {
                    PassportLogger.Info($"{TAG} Input Handler already exists: {webBrowserUI.inputHandler.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to configure input handler: {ex.Message}");
                PassportLogger.Error($"{TAG} You may need to manually assign an Input Handler in the Inspector");
            }
        }

        private void ConfigureIsolatedInstance()
        {
            try
            {
                PassportLogger.Info($"{TAG} Configuring isolated UWB instance...");

                // CRITICAL: Use completely different ports from SDK's bridge instance
                var tcpLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();

                // SDK bridge uses random ports in range 1024-65353 (default 5555/5556)
                // Use fixed, well-separated ports for UI WebView to avoid conflicts
                int basePort = 7777; // Well above default 5555/5556, easy to remember
                int attempts = 0;

                do
                {
                    tcpLayer.inPort = basePort + (attempts * 2);     // 7777, 7779, 7781...
                    tcpLayer.outPort = tcpLayer.inPort + 1;          // 7778, 7780, 7782...
                    attempts++;
                    if (attempts > 50) break; // Try up to port 7877
                } while (!IsPortAvailable(tcpLayer.inPort) || !IsPortAvailable(tcpLayer.outPort));

                webBrowserClient.communicationLayer = tcpLayer;
                PassportLogger.Info($"{TAG} Using isolated ports: {tcpLayer.inPort}/{tcpLayer.outPort}");

                // Configure engine
                ConfigureEngine();

                // Configure cache path (isolated from SDK)
                var cacheDir = Path.Combine(Application.persistentDataPath, "PassportUI_UWBCache");
                webBrowserClient.CachePath = new FileInfo(cacheDir);
                PassportLogger.Info($"{TAG} Using isolated cache: {cacheDir}");

                // Configure remote debugging if enabled
                if (enableRemoteDebugging)
                {
                    webBrowserClient.remoteDebugging = true;
                    webBrowserClient.remoteDebuggingPort = remoteDebuggingPort;
                    PassportLogger.Info($"{TAG} Remote debugging enabled on port {remoteDebuggingPort}");
                }

                PassportLogger.Info($"{TAG} Isolated instance configured successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to configure isolated instance: {ex.Message}");
            }
        }

        private void ConfigureEngine()
        {
            try
            {
                PassportLogger.Info($"{TAG} Configuring CEF engine...");

                var engineConfig = ScriptableObject.CreateInstance<EngineConfiguration>();
                engineConfig.engineAppName = "UnityWebBrowser.Engine.Cef";

                var engineFiles = new System.Collections.Generic.List<Engine.EnginePlatformFiles>();

                // Windows engine configuration
                engineFiles.Add(new Engine.EnginePlatformFiles()
                {
                    platform = Platform.Windows64,
                    engineBaseAppLocation = "",
                    engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
                    ,
                    engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.win.x64@2.2.5-130.1.16/Engine~"
#endif
                });

                engineConfig.engineFiles = engineFiles.ToArray();
                webBrowserClient.engine = engineConfig;

                PassportLogger.Info($"{TAG} CEF engine configured successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to configure engine: {ex.Message}");
            }
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                var tcpConnInfoArray = System.Net.NetworkInformation.IPGlobalProperties
                    .GetIPGlobalProperties().GetActiveTcpListeners();
                return System.Linq.Enumerable.All(tcpConnInfoArray, endpoint => endpoint.Port != port);
            }
            catch
            {
                return false;
            }
        }

        private void OnLoadFinishHandler(string url)
        {
            PassportLogger.Info($"{TAG} Page loaded: {url}");

            // Loading UI removed
            if (!url.StartsWith("about:blank"))
            {
                PassportLogger.Info($"{TAG} Content loaded");
            }

            // Force window focus when page loads (helps with input in builds)
            TryForceWindowFocus();

            // Give WebView a moment to fully initialize input handling (prevent multiple coroutines)
            if (inputActivationCoroutine != null)
            {
                StopCoroutine(inputActivationCoroutine);
                PassportLogger.Info($"{TAG} Stopped previous input activation coroutine");
            }
            inputActivationCoroutine = StartCoroutine(DelayedInputActivation());

            // OAuth callbacks handled by external browser for security - no interception needed
        }

        private void OnLoadStartHandler(string url)
        {
            PassportLogger.Info($"{TAG} Loading started: {url}");
            // OAuth callbacks handled by external browser for security - no interception needed
        }

        /// <summary>
        /// Sets up JavaScript methods for communication between the WebView and Unity
        /// </summary>
        private void SetupJavaScriptMethods()
        {
            try
            {
                // Enable JavaScript methods
                webBrowserClient.jsMethodManager.jsMethodsEnable = true;
                PassportLogger.Info($"{TAG} JavaScript methods enabled");

                // Register methods using the simpler Action<string> approach as per UWB documentation
                webBrowserClient.RegisterJsMethod<string>("HandleLoginData", HandleLoginData);
                PassportLogger.Info($"{TAG} Registered JavaScript method: HandleLoginData");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to setup JavaScript methods: {ex.Message}");
            }
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


#endif

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

                // Clear cache if requested
                if (clearCacheOnLogin)
                {
                    ClearWebViewCache();
                }

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
                if (uwbGameObject != null)
                {
                    uwbGameObject.SetActive(true);

                    // Re-enable and resize the RawImage for display
                    if (rawImage != null)
                    {
                        rawImage.enabled = true;
                        var rectTransform = rawImage.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            // Set to full screen
                            rectTransform.anchorMin = Vector2.zero;
                            rectTransform.anchorMax = Vector2.one;
                            rectTransform.sizeDelta = Vector2.zero;
                            rectTransform.offsetMin = Vector2.zero;
                            rectTransform.offsetMax = Vector2.zero;
                            PassportLogger.Info($"{TAG} RawImage restored for WebView display");
                        }
                    }

#if UNITY_EDITOR
                    // CRITICAL: Select the WebView GameObject to enable input focus
                    // This is required for keyboard/mouse input to work properly in the WebView
                    UnityEditor.Selection.activeGameObject = uwbGameObject;
                    PassportLogger.Info($"{TAG} WebView GameObject selected for input focus (Editor)");
#endif

                    // Additional focus handling for runtime
                    try
                    {
                        // Force application window focus (critical for WebView input in builds)
                        Application.focusChanged += OnApplicationFocusChanged;

                        // Ensure Unity application window has focus
                        if (!Application.isFocused)
                        {
                            PassportLogger.Info($"{TAG} Application not focused - attempting to bring window to front");

                            // Alternative: Force window activation (Windows-specific)
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                            try
                            {
                                var handle = GetActiveWindow();
                                SetForegroundWindow(handle);
                                PassportLogger.Info($"{TAG} Windows SetForegroundWindow called");
                            }
                            catch (System.Exception winEx)
                            {
                                PassportLogger.Warn($"{TAG} Windows focus call failed: {winEx.Message}");
                            }
#endif
                        }

                        // Ensure the WebView gets focus when shown
                        var webBrowserComponent = uwbGameObject.GetComponent<WebBrowserUIFull>();
                        if (webBrowserComponent != null)
                        {
                            PassportLogger.Info($"{TAG} WebBrowserUIFull component focused for input");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        PassportLogger.Warn($"{TAG} Could not set WebView focus: {ex.Message}");
                    }
                }

#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
                // Wait for WebView to be ready
                await WaitForWebBrowserReady();

                // Navigate to test page
                webBrowserClient.LoadUrl(testPageUrl);
                PassportLogger.Info($"{TAG} Navigated to test page: {testPageUrl}");

                // Start input preparation immediately after navigation
                StartCoroutine(EarlyInputPreparation());
#endif

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
            // Loading UI removed

            if (uwbGameObject != null)
            {
                uwbGameObject.SetActive(false);
                if (logMessage)
                {
                    PassportLogger.Info($"{TAG} Login UI hidden");
                }
            }

            // Also hide and resize the RawImage
            if (rawImage != null)
            {
                rawImage.enabled = false;
                var rectTransform = rawImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = Vector2.zero;
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

        // ShowLoadingUI and HideLoadingUI methods removed

        // AnimateSpinner method removed



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

#if !IMMUTABLE_CUSTOM_BROWSER && UNITY_STANDALONE_WIN
        private async UniTask WaitForWebBrowserReady()
        {
            PassportLogger.Info($"{TAG} Waiting for WebBrowser to be ready...");

            int attempts = 0;
            while (attempts < 100) // 10 second timeout
            {
                if (webBrowserClient != null && webBrowserClient.ReadySignalReceived && webBrowserClient.IsConnected)
                {
                    PassportLogger.Info($"{TAG} WebBrowser is ready");
                    return;
                }

                await UniTask.Delay(100);
                attempts++;
            }

            throw new TimeoutException("WebBrowser not ready after 10 seconds");
        }
#endif

        // ProcessOAuthCallback removed - OAuth handled by external browser for security

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

        private void TryForceWindowFocus()
        {
            try
            {
                PassportLogger.Info($"{TAG} Attempting to force window focus for WebView input");

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                // Windows-specific aggressive keyboard focus
                var currentWindow = GetForegroundWindow();
                var activeWindow = GetActiveWindow();

                // Try multiple approaches to ensure keyboard focus
                if (currentWindow != activeWindow)
                {
                    SetForegroundWindow(activeWindow);
                    PassportLogger.Info($"{TAG} SetForegroundWindow called");
                }

                // Also try SetFocus specifically for keyboard input
                SetFocus(activeWindow);
                PassportLogger.Info($"{TAG} SetFocus called for keyboard input");

                // Bring window to top as additional measure
                BringWindowToTop(activeWindow);
                PassportLogger.Info($"{TAG} BringWindowToTop called");

                // Try to find Unity window by title and focus it
                try
                {
                    var unityWindow = FindWindow(null, Application.productName);
                    if (unityWindow != System.IntPtr.Zero)
                    {
                        SetForegroundWindow(unityWindow);
                        SetFocus(unityWindow);
                        PassportLogger.Info($"{TAG} Found and focused Unity window by title");
                    }
                }
                catch (System.Exception ex)
                {
                    PassportLogger.Warn($"{TAG} Could not find Unity window by title: {ex.Message}");
                }
#endif

                // Cross-platform check: warn if application doesn't have focus
                if (!Application.isFocused)
                {
                    PassportLogger.Warn($"{TAG} Unity application not focused - if keyboard input doesn't work, click on the window title bar");

                    // Try to show a brief visual indicator to user
                    StartCoroutine(ShowFocusInstructions());
                }
            }
            catch (System.Exception ex)
            {
                PassportLogger.Warn($"{TAG} Could not force window focus: {ex.Message}");
            }
        }

        private void TrySimulateWebViewClick()
        {
            try
            {
                PassportLogger.Info($"{TAG} Forcing WebView pointer enter to activate keyboard input");

                if (uwbGameObject != null)
                {
                    var webBrowserComponent = uwbGameObject.GetComponent<WebBrowserUIFull>();
                    if (webBrowserComponent != null)
                    {
                        // Check input handler state
                        if (webBrowserComponent.inputHandler != null)
                        {
                            PassportLogger.Info($"{TAG} Input handler: {webBrowserComponent.inputHandler.GetType().Name}");
                            PassportLogger.Info($"{TAG} Keyboard disabled: {webBrowserComponent.disableKeyboardInputs}");
                            PassportLogger.Info($"{TAG} Mouse disabled: {webBrowserComponent.disableMouseInputs}");
                        }
                        else
                        {
                            PassportLogger.Warn($"{TAG} Input handler is NULL!");
                        }

                        // ENHANCED DEBUGGING: Check ALL components that could cause input duplication
                        var allInputHandlers = FindObjectsOfType<VoltstroStudios.UnityWebBrowser.Core.RawImageUwbClientInputHandler>();
                        var allWebBrowserUIFull = FindObjectsOfType<WebBrowserUIFull>();
                        var allWebBrowserNoUi = FindObjectsOfType<VoltstroStudios.UnityWebBrowser.WebBrowserNoUi>();
                        var allWebBrowserInputHandlers = FindObjectsOfType<VoltstroStudios.UnityWebBrowser.Input.WebBrowserInputHandler>();
                        var allEventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
                        var allInputModules = FindObjectsOfType<UnityEngine.EventSystems.StandaloneInputModule>();

                        PassportLogger.Warn($"{TAG} 🔍 ENHANCED INPUT DEBUGGING:");
                        PassportLogger.Info($"{TAG} Found {allInputHandlers.Length} RawImageUwbClientInputHandler components");
                        PassportLogger.Info($"{TAG} Found {allWebBrowserUIFull.Length} WebBrowserUIFull components");
                        PassportLogger.Info($"{TAG} Found {allWebBrowserNoUi.Length} WebBrowserNoUi components (bridge)");
                        PassportLogger.Info($"{TAG} Found {allWebBrowserInputHandlers.Length} WebBrowserInputHandler ScriptableObjects");
                        PassportLogger.Info($"{TAG} Found {allEventSystems.Length} EventSystem components");
                        PassportLogger.Info($"{TAG} Found {allInputModules.Length} StandaloneInputModule components");

                        // CRITICAL: Check for multiple EventSystems (major cause of duplicate input!)
                        if (allEventSystems.Length > 1)
                        {
                            PassportLogger.Warn($"{TAG} ⚠️🔥 MULTIPLE EVENT SYSTEMS DETECTED - THIS IS THE ROOT CAUSE!");
                            for (int i = 0; i < allEventSystems.Length; i++)
                            {
                                var currentEventSystem = allEventSystems[i];
                                PassportLogger.Warn($"{TAG} EventSystem {i}: {currentEventSystem.gameObject.name} (Active: {currentEventSystem.gameObject.activeSelf})");

                                // DISABLE all EventSystems except the first one
                                if (i > 0 && currentEventSystem.gameObject.activeSelf)
                                {
                                    currentEventSystem.gameObject.SetActive(false);
                                    PassportLogger.Warn($"{TAG} 🔥 DISABLED extra EventSystem {currentEventSystem.gameObject.name}!");
                                }
                            }
                        }

                        // DETAILED INPUT HANDLER ANALYSIS
                        for (int i = 0; i < allInputHandlers.Length; i++)
                        {
                            var handler = allInputHandlers[i];
                            PassportLogger.Info($"{TAG} RawImageInputHandler {i}:");
                            PassportLogger.Info($"{TAG}   GameObject: {handler.gameObject.name}");
                            PassportLogger.Info($"{TAG}   Active: {handler.gameObject.activeSelf}");
                            PassportLogger.Info($"{TAG}   KeyboardDisabled: {handler.disableKeyboardInputs}");
                            PassportLogger.Info($"{TAG}   InputHandler: {(handler.inputHandler != null ? handler.inputHandler.name : "NULL")}");
                            PassportLogger.Info($"{TAG}   InputHandler Type: {(handler.inputHandler != null ? handler.inputHandler.GetType().Name : "NULL")}");
                            PassportLogger.Info($"{TAG}   Same Asset: {(handler.inputHandler != null && webBrowserUI.inputHandler != null && handler.inputHandler == webBrowserUI.inputHandler ? "YES - SHARED!" : "NO")}");
                        }

                        // Check ScriptableObject instances
                        if (allWebBrowserInputHandlers.Length > 0)
                        {
                            for (int i = 0; i < allWebBrowserInputHandlers.Length; i++)
                            {
                                var inputHandlerAsset = allWebBrowserInputHandlers[i];
                                PassportLogger.Info($"{TAG} InputHandler Asset {i}: {inputHandlerAsset.name} (Type: {inputHandlerAsset.GetType().Name})");
                            }
                        }

                        // Disable keyboard input on all OTHER input handlers
                        if (allInputHandlers.Length > 1)
                        {
                            PassportLogger.Warn($"{TAG} ⚠️ Multiple input handlers detected - this WILL cause character repetition!");
                            for (int i = 0; i < allInputHandlers.Length; i++)
                            {
                                var handler = allInputHandlers[i];
                                PassportLogger.Info($"{TAG} Input handler {i}: {handler.gameObject.name}");

                                // Disable keyboard input on all handlers except our WebView
                                if (handler.gameObject != uwbGameObject)
                                {
                                    handler.disableKeyboardInputs = true;
                                    PassportLogger.Warn($"{TAG} ⚠️ DISABLED keyboard input on {handler.gameObject.name} to prevent duplicates!");
                                }
                                else
                                {
                                    PassportLogger.Info($"{TAG} ✅ Keeping keyboard input enabled on our WebView: {handler.gameObject.name}");
                                }
                            }
                        }

                        // CRITICAL: Temporarily disable bridge WebBrowser GameObject to prevent input conflicts
                        for (int i = 0; i < allWebBrowserNoUi.Length; i++)
                        {
                            var noUiBrowser = allWebBrowserNoUi[i];
                            PassportLogger.Info($"{TAG} WebBrowserNoUi {i}: {noUiBrowser.gameObject.name}");

                            // TEMPORARILY disable the bridge GameObject to prevent input processing
                            // This is safe because the bridge only needs to be active when we're not showing UI
                            if (noUiBrowser.gameObject.activeSelf)
                            {
                                noUiBrowser.gameObject.SetActive(false);
                                PassportLogger.Warn($"{TAG} ⚠️ TEMPORARILY DISABLED bridge WebView {noUiBrowser.gameObject.name} to prevent input conflicts!");
                                PassportLogger.Warn($"{TAG} Bridge will be re-enabled when UI WebView is hidden");

                                // Store reference so we can re-enable it later
                                if (bridgeWebViewGameObject == null)
                                {
                                    bridgeWebViewGameObject = noUiBrowser.gameObject;
                                }
                            }
                            else
                            {
                                PassportLogger.Info($"{TAG} Bridge WebView {noUiBrowser.gameObject.name} already disabled");
                            }
                        }

                        // Also disable keyboard input on all OTHER WebBrowserUIFull components
                        if (allWebBrowserUIFull.Length > 1)
                        {
                            PassportLogger.Warn($"{TAG} ⚠️ Multiple WebBrowserUIFull components detected!");
                            for (int i = 0; i < allWebBrowserUIFull.Length; i++)
                            {
                                var browser = allWebBrowserUIFull[i];
                                PassportLogger.Info($"{TAG} WebBrowserUIFull {i}: {browser.gameObject.name}");

                                // Disable keyboard input on all browsers except our WebView
                                if (browser.gameObject != uwbGameObject)
                                {
                                    browser.disableKeyboardInputs = true;
                                    PassportLogger.Warn($"{TAG} ⚠️ DISABLED keyboard input on WebBrowserUIFull {browser.gameObject.name}!");
                                }
                            }
                        }

                        // CRITICAL: Force OnPointerEnter to start keyboard coroutine
                        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                        if (eventSystem != null)
                        {
                            var pointerEventData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
                            var rectTransform = uwbGameObject.GetComponent<RectTransform>();

                            if (rectTransform != null)
                            {
                                // Set pointer position to center of WebView
                                Vector2 centerPosition = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
                                pointerEventData.position = centerPosition;

                                // Force trigger OnPointerEnter - this starts the keyboard input coroutine!
                                // But only do it once to prevent multiple coroutines
                                if (!pointerEnterTriggered)
                                {
                                    webBrowserComponent.OnPointerEnter(pointerEventData);
                                    pointerEnterTriggered = true;
                                    PassportLogger.Info($"{TAG} ✅ Forced OnPointerEnter - keyboard coroutine should now be running!");
                                    PassportLogger.Info($"{TAG} OnPointerEnter flag set to prevent duplicates");
                                }
                                else
                                {
                                    PassportLogger.Info($"{TAG} Skipping OnPointerEnter - already triggered to prevent duplicates");
                                    PassportLogger.Info($"{TAG} pointerEnterTriggered flag = {pointerEnterTriggered}");
                                }

                                // Also simulate a brief mouse movement to ensure GetMousePosition works
                                StartCoroutine(SimulateMouseHover());

                                // Select in EventSystem
                                eventSystem.SetSelectedGameObject(uwbGameObject);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                PassportLogger.Warn($"{TAG} Failed to force pointer enter: {ex.Message}");
            }
        }

        /// <summary>
        /// Test if GetMousePosition is working for keyboard input
        /// </summary>
        private IEnumerator SimulateMouseHover()
        {
            yield return new WaitForSeconds(0.1f);

            if (uwbGameObject != null)
            {
                var webBrowserComponent = uwbGameObject.GetComponent<WebBrowserUIFull>();
                if (webBrowserComponent != null)
                {
                    // Test if GetMousePosition is working (required for keyboard input)
                    bool mousePositionWorking = webBrowserComponent.GetMousePosition(out Vector2 pos);
                    PassportLogger.Info($"{TAG} GetMousePosition working: {mousePositionWorking}, pos: {pos}");

                    if (!mousePositionWorking)
                    {
                        PassportLogger.Warn($"{TAG} ⚠️ GetMousePosition failed - this will prevent keyboard input!");
                        PassportLogger.Info($"{TAG} Tip: Move mouse over the WebView area to activate keyboard input");
                    }
                    else
                    {
                        PassportLogger.Info($"{TAG} ✅ GetMousePosition working - keyboard input should be active!");
                    }
                }
            }
        }

        private IEnumerator ShowFocusInstructions()
        {
            // Wait a moment, then check if focus was gained
            yield return new WaitForSeconds(1f);

            if (!Application.isFocused)
            {
                PassportLogger.Info($"{TAG} Tip: If keyboard input doesn't work, click on the window title bar to give focus to the application");
            }
        }

        private IEnumerator EarlyInputPreparation()
        {
            // Start input preparation early, right after navigation
            yield return new WaitForSeconds(0.5f);

            PassportLogger.Info($"{TAG} Starting early input preparation...");
            TryActivateInput();

            // Continue with periodic activation attempts
            yield return new WaitForSeconds(1f);
            TryActivateInput();
        }

        private IEnumerator DelayedInputActivation()
        {
            // Wait for WebView to fully initialize input handling
            // This addresses the 5-10 second delay before input becomes responsive
            yield return new WaitForSeconds(2f);

            PassportLogger.Info($"{TAG} Performing delayed input activation...");

            // Try multiple activation strategies with small delays
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(1f);
                TryActivateInput();
            }

            // Clear the coroutine reference when done
            inputActivationCoroutine = null;
            PassportLogger.Info($"{TAG} DelayedInputActivation coroutine completed");
        }

        private void TryActivateInput()
        {
            try
            {
#if UNITY_EDITOR
                // In editor, ensure GameObject is still selected
                if (uwbGameObject != null)
                {
                    UnityEditor.Selection.activeGameObject = uwbGameObject;
                }
#endif

                // Force focus again
                TryForceWindowFocus();

                // Try to simulate a click on the WebView to activate keyboard focus
                TrySimulateWebViewClick();

                // Try to interact with the WebView component to wake up input
                if (uwbGameObject != null)
                {
                    var webBrowserComponent = uwbGameObject.GetComponent<WebBrowserUIFull>();
                    if (webBrowserComponent != null && webBrowserComponent.browserClient != null)
                    {
                        // Check if WebView is ready for input
                        bool isConnected = webBrowserComponent.browserClient.IsConnected;
                        bool isReady = webBrowserComponent.browserClient.ReadySignalReceived;
                        bool isInitialized = webBrowserComponent.browserClient.HasInitialized;

                        PassportLogger.Info($"{TAG} WebView status - Connected: {isConnected}, Ready: {isReady}, Initialized: {isInitialized}");

                        if (isConnected && isReady && isInitialized)
                        {
                            PassportLogger.Info($"{TAG} ✅ WebView is fully ready for input!");

                            // Try to focus an input field in the WebView
                            TryFocusWebViewInput();
                        }
                        else
                        {
                            PassportLogger.Info($"{TAG} ⏳ WebView still initializing, input may not work yet...");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                PassportLogger.Warn($"{TAG} Input activation attempt failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear the WebView cache
        /// </summary>
        public void ClearWebViewCache()
        {
            try
            {
                var cacheDir = Path.Combine(Application.persistentDataPath, "PassportUI_UWBCache");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                    PassportLogger.Info($"{TAG} Cleared WebView cache: {cacheDir}");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"{TAG} Failed to clear cache: {ex.Message}");
            }
        }

        private void TryFocusWebViewInput()
        {
            try
            {
                PassportLogger.Info($"{TAG} Attempting to focus input field in WebView");

                if (uwbGameObject != null)
                {
                    var webBrowserComponent = uwbGameObject.GetComponent<WebBrowserUIFull>();
                    if (webBrowserComponent != null && webBrowserComponent.browserClient != null)
                    {
                        // Try to execute JavaScript to focus the first input field
                        string focusScript = @"
                            (function() {
                                // Find the first visible input field
                                var inputs = document.querySelectorAll('input[type=""text""], input[type=""email""], input[type=""password""], textarea');
                                for (var i = 0; i < inputs.length; i++) {
                                    var input = inputs[i];
                                    if (input.offsetParent !== null) { // is visible
                                        input.focus();
                                        console.log('Focused input field: ' + input.type);
                                        return true;
                                    }
                                }

                                // If no input found, just click on the body to ensure focus
                                document.body.click();
                                console.log('No input field found, clicked body for focus');
                                return false;
                            })();
                        ";

                        webBrowserComponent.browserClient.ExecuteJs(focusScript);
                        PassportLogger.Info($"{TAG} Executed JavaScript to focus input field");
                    }
                }
            }
            catch (System.Exception ex)
            {
                PassportLogger.Warn($"{TAG} Could not focus WebView input: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // Clean up event subscription
            Application.focusChanged -= OnApplicationFocusChanged;

            // Stop any running input activation coroutines
            if (inputActivationCoroutine != null)
            {
                StopCoroutine(inputActivationCoroutine);
                inputActivationCoroutine = null;
                PassportLogger.Info($"{TAG} Stopped input activation coroutine during cleanup");
            }

            // Clean up WebView GameObject
            if (uwbGameObject != null)
            {
                DestroyImmediate(uwbGameObject);
            }
        }
    }
}
