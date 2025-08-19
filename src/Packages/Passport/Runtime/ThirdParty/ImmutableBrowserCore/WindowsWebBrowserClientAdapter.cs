#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

using System.IO;
using UnityEngine;
using Immutable.Browser.Core;
using Immutable.Passport.Core.Logging;
using Cysharp.Threading.Tasks;

namespace Immutable.Browser.Core
{
    public class WindowsWebBrowserClientAdapter : IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        private readonly IWindowsWebBrowserClient webBrowserClient;

        public WindowsWebBrowserClientAdapter(IWindowsWebBrowserClient windowsWebBrowserClient)
        {
            webBrowserClient = windowsWebBrowserClient;

            // Listen to messages from the web browser client
            windowsWebBrowserClient.OnUnityPostMessage += (message) =>
            {
                OnUnityPostMessage?.Invoke(message);
            };
        }

        /// <summary>
        /// Initialises the web browser client, loads the game bridge file, and sets up the UnityPostMessage function.
        /// </summary>
        public async UniTask Init()
        {
            // Initialise the web browser client asynchronously
            await webBrowserClient.Init();

            // Load the game bridge file into the web browser client
            webBrowserClient.LoadUrl(GameBridge.GetFilePath());

            // Get the JavaScript API call for posting messages from the web page to the Unity application
            string postMessageApiCall = webBrowserClient.GetPostMessageApiCall();

            // Inject a JavaScript function named UnityPostMessage into the web page
            // This function takes a message as an argument and calls the post message API
            webBrowserClient.ExecuteJavaScript($"function UnityPostMessage(message) {{ {postMessageApiCall}(message); }}");
        }

        public void ExecuteJs(string js)
        {
            webBrowserClient.ExecuteJavaScript(js);
        }

        public void LaunchAuthURL(string url, string? redirectUri)
        {
            // Log the auth URL for test automation to capture
            PassportLogger.Info($"PASSPORT_AUTH_URL: {url}");
            Application.OpenURL(url);
        }

        public void Dispose()
        {
            webBrowserClient.Dispose();
        }
    }
}

#endif