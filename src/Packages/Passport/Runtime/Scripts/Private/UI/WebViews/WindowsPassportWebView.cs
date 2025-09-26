using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;
using Cysharp.Threading.Tasks;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_EDITOR && UNITY_EDITOR_WIN))
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltstroStudios.UnityWebBrowser.Shared.Popups;
using VoltstroStudios.UnityWebBrowser.Input;
using static VoltstroStudios.UnityWebBrowser.Core.Engines.Engine;
using Resolution = VoltstroStudios.UnityWebBrowser.Shared.Resolution;
#endif

namespace Immutable.Passport
{
#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_EDITOR && UNITY_EDITOR_WIN))
    /// <summary>
    /// Windows implementation of IPassportWebView using Volt Unity Web Browser (UWB)
    /// Wraps all UWB-specific functionality in a clean, platform-agnostic interface
    /// </summary>
    public class WindowsPassportWebView : IPassportWebView
    {
        private const string TAG = "[WindowsPassportWebView]";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Windows API calls for window focus
        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetFocus(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern System.IntPtr FindWindow(string lpClassName, string lpWindowName);
#endif

        // UWB Components
        private GameObject uwbGameObject;
        private WebBrowserUIFull webBrowserUI;
        private WebBrowserClient webBrowserClient;
        private RawImage targetRawImage;
        private MonoBehaviour coroutineRunner;

        // State
        private bool isInitialized = false;
        private bool isVisible = false;
        private bool isWebBrowserReady = false;
        private string currentUrl = "";
        private string queuedUrl = null;
        private PassportWebViewConfig config;
        private bool needsResolutionUpdate = false;

        // Input management
        private Coroutine inputActivationCoroutine;

        // Events
        public event Action<string> OnJavaScriptMessage;
        public event Action OnLoadFinished;
        public event Action OnLoadStarted;

        // Properties
        public bool IsVisible => isVisible;
        public string CurrentUrl => currentUrl;

        /// <summary>
        /// Constructor for Windows PassportWebView
        /// </summary>
        /// <param name="targetRawImage">RawImage component where the WebView will render</param>
        /// <param name="coroutineRunner">MonoBehaviour to run coroutines (usually PassportUI)</param>
        public WindowsPassportWebView(RawImage targetRawImage, MonoBehaviour coroutineRunner)
        {
            this.targetRawImage = targetRawImage ?? throw new ArgumentNullException(nameof(targetRawImage));
            this.coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));

            PassportLogger.Info($"{TAG} Windows WebView wrapper created");
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
                PassportLogger.Info($"{TAG} Initializing Windows WebView with UWB...");

                CreateUWBInstance();
                ConfigureUWBSettings();
                SetupEventHandlers();

                isInitialized = true;
                PassportLogger.Info($"{TAG} Windows WebView initialized successfully");
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

                // Check if WebBrowser is ready
                if (isWebBrowserReady)
                {
                    // Navigate immediately
                    webBrowserClient.LoadUrl(url);
                    PassportLogger.Info($"{TAG} Navigated immediately (WebBrowser ready): {url}");
                }
                else
                {
                    // Queue for when WebBrowser becomes ready
                    queuedUrl = url;
                    PassportLogger.Info($"{TAG} Queued URL for when WebBrowser is ready: {url}");
                }
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
                PassportLogger.Info($"{TAG} Showing WebView");

                // Update UWB internal resolution to match PassportUI dimensions
                if (webBrowserClient != null && webBrowserClient.IsConnected)
                {
                    SetUWBResolution(config.Width, config.Height);
                }

                // Show the UWB GameObject
                if (uwbGameObject != null)
                {
                    uwbGameObject.SetActive(true);
                }

