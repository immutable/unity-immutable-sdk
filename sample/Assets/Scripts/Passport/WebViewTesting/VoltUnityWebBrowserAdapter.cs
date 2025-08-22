using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltstroStudios.UnityWebBrowser.Shared.Popups;
#endif

namespace Immutable.Passport.WebViewTesting
{
    /// <summary>
    /// Adapter for Volt Unity Web Browser (UWB) package
    /// https://projects.voltstro.dev/UnityWebBrowser/latest/
    /// Uses the existing UWB integration from the Immutable SDK
    /// </summary>
    public class VoltUnityWebBrowserAdapter : IWebViewAdapter
    {
        public event Action<string> OnNavigationCompleted;
        public event Action<string> OnMessageReceived;
        public event Action<string> OnError;
        
        public bool IsActive { get; private set; }
        
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        private GameObject uwbGameObject;
        private WebBrowserUIFull webBrowserUI;
        private WebBrowserClient webBrowserClient;
#endif
        private bool isInitialized = false;
        
        // Performance timing
        private System.Diagnostics.Stopwatch initializationTimer;
        private float initStartTime;
        private float engineReadyTime;
        private float navigationStartTime;
        private System.Diagnostics.Stopwatch navigationTimer;
        
        // Navigation queue
        private string queuedNavigationUrl;
        private bool hasQueuedNavigation;
        
        public void Initialize(int width, int height)
        {
            try
            {
                // Start timing the initialization
                initializationTimer = System.Diagnostics.Stopwatch.StartNew();
                initStartTime = Time.realtimeSinceStartup;
                
                Debug.Log($"[VoltUWBAdapter] 🚀 Creating SEPARATE UWB instance for UI testing (isolated from SDK bridge)");
                Debug.Log($"[VoltUWBAdapter] Initializing Volt Unity Web Browser {width}x{height} - Timer started");
                
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                // Find or create Canvas for WebView display
                Canvas canvas = FindOrCreateCanvas();
                
                // Create UWB GameObject as child of Canvas with unique name
                uwbGameObject = new GameObject("VoltUWB_TestInstance_UI");
                uwbGameObject.transform.SetParent(canvas.transform, false);
                
                // Add RectTransform for UI positioning
                RectTransform rectTransform = uwbGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                Debug.Log("[VoltUWBAdapter] 🎮 Adding WebBrowserUIFull component (separate from SDK's WebBrowserNoUi)");
                
                // Add WebBrowserUIFull component - THIS IS DIFFERENT FROM SDK's WebBrowserNoUi
                webBrowserUI = uwbGameObject.AddComponent<WebBrowserUIFull>();
                
                // Configure input handling
                ConfigureInputHandler();
                
                Debug.Log($"[VoltUWBAdapter] WebBrowser UI added to Canvas: {canvas.name}");
                Debug.Log($"[VoltUWBAdapter] 🔍 HIERARCHY INFO: WebView GameObject created as '{uwbGameObject.name}' under Canvas '{canvas.name}'");
                Debug.Log($"[VoltUWBAdapter] 🔍 INSPECTOR TIP: Look for '{uwbGameObject.name}' in the hierarchy to see WebView config");
                
                // Get the browser client and configure for UI display
                webBrowserClient = webBrowserUI.browserClient;

                webBrowserClient.initialUrl = "https://passport.immutable.com/sdk-sample-app";
                
                Debug.Log("[VoltUWBAdapter] 🔧 Configuring UI instance (headless=false, separate from SDK bridge)");
                webBrowserClient.headless = false; // CRITICAL: SDK uses headless=true, we need UI
                
                // Configure popup handling
                webBrowserClient.popupAction = PopupAction.OpenExternalWindow;
                Debug.Log("[VoltUWBAdapter] 🪟 Popup Action set to: OpenExternalWindow (allows popups to open in external browser)");
                Debug.Log("[VoltUWBAdapter] 💡 Note: CSP 'frame-ancestors' errors are expected and indicate proper security behavior");
                
                // Ensure complete isolation from SDK's UWB instance
                ConfigureIsolatedUWBInstance();
                
                // Subscribe to events
                webBrowserClient.OnLoadFinish += OnLoadFinishHandler;
                webBrowserClient.OnLoadStart += OnLoadStartHandler;
                
                IsActive = true;
                // Note: Don't set isInitialized = true yet, wait for the browser to be ready
                
                float componentSetupTime = Time.realtimeSinceStartup - initStartTime;
                Debug.Log($"[VoltUWBAdapter] Component setup completed in {componentSetupTime:F3}s. Waiting for UWB engine to be ready...");
                
                Debug.Log("[VoltUWBAdapter] ✅ Separate UWB UI instance created successfully (isolated from SDK bridge)");
#else
                OnError?.Invoke("Volt Unity Web Browser only supported on Windows platforms");
                Debug.LogWarning("[VoltUWBAdapter] UWB not supported on this platform");
#endif
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"UWB initialization failed: {ex.Message}");
                Debug.LogError($"[VoltUWBAdapter] Initialization error: {ex}");
                throw;
            }
        }
        
