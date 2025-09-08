#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Core.Logging;

#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
using Vuplex.WebView;
#endif

namespace Immutable.Passport
{
    /// <summary>
    /// Android implementation of IPassportWebView using Vuplex WebView
    /// Provides embedded WebView functionality within the Unity app (not external browser)
    /// This is different from AndroidPassportWebView which uses external browser for auth flows
    /// </summary>
    public class AndroidVuplexWebView : IPassportWebView
    {
        private const string TAG = "[AndroidVuplexWebView]";

#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
        private CanvasWebViewPrefab? _webViewPrefab;
#endif
        private readonly Dictionary<string, Action<string>> _jsHandlers = new Dictionary<string, Action<string>>();
        private readonly RawImage _canvasReference;
        private bool _isInitialized = false;

        public event Action<string>? OnJavaScriptMessage;
        public event Action? OnLoadFinished;
        public event Action? OnLoadStarted;

        // Safe access - check initialization
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
        public bool IsVisible => _webViewPrefab?.Visible ?? false;
        public string CurrentUrl => _webViewPrefab?.WebView?.Url ?? "";
#else
        public bool IsVisible => false;
        public string CurrentUrl => "";
#endif

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

#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
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
#else
            PassportLogger.Warn($"{TAG} Vuplex WebView is only supported on Android builds, not in editor");
            _isInitialized = true;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
        private async UniTaskVoid InitializeAsync(PassportWebViewConfig config)
        {
            try
            {
                // Create WebView prefab and parent to Canvas
                _webViewPrefab = CanvasWebViewPrefab.Instantiate();
                _webViewPrefab.Native2DModeEnabled = true;

                // Must be child of Canvas for Vuplex to work
                _webViewPrefab.transform.SetParent(_canvasReference.canvas.transform, false);

                // Set up full-screen layout for Native 2D Mode
                var rect = _webViewPrefab.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

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
                PassportLogger.Info($"{TAG} Vuplex WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize async: {ex.Message}");
                throw;
            }
        }
#endif

        public void LoadUrl(string url)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot load URL - WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.LoadUrl(url);
#else
            PassportLogger.Warn($"{TAG} LoadUrl not supported in editor mode");
#endif
        }

        public void Show()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = true;
            }
#endif
        }

        public void Hide()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
            if (_webViewPrefab != null)
            {
                _webViewPrefab.Visible = false;
            }
#endif
        }

        public void ExecuteJavaScript(string js)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
            if (!_isInitialized || _webViewPrefab?.WebView == null)
            {
                PassportLogger.Error($"{TAG} Cannot execute JavaScript - WebView not initialized");
                return;
            }

            _webViewPrefab.WebView.ExecuteJavaScript(js);
#endif
        }

        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            _jsHandlers[methodName] = handler;

#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
            if (_isInitialized && _webViewPrefab?.WebView != null)
            {
                ExecuteJavaScript($"window.{methodName}=d=>window.vuplex?.postMessage('{methodName}:'+(typeof d==='object'?JSON.stringify(d):d))");
            }
#endif
        }

        public void Dispose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUPLEX_AVAILABLE && VUPLEX_AVAILABLE
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
