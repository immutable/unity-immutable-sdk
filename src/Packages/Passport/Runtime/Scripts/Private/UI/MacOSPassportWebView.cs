#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using Vuplex.WebView;
#endif

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

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private CanvasWebViewPrefab? _webViewPrefab;
#endif
        private readonly Dictionary<string, Action<string>> _jsHandlers = new Dictionary<string, Action<string>>();
        private readonly RawImage _canvasReference;
        private bool _isInitialized = false;

        public event Action<string>? OnJavaScriptMessage;
        public event Action? OnLoadFinished;
        public event Action? OnLoadStarted;

        // Safe access - check initialization
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        public bool IsVisible => _webViewPrefab?.Visible ?? false;
        public string CurrentUrl => _webViewPrefab?.WebView?.Url ?? "";
#else
        public bool IsVisible => false;
        public string CurrentUrl => "";
#endif

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

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
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
#else
            PassportLogger.Warn($"{TAG} Vuplex WebView is only supported on MacOS builds, not in editor");
            _isInitialized = true;
#endif
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private async UniTaskVoid InitializeAsync(PassportWebViewConfig config)
        {
            try
            {
                // Create WebView prefab and parent to Canvas
                _webViewPrefab = CanvasWebViewPrefab.Instantiate();
                _webViewPrefab.Native2DModeEnabled = false; // Use standard mode for better desktop compatibility

                // Set higher resolution for desktop - MacOS can handle larger textures
                _webViewPrefab.Resolution = 1.5f; // 1.5px per Unity unit for crisp rendering

                // Must be child of Canvas for Vuplex to work
                _webViewPrefab.transform.SetParent(_canvasReference.canvas.transform, false);

                // Set desktop-appropriate size - larger than mobile but not full-screen
                var rect = _webViewPrefab.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1200, 800); // Desktop size: 1200x800 for comfortable login UX
                rect.anchoredPosition = Vector2.zero; // Center position

                // Wait for WebView initialization
                await _webViewPrefab.WaitUntilInitialized();

                // Setup event handlers
                _webViewPrefab.WebView.LoadProgressChanged += (s, e) =>
                {
                    if (e.Type == ProgressChangeType.Started)
                    {
                        OnLoadStarted?.Invoke();
                    }
                    else if (e.Type == ProgressChangeType.Finished)
                    {
                        OnLoadFinished?.Invoke();
                    }
                };
                _webViewPrefab.WebView.MessageEmitted += (s, e) =>
                {
                    foreach (var h in _jsHandlers)
                    {
                        if (e.Value.StartsWith($"{h.Key}:"))
                        {
                            h.Value?.Invoke(e.Value.Substring(h.Key.Length + 1));
                            return;
                        }
                    }

                    OnJavaScriptMessage?.Invoke(e.Value);
                };
                _webViewPrefab.WebView.LoadFailed += (s, e) => PassportLogger.Warn($"{TAG} Load failed: {e.NativeErrorCode} for {e.Url}");

                _isInitialized = true;
                PassportLogger.Info($"{TAG} MacOS WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize MacOS WebView: {ex.Message}");
                throw;
            }
        }
#endif

        public void LoadUrl(string url)
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot load URL - MacOS WebView not initialized");
                return;
            }

            PassportLogger.Info($"{TAG} Loading URL: {url}");
            _webViewPrefab.WebView.LoadUrl(url);
#else
            PassportLogger.Warn($"{TAG} LoadUrl not supported in MacOS editor mode");
#endif
        }

        public void Show()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = true;
                PassportLogger.Info($"{TAG} WebView shown");
            }
#else
            PassportLogger.Info($"{TAG} Show() called (editor mode)");
#endif
        }

        public void Hide()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = false;
                PassportLogger.Info($"{TAG} WebView hidden");
            }
#else
            PassportLogger.Info($"{TAG} Hide() called (editor mode)");
#endif
        }

        public void ExecuteJavaScript(string js)
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - MacOS WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.ExecuteJavaScript(js);
#else
            PassportLogger.Warn($"{TAG} ExecuteJavaScript not supported in MacOS editor mode");
#endif
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            _jsHandlers[methodName] = handler;
            PassportLogger.Info($"{TAG} JavaScript method '{methodName}' registered");

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (_isInitialized && _webViewPrefab?.WebView != null)
            {
                // Register the method with Vuplex WebView using window.vuplex.postMessage
                string jsCode = $"window.{methodName}=d=>window.vuplex?.postMessage('{methodName}:'+(typeof d==='object'?JSON.stringify(d):d))";
                ExecuteJavaScript(jsCode);
                PassportLogger.Info($"{TAG} JavaScript method '{methodName}' registered with Vuplex");
            }
#endif
        }

        public void Dispose()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (_webViewPrefab != null)
            {
                PassportLogger.Info($"{TAG} Disposing MacOS WebView");
                _webViewPrefab.Destroy();
                _webViewPrefab = null;
            }
#endif

            _jsHandlers.Clear();
            _isInitialized = false;
        }
    }
}