        public void Navigate(string url)
        {
            if (!IsActive)
            {
                OnError?.Invoke("Volt Unity Web Browser not active");
                return;
            }
            
            try
            {
                navigationStartTime = Time.realtimeSinceStartup;
                navigationTimer = System.Diagnostics.Stopwatch.StartNew();
                
                Debug.Log($"[VoltUWBAdapter] 🚀 Navigating to: {url}");
                
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                // Check if engine is ready before navigating
                if (webBrowserClient != null && webBrowserClient.ReadySignalReceived)
                {
                    webBrowserClient.LoadUrl(url);
                    Debug.Log($"[VoltUWBAdapter] ✅ Navigation started immediately (engine ready)");
                }
                else
                {
                    Debug.Log($"[VoltUWBAdapter] ⏳ Engine not ready, queueing navigation...");
                    // Queue the navigation for when engine becomes ready
                    QueueNavigation(url);
                }
#else
                OnError?.Invoke("Navigation not supported on this platform");
#endif
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Navigation failed: {ex.Message}");
                Debug.LogError($"[VoltUWBAdapter] Navigation error: {ex}");
            }
        }
        
        public void ExecuteJavaScript(string script)
        {
            if (!isInitialized)
            {
                OnError?.Invoke("Volt Unity Web Browser not initialized");
                return;
            }
            
            try
            {
                Debug.Log($"[VoltUWBAdapter] Executing JavaScript: {script.Substring(0, Math.Min(100, script.Length))}...");
                
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                webBrowserClient?.ExecuteJs(script);
#else
                OnError?.Invoke("JavaScript execution not supported on this platform");
#endif
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"JavaScript execution failed: {ex.Message}");
                Debug.LogError($"[VoltUWBAdapter] JavaScript execution error: {ex}");
            }
        }
        
        public void TestInputFunctionality()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[VoltUWBAdapter] ⚠️ Cannot test input - WebView not initialized");
                return;
            }
            
            try
            {
                // Test if we can detect input elements and add event listeners
                string testScript = @"
                    console.log('=== UWB INPUT TEST ===');
                    
                    // Find all input elements
                    const inputs = document.querySelectorAll('input, textarea, button, select');
                    console.log('Found ' + inputs.length + ' input elements');
                    
                    // Add click listeners to test input detection
                    inputs.forEach((input, index) => {
                        input.addEventListener('click', function() {
                            console.log('Input clicked: ' + index + ' - ' + input.tagName + ' - ' + input.type);
                        });
                        input.addEventListener('focus', function() {
                            console.log('Input focused: ' + index + ' - ' + input.tagName);
                        });
                    });
                    
                    // Test document click
                    document.addEventListener('click', function(e) {
                        console.log('Document clicked at: ' + e.clientX + ', ' + e.clientY);
                        console.log('Target: ' + e.target.tagName);
                    });
                    
                    console.log('Input test listeners added');
                ";
                
                ExecuteJavaScript(testScript);
                Debug.Log("[VoltUWBAdapter] 🧪 Input functionality test injected - check browser console for click events");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] ❌ Input test failed: {ex.Message}");
            }
        }
        
        public void SetPopupAction(PopupAction action)
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            if (webBrowserClient != null)
            {
                webBrowserClient.popupAction = action;
                Debug.Log($"[VoltUWBAdapter] 🪟 Popup Action changed to: {action}");
            }
            else
            {
                Debug.LogWarning("[VoltUWBAdapter] ⚠️ Cannot set popup action - WebBrowserClient not initialized");
            }
