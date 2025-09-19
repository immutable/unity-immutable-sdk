#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if UNITY_IOS && !UNITY_EDITOR
using Vuplex.WebView;
#endif

namespace Immutable.Passport
{
    /// <summary>
    /// iOS implementation of IPassportWebView using Vuplex WebView
    /// Provides embedded WebView functionality within the Unity app
    /// Consistent with Android and macOS Vuplex implementations
    /// </summary>
    public class iOSPassportWebView : IPassportWebView
    {
        private const string TAG = "[iOSPassportWebView]";

#if UNITY_IOS && !UNITY_EDITOR
        private CanvasWebViewPrefab? _webViewPrefab;
#endif
        private readonly Dictionary<string, Action<string>> _jsHandlers = new Dictionary<string, Action<string>>();
        private readonly RawImage _canvasReference;
        private bool _isInitialized = false;

        public event Action<string>? OnJavaScriptMessage;
        public event Action? OnLoadFinished;
        public event Action? OnLoadStarted;

        // Safe access - check initialization
#if UNITY_IOS && !UNITY_EDITOR
        public bool IsVisible => _webViewPrefab?.Visible ?? false;
        public string CurrentUrl => _webViewPrefab?.WebView?.Url ?? "";
#else
        public bool IsVisible => false;
        public string CurrentUrl => "";
#endif

        public iOSPassportWebView(RawImage canvasReference)
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

#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                PassportLogger.Info($"{TAG} Initializing iOS WebView...");

                // Start async initialization but don't wait
                InitializeAsync(config).Forget();
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize: {ex.Message}");
                throw;
            }
#else
            PassportLogger.Warn($"{TAG} Vuplex WebView is only supported on iOS builds, not in editor");
            _isInitialized = true;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private async UniTaskVoid InitializeAsync(PassportWebViewConfig config)
        {
            try
            {
                // Create WebView prefab and parent to Canvas
                _webViewPrefab = CanvasWebViewPrefab.Instantiate();
                _webViewPrefab.Native2DModeEnabled = false; // Disable Native2DMode to avoid Unity integration issues

                // Set reasonable resolution - much lower to avoid texture size issues
                _webViewPrefab.Resolution = 1.0f; // 1px per Unity unit - creates 800x600px texture

                // Must be child of Canvas for Vuplex to work
                _webViewPrefab.transform.SetParent(_canvasReference.canvas.transform, false);

                // Set WebView size based on configuration
                var rect = _webViewPrefab.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
                rect.anchorMax = new Vector2(0.5f, 0.5f);

                // Use configured dimensions or fallback to reasonable defaults
                float width = config.Width > 0 ? config.Width : 1000;
                float height = config.Height > 0 ? config.Height : 700;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero; // Center position

                PassportLogger.Info($"{TAG} Using WebView dimensions: {width}x{height}");

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
                PassportLogger.Info($"{TAG} iOS WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize iOS WebView: {ex.Message}");
                throw;
            }
        }
#endif

        public void LoadUrl(string url)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot load URL - iOS WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.LoadUrl(url);
#else
            PassportLogger.Warn($"{TAG} LoadUrl not supported in iOS editor mode");
#endif
        }

        public void Show()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = true;
            }
#endif
        }

        public void Hide()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = false;
            }
#endif
        }

        public void ExecuteJavaScript(string js)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - iOS WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.ExecuteJavaScript(js);
#endif
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
#if UNITY_IOS && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Destroy();
                _webViewPrefab = null;
            }
#endif

            _jsHandlers.Clear();
            _isInitialized = false;
        }
    }
}
