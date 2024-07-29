#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

using UnityEngine;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using Vuplex.WebView;

namespace Immutable.Browser.Vuplex
{
    public class VuplexWebView : IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate OnPostMessageError;
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        private IWebView webView;

        public VuplexWebView()
        {
            webView = Web.CreateWebView();
        }

        public async UniTask Init()
        {
            await webView.Init(600, 300);

            // Get game bridge file path
            string filePath = "";
#if UNITY_EDITOR
            filePath = Constants.SCHEME_FILE + Path.GetFullPath($"{Constants.PASSPORT_PACKAGE_RESOURCES_DIRECTORY}{Constants.PASSPORT_HTML_FILE_NAME}");
#elif UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            filePath = Constants.SCHEME_FILE + Path.GetFullPath(Application.dataPath) + Constants.PASSPORT_DATA_DIRECTORY_NAME + Constants.PASSPORT_HTML_FILE_NAME;
#endif
            // Load game bridge file
            webView.LoadUrl(filePath);

            // Listen to messages from game bridge
            webView.MessageEmitted += (sender, eventArgs) => {
                OnUnityPostMessage?.Invoke(eventArgs.Value);
            };
        }

        public void ExecuteJs(string js)
        {
            await webView.ExecuteJavaScript(js);
        }

        public void LaunchAuthURL(string url, string? redirectUri)
        {
            Application.OpenURL(url);
        }
    }
}

#endif