#endif
        }
        
        public PopupAction GetPopupAction()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            return webBrowserClient?.popupAction ?? PopupAction.Ignore;
#else
            return PopupAction.Ignore;
#endif
        }
        
        public void SendMessage(string message)
        {
            if (!isInitialized)
            {
                OnError?.Invoke("Volt Unity Web Browser not initialized");
                return;
            }
            
            try
            {
                Debug.Log($"[VoltUWBAdapter] Sending message: {message}");
                
                // TODO: Implement actual message sending
                // webBrowserClient.SendMessage(message);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send message failed: {ex.Message}");
            }
        }
        
        public void Show()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            if (uwbGameObject != null)
            {
                uwbGameObject.SetActive(true);
                
                // Ensure WebView is on top
                Canvas canvas = uwbGameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 1000; // Very high priority
                    Debug.Log($"[VoltUWBAdapter] 📱 Canvas sorting order set to {canvas.sortingOrder}");
                }
                
                Debug.Log("[VoltUWBAdapter] 👁️ Volt Unity Web Browser shown");
            }
#endif
        }
        
        public void Hide()
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            if (uwbGameObject != null)
            {
                uwbGameObject.SetActive(false);
                Debug.Log("[VoltUWBAdapter] 🙈 Volt Unity Web Browser hidden");
            }
