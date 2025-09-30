#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && VUPLEX_WEBVIEW
using Vuplex.WebView;

namespace Immutable.Passport
{
    /// <summary>
    /// MacOS implementation of IPassportWebView using Vuplex WebView
    /// Provides embedded WebView functionality within the Unity app
    /// Similar to iOS implementation but optimized for MacOS desktop environment
    /// </summary>
    public class MacOSPassportWebView : IPassportWebView
    {
        private const string TAG = "[MacOSPassportWebView]";

        private CanvasWebViewPrefab? _webViewPrefab;
        private readonly Dictionary<string, Action<string>> _jsHandlers = new Dictionary<string, Action<string>>();
        private readonly RawImage _canvasReference;
        private bool _isInitialized = false;
        private string? _queuedUrl = null; // Queue URL if LoadUrl called before initialization

        public event Action<string>? OnJavaScriptMessage;
        public event Action? OnLoadFinished;
        public event Action? OnLoadStarted;

        public bool IsVisible => _webViewPrefab?.Visible ?? false;
        public string CurrentUrl => _webViewPrefab?.WebView?.Url ?? "";

        public MacOSPassportWebView(RawImage canvasReference)
        {
            _canvasReference = canvasReference ?? throw new ArgumentNullException(nameof(canvasReference));
        }

        public void Initialize(PassportWebViewConfig config)
        {
            if (_isInitialized)
            {
                PassportLogger.Warn($"{TAG} Already initialized, skipping");
                return;
            }

            try
            {
                PassportLogger.Info($"{TAG} Initializing MacOS WebView...");

                // Start async initialization but don't wait
                InitializeAsync(config).Forget();
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize: {ex.Message}");
                throw;
            }
        }

