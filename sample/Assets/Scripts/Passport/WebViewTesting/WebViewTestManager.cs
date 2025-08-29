using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
using VoltstroStudios.UnityWebBrowser;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Immutable.Passport.WebViewTesting
{
    /// <summary>
    /// Manages WebView testing for different packages
    /// </summary>
    public class WebViewTestManager : MonoBehaviour
    {
        [Header("Test Configuration")]
        public WebViewPackage selectedPackage = WebViewPackage.VoltUnityWebBrowser;
        public string testUrl = "https://passport.immutable.com/sdk-sample-app";
        public string messageTestUrl = "http://localhost:8080"; // HTTP server URL

        [Header("UI References")]
        public Button testLoginButton;
        public Button testMessagingButton;
        public Button closeWebViewButton;
        public Text statusOutput;
        public Text performanceOutput;

        [Header("Navigation Controls")]
        public InputField urlInputField;
        public Button navigateButton;
        public Button backButton;
        public Button forwardButton;
        public Button refreshButton;
        public Button testInputButton;
        public Button findWebViewButton;
        public Button testPopupButton;

        [Header("WebView Settings")]
        public int webViewWidth = 1024;
        public int webViewHeight = 768;
        public bool fullScreenMode = true;

        [Header("UI Layout")]
        public GameObject webViewContainer;  // Green area where WebView will be displayed

        private IWebViewAdapter currentWebView;
        private float startTime;
        private int frameCount;
        private bool isCreatingWebView = false;

        public enum WebViewPackage
        {
            VoltUnityWebBrowser
        }

        void Start()
        {
            SetupUI();

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            ShowOutput("WebView Test Manager initialized. Volt Unity Web Browser ready for testing.");
#else
            ShowOutput("⚠️ Platform not supported. Volt Unity Web Browser only supports Windows and Mac.");
            Debug.LogWarning("[WebViewTestManager] Current platform not supported. UWB requires Windows or Mac.");
#endif
        }

        void Update()
        {
            // Performance monitoring
            if (currentWebView != null && currentWebView.IsActive)
            {
                frameCount++;
                float elapsed = Time.time - startTime;
                if (elapsed >= 1f)
                {
                    float fps = frameCount / elapsed;
                    ShowPerformance($"FPS: {fps:F1} | Memory: {GetMemoryUsage():F1}MB");
                    frameCount = 0;
                    startTime = Time.time;
                }
            }

            // Debug: Show WebView container dimensions (Press D key)
            if (webViewContainer != null && Input.GetKeyDown(KeyCode.D))
            {
                RectTransform rect = webViewContainer.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 size = rect.rect.size;
                    Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                    float widthPercent = (size.x / screenSize.x) * 100f;
                    float heightPercent = (size.y / screenSize.y) * 100f;

                    ShowOutput($"WebView Area: {size.x:F0}x{size.y:F0} px ({widthPercent:F1}% x {heightPercent:F1}%)");
                    Debug.Log($"[WebViewTestManager] WebView dimensions: {size.x}x{size.y} pixels, {widthPercent:F1}% x {heightPercent:F1}% of screen");
                }
            }
        }

        private void SetupUI()
        {
            if (testLoginButton != null)
                testLoginButton.onClick.AddListener(TestLoginPage);

            if (testMessagingButton != null)
                testMessagingButton.onClick.AddListener(TestMessagePassing);

            if (closeWebViewButton != null)
                closeWebViewButton.onClick.AddListener(CloseWebView);

            // Navigation controls
            if (navigateButton != null)
                navigateButton.onClick.AddListener(NavigateToUrl);

            if (backButton != null)
                backButton.onClick.AddListener(GoBack);

            if (forwardButton != null)
                forwardButton.onClick.AddListener(GoForward);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshPage);

            if (testInputButton != null)
                testInputButton.onClick.AddListener(TestInput);

            if (findWebViewButton != null)
                findWebViewButton.onClick.AddListener(FindWebViewInHierarchy);

            if (testPopupButton != null)
                testPopupButton.onClick.AddListener(TestPopupFunctionality);

            // Set default URL in input field
            if (urlInputField != null)
                urlInputField.text = testUrl;
        }

        public void TestLoginPage()
        {
            if (isCreatingWebView)
            {
                ShowOutput("WebView creation already in progress, please wait...");
                return;
            }

            ShowOutput($"Testing {selectedPackage} with login page...");

            try
            {
                isCreatingWebView = true;

                // Clean up any existing WebView first
                CloseWebView();

                currentWebView = CreateWebViewAdapter(selectedPackage);
                currentWebView.Initialize(webViewWidth, webViewHeight);
                currentWebView.Navigate(testUrl);
                currentWebView.OnNavigationCompleted += OnLoginPageLoaded;
                currentWebView.OnMessageReceived += OnWebViewMessage;

                startTime = Time.time;
                frameCount = 0;

                ShowOutput($"WebView created successfully. Loading {testUrl}");
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to create WebView: {ex.Message}");
            }
            finally
            {
                isCreatingWebView = false;
            }
        }

        public void TestMessagePassing()
        {
            if (isCreatingWebView)
            {
                ShowOutput("WebView creation already in progress, please wait...");
                return;
            }

            ShowOutput($"Testing {selectedPackage} message passing...");

            try
            {
                // isCreatingWebView = true;

                // // Clean up any existing WebView first
                // CloseWebView();

                // currentWebView = CreateWebViewAdapter(selectedPackage);
                // currentWebView.Initialize(webViewWidth, webViewHeight);
                // currentWebView.Navigate(messageTestUrl);
                // currentWebView.OnMessageReceived += OnTestMessage;

                // ShowOutput($"Loading message test page: {messageTestUrl}");

                // Force refresh to get latest content from HTTP server
                // ForceRefreshFromHttpServer();

                // // Wait a moment for refresh, then call testMessage
                // StartCoroutine(CallTestMessageAfterDelay(1.0f));

                currentWebView.ExecuteJavaScript("testMessage();");
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to create message test: {ex.Message}");
            }
            finally
            {
                isCreatingWebView = false;
            }
        }

        public void CloseWebView()
        {
            if (currentWebView != null)
            {
                try
                {
                    string webViewType = currentWebView.GetType().Name;
                    ShowOutput($"Closing existing {webViewType}...");

                    currentWebView.Dispose();
                    currentWebView = null;

                    ShowOutput("WebView closed successfully.");
                }
                catch (Exception ex)
                {
                    ShowOutput($"Error closing WebView: {ex.Message}");
                    currentWebView = null; // Force cleanup even if dispose failed
                }
            }
            else
            {
                ShowOutput("No active WebView to close.");
            }

            // Reset creation flag in case it got stuck
            isCreatingWebView = false;
        }

        public void NavigateToUrl()
        {
            if (currentWebView != null && urlInputField != null && !string.IsNullOrEmpty(urlInputField.text))
            {
                string url = urlInputField.text;
                ShowOutput($"Navigating to: {url}");
                currentWebView.Navigate(url);
            }
            else if (currentWebView == null)
            {
                ShowOutput("No active WebView. Please test a WebView first.");
            }
            else
            {
                ShowOutput("Please enter a URL to navigate to.");
            }
        }

        public void GoBack()
        {
            if (currentWebView != null)
            {
                // For now, we'll use JavaScript to go back
                currentWebView.ExecuteJavaScript("window.history.back();");
                ShowOutput("Going back...");
            }
            else
            {
                ShowOutput("No active WebView.");
            }
        }

        public void GoForward()
        {
            if (currentWebView != null)
            {
                // For now, we'll use JavaScript to go forward
                currentWebView.ExecuteJavaScript("window.history.forward();");
                ShowOutput("Going forward...");
            }
            else
            {
                ShowOutput("No active WebView.");
            }
        }

        public void RefreshPage()
        {
            if (currentWebView != null)
            {
                // For now, we'll use JavaScript to refresh
                currentWebView.ExecuteJavaScript("window.location.reload();");
                ShowOutput("Refreshing page...");
            }
            else
            {
                ShowOutput("No active WebView.");
            }
        }

        public void TestInput()
        {
            if (currentWebView != null)
            {
                // Test input functionality for Volt Unity Web Browser
                if (currentWebView is VoltUnityWebBrowserAdapter voltAdapter)
                {
                    voltAdapter.TestInputFunctionality();
                    ShowOutput("Input test injected - try clicking on form fields and check console logs");
                }
                else
                {
                    ShowOutput("Input testing not implemented for this WebView adapter yet");
                }
            }
            else
            {
                ShowOutput("No active WebView.");
            }
        }

        public void FindWebViewInHierarchy()
        {
            ShowOutput("Searching for WebView components in scene...");

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            // Find WebView GameObjects in the scene
            var webViewObjects = FindObjectsOfType<WebBrowserUIFull>();
            if (webViewObjects.Length > 0)
            {
                foreach (var webView in webViewObjects)
                {
                    ShowOutput($"Found WebView: '{webView.gameObject.name}' under '{webView.transform.parent?.name ?? "Root"}'");
                    Debug.Log($"[WebViewTestManager] 🔍 WebView GameObject: {webView.gameObject.name} (Active: {webView.gameObject.activeInHierarchy})");

                    // Log component details
                    var components = webView.GetComponents<Component>();
                    Debug.Log($"[WebViewTestManager] 📋 Components on {webView.gameObject.name}: {string.Join(", ", components.Select(c => c.GetType().Name))}");

#if UNITY_EDITOR
                    // Highlight in hierarchy (Editor only)
                    Selection.activeGameObject = webView.gameObject;
#endif
                }
            }
            else
            {
                ShowOutput("No WebView components found in scene hierarchy");
            }
#else
            ShowOutput("WebView search not available on this platform");
#endif
        }

        public void TestPopupFunctionality()
        {
            if (currentWebView != null)
            {
                // Test popup functionality for Volt Unity Web Browser
                if (currentWebView is VoltUnityWebBrowserAdapter voltAdapter)
                {
                    var currentAction = voltAdapter.GetPopupAction();
                    ShowOutput($"Current popup action: {currentAction}");

                    // Test popup with JavaScript
                    string popupTestScript = @"
                        console.log('=== POPUP TEST ===');

                        // Test window.open (popup)
                        function testPopup() {
                            console.log('Testing popup...');
                            const popup = window.open('https://www.google.com', 'testPopup', 'width=600,height=400');
                            if (popup) {
                                console.log('Popup opened successfully');
                            } else {
                                console.log('Popup blocked or failed');
                            }
                        }

                        // Test external link
                        function testExternalLink() {
                            console.log('Testing external link...');
                            window.open('https://github.com/Voltstro-Studios/UnityWebBrowser', '_blank');
                        }

                        // Add test buttons to page
                        const testDiv = document.createElement('div');
                        testDiv.style.position = 'fixed';
                        testDiv.style.top = '10px';
                        testDiv.style.right = '10px';
                        testDiv.style.zIndex = '9999';
                        testDiv.style.background = 'rgba(0,0,0,0.8)';
                        testDiv.style.color = 'white';
                        testDiv.style.padding = '10px';
                        testDiv.style.borderRadius = '5px';

                        const popupBtn = document.createElement('button');
                        popupBtn.textContent = 'Test Popup';
                        popupBtn.onclick = testPopup;
                        popupBtn.style.margin = '5px';

                        const linkBtn = document.createElement('button');
                        linkBtn.textContent = 'Test External Link';
                        linkBtn.onclick = testExternalLink;
                        linkBtn.style.margin = '5px';

                        testDiv.appendChild(popupBtn);
                        testDiv.appendChild(document.createElement('br'));
                        testDiv.appendChild(linkBtn);
                        document.body.appendChild(testDiv);

                        console.log('Popup test buttons added to page');
                    ";

                    currentWebView.ExecuteJavaScript(popupTestScript);
                    ShowOutput("Popup test injected - look for test buttons in top-right corner of WebView");
                }
                else
                {
                    ShowOutput("Popup testing not implemented for this WebView adapter yet");
                }
            }
            else
            {
                ShowOutput("No active WebView.");
            }
        }

        private IWebViewAdapter CreateWebViewAdapter(WebViewPackage package)
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
            return new VoltUnityWebBrowserAdapter();