#endif
        }
        
        public WebViewPerformanceMetrics GetPerformanceMetrics()
        {
            float initTime = isInitialized ? (engineReadyTime - initStartTime) : -1f;
            long preciseInitTime = initializationTimer?.ElapsedMilliseconds ?? -1;
            
            return new WebViewPerformanceMetrics
            {
                memoryUsageMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f),
                cpuUsagePercent = 0f, // TODO: Get from UWB
                renderTimeMsAvg = preciseInitTime > 0 ? preciseInitTime : 16.67f,
                textureWidth = 1024, // TODO: Get from UWB
                textureHeight = 768, // TODO: Get from UWB
                engineVersion = $"Volt Unity Web Browser (Init: {(initTime > 0 ? $"{initTime:F2}s" : "Pending")})"
            };
        }
        
        public void Dispose()
        {
            try
            {
                if (isInitialized)
                {
                    // TODO: Implement proper disposal
                    // webBrowserClient?.Dispose();
                    
                    IsActive = false;
                    isInitialized = false;
                    Debug.Log("[VoltUWBAdapter] Volt Unity Web Browser disposed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] Disposal error: {ex.Message}");
            }
        }
        
        // Event handlers for UWB
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        private void OnLoadFinishHandler(string url)
        {
            Debug.Log($"[VoltUWBAdapter] 🏁 Load finished: {url}");
            
            // Mark as fully initialized on first load and record timing
            if (!isInitialized)
            {
                isInitialized = true;
                engineReadyTime = Time.realtimeSinceStartup;
                initializationTimer?.Stop();
                
                float totalInitTime = engineReadyTime - initStartTime;
                long preciseInitTime = initializationTimer?.ElapsedMilliseconds ?? 0;
                
                Debug.Log($"[VoltUWBAdapter] 🎉 UWB engine fully initialized!");
                Debug.Log($"[VoltUWBAdapter] ⏱️  Total initialization time: {totalInitTime:F3}s ({preciseInitTime}ms)");
                Debug.Log($"[VoltUWBAdapter] 🚀 Ready for navigation!");
                
                // Process any queued navigation
                if (hasQueuedNavigation && !string.IsNullOrEmpty(queuedNavigationUrl))
                {
                    Debug.Log($"[VoltUWBAdapter] 🔄 Processing queued navigation to: {queuedNavigationUrl}");
                    string urlToLoad = queuedNavigationUrl;
                    queuedNavigationUrl = null;
                    hasQueuedNavigation = false;
                    
                    // Navigate to the queued URL
                    webBrowserClient.LoadUrl(urlToLoad);
                    return; // Don't fire OnNavigationCompleted yet, wait for the actual page
                }
            }
            
            // Check if this looks like a real webpage load
            if (url.StartsWith("http"))
            {
                Debug.Log($"[VoltUWBAdapter] 🌐 HTTP page loaded successfully: {url}");
                
                // Inject JavaScript to check page content
                ExecuteJavaScript(@"
                    try {
                        const bodyContent = document.body ? document.body.innerHTML : 'No body';
                        const title = document.title || 'No title';
                        const readyState = document.readyState;
                        console.log('Page Debug Info:', {
                            url: window.location.href,
                            title: title,
                            readyState: readyState,
                            bodyLength: bodyContent.length,
                            hasContent: bodyContent.length > 100
                        });
                        
                        if (bodyContent.length < 100) {
                            console.warn('Page appears to have minimal content!');
                        }
                    } catch(e) {
                        console.error('Page debug check failed:', e);
                    }
                ");
            }
            else
            {
                Debug.Log($"[VoltUWBAdapter] 📄 Non-HTTP page loaded: {url}");
            }
            
            // Calculate navigation timing
            float navigationTime = navigationTimer != null ? (float)navigationTimer.ElapsedMilliseconds / 1000f : 0f;
            navigationTimer?.Stop();
            
            Debug.Log($"[VoltUWBAdapter] ⏱️  Page load completed in {navigationTime:F3}s");
            OnNavigationCompleted?.Invoke(url);
            
            // Auto-execute test script to check page elements
            ExecuteJavaScript(@"
                try {
                    const result = {
                        hasLoginButton: !!document.querySelector('button[type=""submit""], .login-button, [class*=""login""]'),
                        hasEmailInput: !!document.querySelector('input[type=""email""], input[name=""email""]'),
                        pageTitle: document.title,
                        timestamp: Date.now(),
                        engine: 'Volt Unity Web Browser (UWB)',
                        platform: 'Windows (CEF)',
                        license: 'MIT License',
                        url: window.location.href
                    };
                    console.log('UWB Test Result:', JSON.stringify(result));
                } catch(e) {
                    console.error('UWB Test Error:', e);
                }
            ");
        }
        
        private void OnLoadStartHandler(string url)
        {
            float loadStartTime = navigationTimer != null ? (float)navigationTimer.ElapsedMilliseconds / 1000f : 0f;
            Debug.Log($"[VoltUWBAdapter] 🚀 Load started: {url} (after {loadStartTime:F3}s)");
            
            // Check if WebView is visible
            if (uwbGameObject != null)
            {
                bool isActive = uwbGameObject.activeInHierarchy;
                Canvas canvas = uwbGameObject.GetComponentInParent<Canvas>();
                Debug.Log($"[VoltUWBAdapter] 📱 WebView Status - Active: {isActive}, Canvas: {(canvas != null ? canvas.name : "null")}");
                
                if (canvas != null)
                {
                    Debug.Log($"[VoltUWBAdapter] 📱 Canvas - RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}");
                }
            }
        }
        
        private Canvas FindOrCreateCanvas()
        {
            // Try to find WebView container from the test manager
            var testManager = UnityEngine.Object.FindObjectOfType<WebViewTestManager>();
            if (testManager != null && testManager.webViewContainer != null)
            {
                Debug.Log($"[VoltUWBAdapter] Using WebView container: {testManager.webViewContainer.name}");
                
                // Check if container already has a Canvas
                Canvas containerCanvas = testManager.webViewContainer.GetComponent<Canvas>();
                if (containerCanvas == null)
                {
                    // Add Canvas to the container
                    containerCanvas = testManager.webViewContainer.AddComponent<Canvas>();
                    containerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    containerCanvas.sortingOrder = 100;
                    
                    // Add required components
                    if (testManager.webViewContainer.GetComponent<UnityEngine.UI.CanvasScaler>() == null)
                        testManager.webViewContainer.AddComponent<UnityEngine.UI.CanvasScaler>();
                    if (testManager.webViewContainer.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                        testManager.webViewContainer.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    
                    Debug.Log($"[VoltUWBAdapter] Added Canvas to WebView container");
                }
                
                return containerCanvas;
            }
            
            // Fallback: Try to find existing Canvas
            Canvas existingCanvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log($"[VoltUWBAdapter] Using existing Canvas: {existingCanvas.name}");
                return existingCanvas;
            }
            
            // Create new Canvas if none exists
            GameObject canvasGO = new GameObject("VoltUWB_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure WebView appears on top
            
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create EventSystem if it doesn't exist
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("VoltUWB_EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[VoltUWBAdapter] Created EventSystem for input handling");
            }
            
            Debug.Log("[VoltUWBAdapter] Created new Canvas for WebView display");
            return canvas;
        }
        
        private void QueueNavigation(string url)
        {
            queuedNavigationUrl = url;
            hasQueuedNavigation = true;
            Debug.Log($"[VoltUWBAdapter] 📝 Navigation queued: {url}");
        }
        
        private void ConfigureIsolatedUWBInstance()
        {
            try
            {
                Debug.Log("[VoltUWBAdapter] 🔧 Configuring COMPLETELY ISOLATED UWB instance (separate from SDK bridge)...");
                
                // CRITICAL: Use completely different ports from SDK's bridge instance
                var tcpLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();
                var rnd = new System.Random();
                int attempts = 0;
                do
                {
                    // Use high port range to avoid SDK's bridge ports (which use 1024-65353)
                    tcpLayer.inPort = rnd.Next(45000, 49999); 
                    tcpLayer.outPort = tcpLayer.inPort + 1;
                    attempts++;
                    if (attempts > 100) break; // Prevent infinite loop
                } while (!IsPortAvailable(tcpLayer.inPort) || !IsPortAvailable(tcpLayer.outPort));
                
                webBrowserClient.communicationLayer = tcpLayer;
                Debug.Log($"[VoltUWBAdapter] 🔌 Using isolated ports: {tcpLayer.inPort}/{tcpLayer.outPort}");
                
                // CRITICAL: Set up engine configuration (this was missing!)
                ConfigureUWBEngine();
                
                // Configure for UI display (not API communication like SDK)
                webBrowserClient.engineStartupTimeout = 15000; // 15 seconds (longer than SDK's 10s)
                webBrowserClient.noSandbox = true; // Allow broader web content
                
                // Different cache path to avoid conflicts
                var cacheDir = Path.Combine(Application.persistentDataPath, "UWB_TestCache");
                webBrowserClient.CachePath = new System.IO.FileInfo(cacheDir);
                Debug.Log($"[VoltUWBAdapter] 💾 Using isolated cache: {cacheDir}");
                
                // Set logging
                webBrowserClient.logSeverity = LogSeverity.Debug;
                
                Debug.Log("[VoltUWBAdapter] ✅ Isolated UWB instance configured successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] ❌ Failed to configure isolated UWB instance: {ex}");
            }
        }
        
        private bool IsPortAvailable(int port)
        {
            try
            {
                var tcpConnInfoArray = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                return tcpConnInfoArray.All(endpoint => endpoint.Port != port);
            }
            catch
            {
                return false;
            }
        }
        
        private void ConfigureUWBEngine()
        {
            try
            {
                Debug.Log("[VoltUWBAdapter] 🔧 Configuring UWB CEF Engine for UI instance...");
                
                // Create engine configuration (same as SDK but for UI instance)
                var engineConfig = ScriptableObject.CreateInstance<EngineConfiguration>();
                engineConfig.engineAppName = "UnityWebBrowser.Engine.Cef";
                engineConfig.engineFiles = new Engine.EnginePlatformFiles[]
                {
                    new Engine.EnginePlatformFiles()
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
                
                // Assign engine to our UI instance
                webBrowserClient.engine = engineConfig;
                
                Debug.Log("[VoltUWBAdapter] ✅ CEF Engine configured successfully for UI instance");
                Debug.Log("[VoltUWBAdapter] 🎯 Engine path: Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.win.x64@2.2.5-130.1.16/Engine~");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] ❌ Failed to configure UWB engine: {ex}");
            }
        }
        
        private void SetupInputHandler()
        {
            try
            {
                if (webBrowserUI.inputHandler == null)
                {
                    Debug.Log("[VoltUWBAdapter] 🎮 Creating WebBrowser Input Handler...");
                    
#if ENABLE_INPUT_SYSTEM
                    // Use new Input System if available
                    Debug.Log("[VoltUWBAdapter] 🆕 Using New Input System (Input System Package)");
                    var inputHandler = ScriptableObject.CreateInstance<VoltstroStudios.UnityWebBrowser.Input.WebBrowserInputSystemHandler>();
                    Debug.Log("[VoltUWBAdapter] ✅ Input Handler created: WebBrowserInputSystemHandler");
#else
                    // Fall back to legacy Input Manager
                    Debug.Log("[VoltUWBAdapter] 🔄 Using Legacy Input Manager (Old Input System)");
                    var inputHandler = ScriptableObject.CreateInstance<VoltstroStudios.UnityWebBrowser.Input.WebBrowserOldInputHandler>();
                    Debug.Log("[VoltUWBAdapter] ✅ Input Handler created: WebBrowserOldInputHandler");
#endif
                    
                    // Assign it to the WebBrowserUIFull
                    webBrowserUI.inputHandler = inputHandler;
                    
                    Debug.Log($"[VoltUWBAdapter] ✅ Input Handler assigned to WebBrowserUIFull");
                }
                else
                {
                    Debug.Log($"[VoltUWBAdapter] ✅ Input Handler already exists: {webBrowserUI.inputHandler.GetType().Name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] ❌ Failed to setup input handler: {ex.Message}");
                Debug.LogError($"[VoltUWBAdapter] 💡 You may need to manually assign an Input Handler in the Inspector");
            }
        }
        
        private void ConfigureInputHandler()
        {
            if (webBrowserUI == null) return;
            
            try
            {
                Debug.Log("[VoltUWBAdapter] 🎮 Configuring WebBrowserUIFull input handling...");
                
                // Ensure EventSystem exists
                var eventSystem = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogWarning("[VoltUWBAdapter] ⚠️ No EventSystem found! Input may not work properly.");
                }
                else
                {
                    Debug.Log($"[VoltUWBAdapter] ✅ EventSystem found: {eventSystem.name}");
                }
                
                // WebBrowserUIFull inherits from RawImageUwbClientInputHandler which handles input automatically
                // It needs a RawImage component to work properly
                var rawImage = uwbGameObject.GetComponent<UnityEngine.UI.RawImage>();
                if (rawImage == null)
                {
                    Debug.Log("[VoltUWBAdapter] 🖼️ Adding RawImage component for WebBrowserUIFull");
                    rawImage = uwbGameObject.AddComponent<UnityEngine.UI.RawImage>();
                    
                    // Set up the RawImage to be transparent initially
                    rawImage.color = new Color(1, 1, 1, 1);
                    rawImage.raycastTarget = true; // Important for input handling!
                }
                
                // Ensure GraphicRaycaster exists on the Canvas for UI input
                Canvas canvas = uwbGameObject.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (raycaster == null)
                    {
                        Debug.Log("[VoltUWBAdapter] 🎯 Adding GraphicRaycaster to Canvas");
                        raycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    }
                }
                
                // Create and assign the input handler
                SetupInputHandler();
                
                Debug.Log("[VoltUWBAdapter] ✅ WebBrowserUIFull input configuration complete");
                Debug.Log("[VoltUWBAdapter] 💡 WebBrowserUIFull should now handle mouse clicks and keyboard input automatically");
                
                // Debug input setup
                Debug.Log($"[VoltUWBAdapter] 🔍 Input Debug - RawImage: {(webBrowserUI.GetComponent<RawImage>() != null)}");
                Debug.Log($"[VoltUWBAdapter] 🔍 Input Debug - InputHandler: {(webBrowserUI.inputHandler != null)}");
                Debug.Log($"[VoltUWBAdapter] 🔍 Input Debug - EventSystem: {(UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)}");
                Debug.Log($"[VoltUWBAdapter] 🔍 Input Debug - GraphicRaycaster: {(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null)}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoltUWBAdapter] ❌ Failed to configure input handler: {ex.Message}");
            }
        }
#endif
    }
}
