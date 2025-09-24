#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if UNITY_ANDROID && VUPLEX_WEBVIEW
using Vuplex.WebView;

namespace Immutable.Passport
{
    /// <summary>
    /// Android implementation of IPassportWebView using Vuplex WebView
    /// Provides embedded WebView functionality within the Unity app
    /// Consistent with iOS and macOS Vuplex implementations
    /// </summary>
    public class AndroidVuplexWebView : IPassportWebView
    {
        private const string TAG = "[AndroidVuplexWebView]";

        private CanvasWebViewPrefab? _webViewPrefab;
        private readonly Dictionary<string, Action<string>> _jsHandlers = new Dictionary<string, Action<string>>();
        private readonly RawImage _canvasReference;
        private bool _isInitialized = false;

        public event Action<string>? OnJavaScriptMessage;
        public event Action? OnLoadFinished;
        public event Action? OnLoadStarted;

        public bool IsVisible => _webViewPrefab?.Visible ?? false;
        public string CurrentUrl => _webViewPrefab?.WebView?.Url ?? "";

        public AndroidVuplexWebView(RawImage canvasReference)
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
                PassportLogger.Info($"{TAG} Initializing Vuplex WebView...");

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
                // Create WebView prefab and parent to Canvas
                _webViewPrefab = CanvasWebViewPrefab.Instantiate();
                _webViewPrefab.Native2DModeEnabled = true;

                // Must be child of Canvas for Vuplex to work
                _webViewPrefab.transform.SetParent(_canvasReference.canvas.transform, false);

                // Set WebView size based on configuration
                var rect = _webViewPrefab.GetComponent<RectTransform>();

                // Use configured dimensions or fallback to full-screen for Native 2D Mode
                if (config.Width > 0 && config.Height > 0)
                {
                    // Center the WebView with specific dimensions
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(config.Width, config.Height);
                    rect.anchoredPosition = Vector2.zero;
                    PassportLogger.Info($"{TAG} Using configured dimensions: {config.Width}x{config.Height}");
                }
                else
                {
                    // Full-screen fallback for Native 2D Mode
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    PassportLogger.Info($"{TAG} Using full-screen dimensions for Native 2D Mode");
                }

                // Wait for WebView initialization
                await _webViewPrefab.WaitUntilInitialized();

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
                PassportLogger.Info($"{TAG} Vuplex WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize async: {ex.Message}");
                throw;
            }
        }

        public void LoadUrl(string url)
        {
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot load URL - WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.LoadUrl(url);
        }

        public void Show()
        {
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = true;
            }
        }

        public void Hide()
        {
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = false;
            }
        }

        public void ExecuteJavaScript(string js)
        {
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - WebView not initialized");
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
                _webViewPrefab.Destroy();
                _webViewPrefab = null;
            }

            _jsHandlers.Clear();
            _isInitialized = false;
        }
    }
}
#endif
