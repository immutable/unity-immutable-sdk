#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN) || UNITY_WEBGL

using System.IO;
using UnityEngine;
using Immutable.Browser.Core;
using Cysharp.Threading.Tasks;

namespace Immutable.Browser.Core
{
    /// <summary>
    /// The interface for implementing a custom Windows web browser client.
    /// <para>
    /// To use a custom web browser client, ensure you include <c>IMMUTABLE_CUSTOM_BROWSER</c> in your game's Scripting Define Symbols.
    /// This will enable the SDK to integrate with your custom browser implementation instead of using the default one.
    /// </para>
    /// </summary>
    public interface IWindowsWebBrowserClient
    {
        /// <summary>
        /// Event triggered when the JavaScript API defined in <see cref="GetPostMessageApiCall"/> posts a message 
        /// to the Unity application.
        /// </summary>
        /// <example>
        /// <code>
        /// // Example implementation for Vuplex Windows WebView:
        /// public event OnUnityPostMessageDelegate OnUnityPostMessage; 
        /// 
        /// vuplexWebView.MessageEmitted += (sender, eventArgs) =>
        /// {
        ///     OnUnityPostMessage?.Invoke(eventArgs.Value);
        /// };
        /// </code>
        /// </example>
        event OnUnityPostMessageDelegate OnUnityPostMessage;

        /// <summary>
        /// Initialises the Windows Web Browser Client
        /// </summary>
        /// <returns></returns>
        UniTask Init();

        /// <summary>
        /// Loads the specified URL.
        /// </summary>
        /// <param name="url">The URL to load.</param>
        void LoadUrl(string url);

        /// <summary>
        /// Executes the specified JavaScript code in the web browser.
        /// </summary>
        /// <param name="javaScript">The JavaScript code to execute.</param>
        void ExecuteJavaScript(string javaScript);

        /// <summary>
        /// Returns the JavaScript API call for sending messages from the web page to the Unity application.
        /// </summary>
        /// <remarks>
        /// This API call must take exactly one argument of type string.
        /// Implementers should provide the appropriate JavaScript function for their web browser client.
        /// </remarks>
        /// <returns>
        /// A string representing the JavaScript API call for sending messages from the web page to the Unity application.
        /// </returns>
        /// <example>
        /// <code>
        /// // Example implementation for Vuplex Windows WebView:
        /// public string GetPostMessageApiCall() {
        ///     return "window.vuplex.postMessage";
        /// }
        /// </code>
        /// </example>
        string GetPostMessageApiCall();

        /// <summary>
        /// Destroys the web browser client and releases all of its resources.
        /// </summary>
        void Dispose();
    }
}

#endif