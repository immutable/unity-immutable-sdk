using System;

namespace Immutable.Passport
{
    /// <summary>
    /// Message structure for Vuplex WebView communication
    /// Matches the JSON format: {method: "MethodName", data: "..."}
    /// </summary>
    [System.Serializable]
    public class VuplexMessage
    {
        public string method;
        public string data;
    }

    /// <summary>
    /// Error data structure for JavaScript error messages
    /// Matches the TypeScript Error serialization: {message: string, name: string}
    /// </summary>
    [System.Serializable]
    public class ErrorData
    {
        public string message;
        public string name;

        public override string ToString()
        {
            return $"ErrorData(name: {name}, message: {message})";
        }
    }
    /// <summary>
    /// Platform abstraction interface for PassportUI WebView implementations.
    /// Provides a unified API for different WebView technologies across platforms:
    /// - Windows: Volt Unity Web Browser (UWB) with Chromium CEF
    /// - macOS: Volt Unity Web Browser (UWB) with Chromium CEF
    /// - iOS: Vuplex 3D WebView with WKWebView
    /// - Android: Gree WebView with Android WebView
    /// </summary>
    public interface IPassportWebView
    {
        /// <summary>
        /// Load a URL in the WebView
        /// </summary>
        /// <param name="url">The URL to load</param>
        void LoadUrl(string url);

        /// <summary>
        /// Show the WebView (make it visible to the user)
        /// </summary>
        void Show();

        /// <summary>
        /// Hide the WebView (make it invisible)
        /// </summary>
        void Hide();

        /// <summary>
        /// Execute JavaScript code in the WebView
        /// </summary>
        /// <param name="js">JavaScript code to execute</param>
        void ExecuteJavaScript(string js);

        /// <summary>
        /// Event triggered when JavaScript sends a message to Unity
        /// The string parameter contains the message data from JavaScript
        /// </summary>
        event Action<string> OnJavaScriptMessage;

        /// <summary>
        /// Event triggered when a page finishes loading in the WebView
        /// </summary>
        event Action OnLoadFinished;

        /// <summary>
        /// Event triggered when a page starts loading in the WebView
        /// </summary>
        event Action OnLoadStarted;

        /// <summary>
        /// Get or set whether the WebView is currently visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Get the current URL loaded in the WebView
        /// </summary>
        string CurrentUrl { get; }

        /// <summary>
        /// Initialize the WebView with the specified configuration
        /// </summary>
        /// <param name="config">WebView configuration options</param>
        void Initialize(PassportWebViewConfig config);

        /// <summary>
        /// Register a JavaScript method that can be called from web pages
        /// </summary>
        /// <param name="methodName">Name of the method to register</param>
        /// <param name="handler">Handler function to call when JavaScript invokes this method</param>
        void RegisterJavaScriptMethod(string methodName, Action<string> handler);

        /// <summary>
        /// Clean up resources and dispose of the WebView
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Configuration options for PassportUI WebView
    /// </summary>
    public class PassportWebViewConfig
    {
        /// <summary>
        /// Enable remote debugging (Chrome DevTools, Safari Web Inspector, etc.)
        /// </summary>
        public bool EnableRemoteDebugging { get; set; } = false;

        /// <summary>
        /// Port for remote debugging (Windows UWB only)
        /// </summary>
        public uint RemoteDebuggingPort { get; set; } = 9222;

        /// <summary>
        /// Clear cache on initialization
        /// </summary>
        public bool ClearCacheOnInit { get; set; } = false;

        /// <summary>
        /// Initial URL to load (use "about:blank" for blank page)
        /// </summary>
        public string InitialUrl { get; set; } = "about:blank";

        /// <summary>
        /// WebView width in pixels (0 = use RawImage width)
        /// </summary>
        public int Width { get; set; } = 1920;

        /// <summary>
        /// WebView height in pixels (0 = use RawImage height)
        /// </summary>
        public int Height { get; set; } = 1080;

        /// <summary>
        /// Custom User-Agent string (optional)
        /// </summary>
        public string UserAgent { get; set; } = "";

        /// <summary>
        /// Platform-specific configuration data
        /// </summary>
        public object PlatformSpecificConfig { get; set; }
    }
}