                // Enable the RawImage and set size to match WebView
                if (targetRawImage != null)
                {
                    targetRawImage.enabled = true;
                    var rectTransform = targetRawImage.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        // Match the WebView dimensions that were configured
                        if (config.Width > 0 && config.Height > 0)
                        {
                            // Center the RawImage with specific dimensions to match WebView
                            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                            rectTransform.sizeDelta = new Vector2(config.Width, config.Height);
                            rectTransform.anchoredPosition = Vector2.zero;
                            PassportLogger.Info($"{TAG} RawImage sized to match WebView: {config.Width}x{config.Height}");
                        }
                        else
                        {
                            // Full-screen fallback to match WebView
                            rectTransform.anchorMin = Vector2.zero;
                            rectTransform.anchorMax = Vector2.one;
                            rectTransform.offsetMin = Vector2.zero;
                            rectTransform.offsetMax = Vector2.zero;
                            PassportLogger.Info($"{TAG} RawImage sized to full-screen to match WebView");
                        }
                    }
                }

                isVisible = true;

                // Start input preparation
                coroutineRunner.StartCoroutine(PrepareInputAfterShow());

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
                PassportLogger.Info($"{TAG} Hiding WebView");

                // Stop input coroutine if running
                if (inputActivationCoroutine != null)
                {
                    coroutineRunner.StopCoroutine(inputActivationCoroutine);
                    inputActivationCoroutine = null;
                }

                // Hide the UWB GameObject
                if (uwbGameObject != null)
                {
                    uwbGameObject.SetActive(false);
                }

