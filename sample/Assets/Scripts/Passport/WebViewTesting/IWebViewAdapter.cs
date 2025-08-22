using System;

namespace Immutable.Passport.WebViewTesting
{
    /// <summary>
    /// Interface for WebView adapters to standardize testing across different packages
    /// </summary>
    public interface IWebViewAdapter : IDisposable
    {
        /// <summary>
        /// Event fired when navigation completes
        /// </summary>
        event Action<string> OnNavigationCompleted;
        
        /// <summary>
        /// Event fired when a message is received from JavaScript
        /// </summary>
        event Action<string> OnMessageReceived;
        
        /// <summary>
        /// Event fired when an error occurs
        /// </summary>
        event Action<string> OnError;
        
        /// <summary>
        /// Whether the WebView is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Initialize the WebView with specified dimensions
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        void Initialize(int width, int height);
        
        /// <summary>
        /// Navigate to the specified URL
        /// </summary>
        /// <param name="url">URL to navigate to</param>
        void Navigate(string url);
        
        /// <summary>
        /// Execute JavaScript in the WebView
        /// </summary>
        /// <param name="script">JavaScript code to execute</param>
        void ExecuteJavaScript(string script);
        
        /// <summary>
        /// Send a message to the WebView
        /// </summary>
        /// <param name="message">Message to send</param>
        void SendMessage(string message);
        
        /// <summary>
        /// Show the WebView
        /// </summary>
        void Show();
        
        /// <summary>
        /// Hide the WebView
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Get current performance metrics
        /// </summary>
        /// <returns>Performance data</returns>
        WebViewPerformanceMetrics GetPerformanceMetrics();
    }
    
    [System.Serializable]
    public class WebViewPerformanceMetrics
    {
        public float memoryUsageMB;
        public float cpuUsagePercent;
        public float renderTimeMsAvg;
        public int textureWidth;
        public int textureHeight;
        public string engineVersion;
    }
}