#else
            ShowOutput("❌ Cannot create WebView - platform not supported");
            throw new System.PlatformNotSupportedException("Volt Unity Web Browser only supports Windows and Mac platforms");
#endif
        }

        private void OnLoginPageLoaded(string url)
        {
            ShowOutput($"Login page loaded: {url}");

            // Show the WebView once it's loaded
            currentWebView?.Show();

            // Test basic functionality
            TestWebViewFeatures();
        }

        private void TestWebViewFeatures()
        {
            ShowOutput("Testing WebView features...");

            // Test JavaScript execution
            currentWebView?.ExecuteJavaScript(@"
                console.log('Unity WebView test message');

                // Test if login elements are present
                const loginButton = document.querySelector('button[type=""submit""]');
                const emailInput = document.querySelector('input[type=""email""]');

                window.unityWebViewTest = {
                    hasLoginButton: !!loginButton,
                    hasEmailInput: !!emailInput,
                    pageTitle: document.title,
                    timestamp: Date.now()
                };

                // Send test message to Unity
                if (window.unityInstance) {
                    window.unityInstance.SendMessage('WebViewTestManager', 'OnTestResult', JSON.stringify(window.unityWebViewTest));
                }
            ");
        }

        private void OnWebViewMessage(string message)
        {
            ShowOutput($"Received message: {message}");

            try
            {
                var testResult = JsonUtility.FromJson<WebViewTestResult>(message);
                ShowOutput($"Login elements found - Button: {testResult.hasLoginButton}, Email: {testResult.hasEmailInput}");
            }
            catch (Exception ex)
            {
                ShowOutput($"Failed to parse message: {ex.Message}");
            }
        }

        private void OnTestMessage(string message)
        {
            ShowOutput($"Test message received: {message}");
        }

        // Called from JavaScript
        public void OnTestResult(string jsonResult)
        {
            OnWebViewMessage(jsonResult);
        }

        private void ShowOutput(string message)
        {
            if (statusOutput != null)
            {
                statusOutput.text = message;
            }

            Debug.Log($"[WebViewTest] {message}");
        }

        private void ShowPerformance(string message)
        {
            if (performanceOutput != null)
            {
                performanceOutput.text = message;
            }
        }

        private float GetMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f);
        }

        [System.Serializable]
        public class WebViewTestResult
        {
            public bool hasLoginButton;
            public bool hasEmailInput;
            public string pageTitle;
            public long timestamp;
        }
    }
}