        private async UniTaskVoid InitializeAsync(PassportWebViewConfig config)
        {
            try
            {
                PassportLogger.Info($"{TAG} Starting Vuplex CanvasWebViewPrefab instantiation...");

                // Apply aggressive performance optimizations for macOS
                try
                {
                    StandaloneWebView.SetCommandLineArguments(
                        "--disable-gpu " +
                        "--disable-gpu-compositing " +
                        "--disable-software-rasterizer " +
                        "--disable-background-timer-throttling " +
                        "--disable-renderer-backgrounding " +
                        "--disable-features=TranslateUI " +
                        "--no-sandbox"
                    );
                    PassportLogger.Info($"{TAG} Applied comprehensive performance optimizations for macOS");
                }
                catch (System.Exception ex)
                {
                    PassportLogger.Warn($"{TAG} Could not apply performance optimizations: {ex.Message}");
                }

                // Create WebView prefab and parent to Canvas
                _webViewPrefab = CanvasWebViewPrefab.Instantiate();
                PassportLogger.Info($"{TAG} CanvasWebViewPrefab created successfully");

                // Enable Native2DMode and additional performance settings
                _webViewPrefab.Native2DModeEnabled = true; // Direct native rendering - fastest on desktop
                _webViewPrefab.Resolution = 0.5f; // Balanced resolution for desktop

                // Additional 2D mode optimizations
                if (_webViewPrefab.Native2DModeEnabled)
                {
                    PassportLogger.Info($"{TAG} Native2DMode confirmed enabled - using direct native rendering");
                    // In Native2D mode, reduce pixel density for better performance
                    _webViewPrefab.PixelDensity = 1.0f; // Standard density, no high-DPI overhead
                }

                // Must be child of Canvas for Vuplex to work
                _webViewPrefab.transform.SetParent(_canvasReference.canvas.transform, false);

                // Set WebView size based on configuration
                var rect = _webViewPrefab.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
                rect.anchorMax = new Vector2(0.5f, 0.5f);

                // Use configured dimensions or fallback to desktop-appropriate defaults (optimized for performance)
                float width = config.Width > 0 ? config.Width : 600;
                float height = config.Height > 0 ? config.Height : 650;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero; // Center position

                PassportLogger.Info($"{TAG} Using WebView dimensions: {width}x{height}");

                // Wait for WebView initialization with timing
                var startTime = System.DateTime.Now;
                await _webViewPrefab.WaitUntilInitialized();
                var initTime = (System.DateTime.Now - startTime).TotalSeconds;
                PassportLogger.Info($"{TAG} Vuplex WebView initialization completed in {initTime:F2}s");

                // Pre-load the login page for instant display
                try
                {
                    if (!string.IsNullOrEmpty(config.InitialUrl) && config.InitialUrl != "about:blank")
                    {
                        _webViewPrefab.WebView.LoadUrl(config.InitialUrl);
                        PassportLogger.Info($"{TAG} Pre-loaded login page: {config.InitialUrl}");
                    }
                    else
                    {
                        // Load minimal blank page if no URL provided (rare edge case)
                        _webViewPrefab.WebView.LoadHtml("<html><body style='margin:0;padding:20px;font-family:system-ui;color:#666;text-align:center;'>Initializing...</body></html>");
                        PassportLogger.Info($"{TAG} Loaded minimal blank page (no InitialUrl provided)");
                    }
                }
                catch (System.Exception ex)
                {
                    PassportLogger.Warn($"{TAG} Could not pre-load content: {ex.Message}");
                }

                // Setup event handlers
                _webViewPrefab.WebView.LoadProgressChanged += (sender, progressArgs) =>
                {
                    if (progressArgs.Type == ProgressChangeType.Started)
                    {
                        OnLoadStarted?.Invoke();
                    }
                    else if (progressArgs.Type == ProgressChangeType.Finished)
                    {
                        OnLoadFinished?.Invoke();
                    }
                };
                _webViewPrefab.WebView.MessageEmitted += (sender, messageArgs) =>
                {
                    try
                    {
                        // Parse the JSON message from window.vuplex.postMessage()
                        var message = JsonUtility.FromJson<VuplexMessage>(messageArgs.Value);

                        if (_jsHandlers.ContainsKey(message.method))
                        {
                            _jsHandlers[message.method]?.Invoke(message.data);
                            return;
                        }

                        PassportLogger.Warn($"{TAG} No handler registered for method: {message.method}");
                    }
                    catch (Exception ex)
                    {
                        PassportLogger.Error($"{TAG} Failed to parse Vuplex message: {ex.Message}, Raw message: {messageArgs.Value}");
                        // Fallback to raw message for backwards compatibility
                        OnJavaScriptMessage?.Invoke(messageArgs.Value);
                    }
                };
                _webViewPrefab.WebView.LoadFailed += (sender, failedArgs) => PassportLogger.Warn($"{TAG} Load failed: {failedArgs.NativeErrorCode} for {failedArgs.Url}");

                _isInitialized = true;
                PassportLogger.Info($"{TAG} MacOS WebView initialized successfully");

                // Load queued URL if one was requested before initialization completed
                if (!string.IsNullOrEmpty(_queuedUrl))
                {
                    PassportLogger.Info($"{TAG} Loading queued URL: {_queuedUrl}");
                    var urlToLoad = _queuedUrl;
                    _queuedUrl = null; // Clear the queue
                    _webViewPrefab.WebView.LoadUrl(urlToLoad);
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize MacOS WebView: {ex.Message}");
                throw;
            }
        }

        public void LoadUrl(string url)
        {
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Info($"{TAG} WebView not ready, queueing URL: {url}");
                _queuedUrl = url; // Queue the URL for later loading
                return;
            }

            // Check if the requested URL is already loaded (performance optimization)
            var currentUrl = _webViewPrefab.WebView.Url;
            if (currentUrl == url)
            {
                PassportLogger.Info($"{TAG} URL already loaded, showing instantly: {url}");
                // No need to reload - just show the WebView if hidden
                if (!_webViewPrefab.Visible)
                {
                    _webViewPrefab.Visible = true;
                }
                return;
            }

            PassportLogger.Info($"{TAG} Loading URL: {url}");
            _webViewPrefab.WebView.LoadUrl(url);
        }

        public void Show()
        {
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = true;
                PassportLogger.Info($"{TAG} WebView shown");
            }
        }

        public void Hide()
        {
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = false;
                PassportLogger.Info($"{TAG} WebView hidden");
            }
        }

        public void ExecuteJavaScript(string js)
        {
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - MacOS WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.ExecuteJavaScript(js);
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            _jsHandlers[methodName] = handler;
            PassportLogger.Info($"{TAG} JavaScript method '{methodName}' registered for Vuplex message handling");

            // Note: No JavaScript injection needed with Vuplex - web page should call:
            // window.vuplex.postMessage({method: 'methodName', data: 'jsonData'})
        }

        public void Dispose()
        {
            if (_webViewPrefab != null)
            {
                PassportLogger.Info($"{TAG} Disposing MacOS WebView");
                _webViewPrefab.Destroy();
                _webViewPrefab = null;
            }

            _jsHandlers.Clear();
            _isInitialized = false;
        }
    }
}
#endif