                // Hide the RawImage and set size to zero
                if (targetRawImage != null)
                {
                    targetRawImage.enabled = false;
                    var rectTransform = targetRawImage.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = Vector2.zero;
                    }
                }

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
            if (!isInitialized || webBrowserClient == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - WebView not ready");
                return;
            }

            try
            {
                PassportLogger.Debug($"{TAG} Executing JavaScript: {js}");
                webBrowserClient.ExecuteJs(js);
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to execute JavaScript: {ex.Message}");
            }
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            if (!isInitialized || webBrowserClient == null)
            {
                PassportLogger.Error($"{TAG} Cannot register JS method - WebView not ready");
                return;
            }

            try
            {
                PassportLogger.Info($"{TAG} Registering JavaScript method: {methodName}");

                // Enable JS methods if not already enabled
                if (!webBrowserClient.jsMethodManager.jsMethodsEnable)
                {
                    webBrowserClient.jsMethodManager.jsMethodsEnable = true;
                    PassportLogger.Info($"{TAG} JavaScript methods enabled");
                }

                // Register the method
                webBrowserClient.RegisterJsMethod<string>(methodName, handler);
                PassportLogger.Info($"{TAG} JavaScript method '{methodName}' registered successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to register JavaScript method '{methodName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Check and apply any pending resolution updates (call from main thread)
        /// </summary>
        public void UpdatePendingResolution()
        {
            if (needsResolutionUpdate && isWebBrowserReady && webBrowserClient != null)
            {
                needsResolutionUpdate = false;
                SetUWBResolution(config.Width, config.Height);
            }
        }

        /// <summary>
        /// Update the UWB internal resolution to match new dimensions
        /// </summary>
        /// <param name="width">New width in pixels</param>
        /// <param name="height">New height in pixels</param>
        public void UpdateUWBResolution(int width, int height)
        {
            if (!isInitialized || webBrowserClient == null || !webBrowserClient.IsConnected)
            {
                return; // Silently ignore if not ready
            }

            SetUWBResolution(width, height);
        }

        /// <summary>
        /// Internal method to set UWB resolution with validation and logging
        /// </summary>
        private void SetUWBResolution(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            try
            {
                var newResolution = new Resolution((uint)width, (uint)height);
                webBrowserClient.Resolution = newResolution;
                PassportLogger.Info($"{TAG} UWB resolution set to: {width}x{height}");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to set UWB resolution: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                PassportLogger.Info($"{TAG} Disposing Windows WebView");

                // Stop any running coroutines
                if (inputActivationCoroutine != null)
                {
                    coroutineRunner.StopCoroutine(inputActivationCoroutine);
                    inputActivationCoroutine = null;
                }

                // Unsubscribe from events
                if (webBrowserClient != null)
                {
                    webBrowserClient.OnLoadFinish -= OnLoadFinishHandler;
                    webBrowserClient.OnLoadStart -= OnLoadStartHandler;
                    webBrowserClient.OnClientConnected -= OnClientConnectedHandler;
                }

                // Destroy UWB GameObject
                if (uwbGameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(uwbGameObject);
                    uwbGameObject = null;
                }

                // Clear references
                webBrowserClient = null;
                webBrowserUI = null;
                targetRawImage = null;
                coroutineRunner = null;

                isInitialized = false;
                isVisible = false;
                isWebBrowserReady = false;
                queuedUrl = null;

                PassportLogger.Info($"{TAG} Windows WebView disposed successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Error during disposal: {ex.Message}");
            }
        }

    #region Private Implementation

        private void CreateUWBInstance()
        {
            PassportLogger.Info($"{TAG} Creating UWB instance...");

            // Check if WebView already exists (prevent double creation)
            if (uwbGameObject != null)
            {
                PassportLogger.Warn($"{TAG} UWB GameObject already exists! Destroying previous instance.");
                UnityEngine.Object.DestroyImmediate(uwbGameObject);
                uwbGameObject = null;
            }

            // Find or create Canvas
            Canvas canvas = FindOrCreateCanvas();

            // Create UWB GameObject as child of Canvas with unique name
            uwbGameObject = new GameObject("PassportUI_WebView");
            uwbGameObject.transform.SetParent(canvas.transform, false);

            // IMPORTANT: Start hidden
            uwbGameObject.SetActive(false);

            // Add RectTransform for UI positioning
            RectTransform rectTransform = uwbGameObject.AddComponent<RectTransform>();

            // Use configured dimensions or fallback to full-screen
            if (config.Width > 0 && config.Height > 0)
            {
                // Center the WebView with specific dimensions
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(config.Width, config.Height);
                rectTransform.anchoredPosition = Vector2.zero;
                PassportLogger.Info($"{TAG} Using configured dimensions: {config.Width}x{config.Height}");
            }
            else
            {
                // Full-screen fallback
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                PassportLogger.Info($"{TAG} Using full-screen dimensions");
            }

            // Add WebBrowserUIFull component
            webBrowserUI = uwbGameObject.AddComponent<WebBrowserUIFull>();

            // Configure input handling
            ConfigureInputHandler();

            // Get the browser client
            webBrowserClient = webBrowserUI.browserClient;

            PassportLogger.Info($"{TAG} UWB instance created successfully");
        }

        private void ConfigureUWBSettings()
        {
            PassportLogger.Info($"{TAG} Configuring UWB settings...");

            // Set UWB resolution before starting (direct field access to avoid Resize() call)
            if (config.Width > 0 && config.Height > 0)
            {
                try
                {
                    // Use reflection to set the private resolution field directly
                    var resolutionField = typeof(VoltstroStudios.UnityWebBrowser.Core.WebBrowserClient).GetField("resolution",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (resolutionField != null)
                    {
                        var initialResolution = new Resolution((uint)config.Width, (uint)config.Height);
                        resolutionField.SetValue(webBrowserClient, initialResolution);
                        PassportLogger.Info($"{TAG} Set initial UWB resolution field to: {config.Width}x{config.Height}");
                    }
                    else
                    {
                        PassportLogger.Warn($"{TAG} Could not find UWB resolution field for direct setting");
                    }
                }
                catch (Exception ex)
                {
                    PassportLogger.Error($"{TAG} Failed to set initial UWB resolution field: {ex.Message}");
                }
            }

            // Configure for UI display (not headless)
            webBrowserClient.headless = false;
            webBrowserClient.popupAction = PopupAction.OpenExternalWindow;

            // Set initial URL
            webBrowserClient.initialUrl = config.InitialUrl;
            PassportLogger.Info($"{TAG} Set initial URL to: {config.InitialUrl}");

            // Configure isolated instance
            ConfigureIsolatedInstance();

            PassportLogger.Info($"{TAG} UWB settings configured successfully");
        }

        private void ConfigureIsolatedInstance()
        {
            PassportLogger.Info($"{TAG} Configuring isolated UWB instance...");

            // CRITICAL: Use completely different ports from SDK's bridge instance
            var tcpLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();

            // Use fixed, well-separated ports for UI WebView to avoid conflicts
            int basePort = 7777; // Well above default 5555/5556
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
            if (config.ClearCacheOnInit)
            {
                ClearWebViewCache();
            }

            var cacheDir = Path.Combine(Application.persistentDataPath, "PassportUI_UWBCache");
            webBrowserClient.CachePath = new FileInfo(cacheDir);
            PassportLogger.Info($"{TAG} Using isolated cache: {cacheDir}");

            // Configure remote debugging if enabled
            if (config.EnableRemoteDebugging)
            {
                webBrowserClient.remoteDebugging = true;
                webBrowserClient.remoteDebuggingPort = config.RemoteDebuggingPort;
                PassportLogger.Info($"{TAG} Remote debugging enabled on port {config.RemoteDebuggingPort}");
            }

            PassportLogger.Info($"{TAG} Isolated instance configured successfully");
        }

        private void ConfigureEngine()
        {
            var engineConfig = ScriptableObject.CreateInstance<EngineConfiguration>();
            engineConfig.engineAppName = "UnityWebBrowser.Engine.Cef";

            List<EnginePlatformFiles> engineFiles = new List<EnginePlatformFiles>
            {
                new EnginePlatformFiles
                {
                    platform = Platform.Windows64,
                    engineBaseAppLocation = "",
                    engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
                    ,
                    engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.win.x64@2.2.5-130.1.16/Engine~"
#endif
                }
            };

            engineConfig.engineFiles = engineFiles.ToArray();
            webBrowserClient.engine = engineConfig;
        }

        private void ConfigureInputHandler()
        {
            try
            {
#if ENABLE_INPUT_SYSTEM
                // Use UWB's built-in Input System handler
                PassportLogger.Info($"{TAG} Using UWB Input System handler");
                var inputSystemHandler = ScriptableObject.CreateInstance<VoltstroStudios.UnityWebBrowser.Input.WebBrowserInputSystemHandler>();

                // Configure Input Actions for the handler
                // Set up scroll input action
                inputSystemHandler.scrollInput = new InputAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
                inputSystemHandler.scrollInput.Enable();

                // Set up pointer position input action
                inputSystemHandler.pointPosition = new InputAction("PointerPosition", InputActionType.Value, "<Mouse>/position");
                inputSystemHandler.pointPosition.Enable();

                webBrowserUI.inputHandler = inputSystemHandler;
                PassportLogger.Info($"{TAG} Input System handler configured with mouse actions");
#else
                // Load the BrowserInput asset from SDK Resources (legacy input)
                var inputHandler = Resources.Load<VoltstroStudios.UnityWebBrowser.Input.WebBrowserOldInputHandler>("BrowserInput");
                if (inputHandler != null)
                {
                    webBrowserUI.inputHandler = inputHandler;
                    PassportLogger.Info($"{TAG} Loaded BrowserInput.asset from SDK Resources");
                }
                else
                {
                    PassportLogger.Warn($"{TAG} BrowserInput.asset not found in Resources, creating fallback");
                    var fallbackHandler = ScriptableObject.CreateInstance<VoltstroStudios.UnityWebBrowser.Input.WebBrowserOldInputHandler>();
                    webBrowserUI.inputHandler = fallbackHandler;
                }
#endif
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to configure input handler: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            if (webBrowserClient != null)
            {
                webBrowserClient.OnLoadFinish += OnLoadFinishHandler;
                webBrowserClient.OnLoadStart += OnLoadStartHandler;
                webBrowserClient.OnClientConnected += OnClientConnectedHandler;
                PassportLogger.Info($"{TAG} Event handlers configured");
            }
        }

        private void OnLoadFinishHandler(string url)
        {
            PassportLogger.Info($"{TAG} Load finished: {url}");
            currentUrl = url;
            OnLoadFinished?.Invoke();
        }

        private void OnLoadStartHandler(string url)
        {
            PassportLogger.Info($"{TAG} Load started: {url}");
            currentUrl = url;
            OnLoadStarted?.Invoke();
        }

        private void OnClientConnectedHandler()
        {
            PassportLogger.Info($"{TAG} WebBrowser client connected and ready!");
            isWebBrowserReady = true;

            // Set flag to update resolution on main thread
            if (config.Width > 0 && config.Height > 0)
            {
                needsResolutionUpdate = true;
            }

            // Process any queued URL
            if (!string.IsNullOrEmpty(queuedUrl))
            {
                PassportLogger.Info($"{TAG} Processing queued URL: {queuedUrl}");
                string urlToLoad = queuedUrl;
                queuedUrl = null;

                // Load the queued URL now that we're ready
                webBrowserClient.LoadUrl(urlToLoad);
                PassportLogger.Info($"{TAG} Navigated to queued URL: {urlToLoad}");
            }
        }

        private Canvas FindOrCreateCanvas()
        {
            // Try to find existing Canvas
            Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                PassportLogger.Info($"{TAG} Using existing Canvas: {canvas.name}");
                return canvas;
            }

            // Create new Canvas if none exists
            GameObject canvasGO = new GameObject("PassportUI_Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order to appear on top

            // Add CanvasScaler for responsive UI
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster for UI interaction
            canvasGO.AddComponent<GraphicRaycaster>();

            PassportLogger.Info($"{TAG} Created new Canvas: {canvas.name}");
            return canvas;
        }

        private IEnumerator PrepareInputAfterShow()
        {
            PassportLogger.Info($"{TAG} Preparing input after show...");

            // Wait a frame for UI to settle
            yield return null;

            // Focus handling for editor
#if UNITY_EDITOR
            if (uwbGameObject != null)
            {
                UnityEditor.Selection.activeGameObject = uwbGameObject;
                PassportLogger.Info($"{TAG} WebView GameObject selected for input focus (Editor)");
            }
#endif

            // Focus handling for builds
            TryForceWindowFocus();

            // Additional input activation attempts
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.5f);
                TryActivateInput();
            }

            PassportLogger.Info($"{TAG} Input preparation completed");
        }

        private void TryForceWindowFocus()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var currentWindow = GetForegroundWindow();
                if (currentWindow != System.IntPtr.Zero)
                {
                    SetForegroundWindow(currentWindow);
                    SetFocus(currentWindow);
                    BringWindowToTop(currentWindow);
                    PassportLogger.Info($"{TAG} Window focus applied (Windows)");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"{TAG} Windows focus call failed: {ex.Message}");
            }
#endif
        }

        private void TryActivateInput()
        {
            try
            {
                // Focus the WebView GameObject
                if (uwbGameObject != null && uwbGameObject.activeInHierarchy)
                {
#if UNITY_EDITOR
                    UnityEditor.Selection.activeGameObject = uwbGameObject;
#endif
                    PassportLogger.Debug($"{TAG} Input activation attempted");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"{TAG} Input activation failed: {ex.Message}");
            }
        }

        private void ClearWebViewCache()
        {
            try
            {
                var cacheDir = Path.Combine(Application.persistentDataPath, "PassportUI_UWBCache");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                    PassportLogger.Info($"{TAG} WebView cache cleared: {cacheDir}");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"{TAG} Failed to clear cache: {ex.Message}");
            }
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    #endregion
    }
#endif
}
