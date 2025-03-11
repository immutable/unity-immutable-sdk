#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using Immutable.Passport.Core.Logging;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Core.Js;
using VoltstroStudios.UnityWebBrowser.Helper;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;

// ReSharper disable Unity.IncorrectScriptableObjectInstantiation

namespace VoltstroStudios.UnityWebBrowser
{
    public class UwbWebView : MonoBehaviour, IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate? OnUnityPostMessage;

        private WebBrowserClient? webBrowserClient;

        public async UniTask Init(int engineStartupTimeoutMs)
        {
            GameObject persistentObject = new GameObject("UWB");
            WebBrowserNoUi browser = persistentObject.AddComponent<WebBrowserNoUi>();
            DontDestroyOnLoad(persistentObject);

            WebBrowserClient webBrowserClient = browser.browserClient;

            webBrowserClient.engineStartupTimeout = engineStartupTimeoutMs;
            webBrowserClient.headless = true;
            webBrowserClient.noSandbox = true;

            // Log level
            webBrowserClient.logSeverity = PassportLogger.CurrentLogLevel switch
            {
                LogLevel.Debug => LogSeverity.Debug,
                LogLevel.Warn => LogSeverity.Warn,
                LogLevel.Error => LogSeverity.Error,
                _ => LogSeverity.Info
            };

            // Js
            webBrowserClient.jsMethodManager = new JsMethodManager { jsMethodsEnable = true };
            webBrowserClient.RegisterJsMethod<string>("callback",
                (message) => { OnUnityPostMessage?.Invoke(message); });

            // Cache
            var browserEngineMainDir = WebBrowserUtils.GetAdditionFilesDirectory();
            webBrowserClient.CachePath = new FileInfo(Path.Combine(browserEngineMainDir, "ImmutableSDK/UWBCache"));

            // Game bridge path
            webBrowserClient.initialUrl = GameBridge.GetFilePath();

            // Set up engine
            EngineConfiguration engineConfig = ScriptableObject.CreateInstance<EngineConfiguration>();
            engineConfig.engineAppName = "UnityWebBrowser.Engine.Cef";
            engineConfig.engineFiles = new Engine.EnginePlatformFiles[]
            {
                new Engine.EnginePlatformFiles()
                {
                    platform = Platform.Windows64,
                    engineBaseAppLocation = "",
                    engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
                    ,
                    engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.win.x64@2.2.5-130.1.16/Engine~"
#endif
                }
            };
            webBrowserClient.engine = engineConfig;

            // Find available ports
            TCPCommunicationLayer tcpCommunicationLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();
            var rnd = new System.Random();
            do
            {
                tcpCommunicationLayer.inPort = rnd.Next(1024, 65353);
                tcpCommunicationLayer.outPort = tcpCommunicationLayer.inPort + 1;
            } 
            while (!CheckAvailableServerPort(tcpCommunicationLayer.inPort) || !CheckAvailableServerPort(tcpCommunicationLayer.outPort));

            webBrowserClient.communicationLayer = tcpCommunicationLayer;

            // Wait for UWB to be connected
            await WaitForClientConnected(webBrowserClient);

            this.webBrowserClient = webBrowserClient;
        }

        private UniTask WaitForClientConnected(WebBrowserClient webBrowserClient)
        {
            var tcs = new TaskCompletionSource<bool>();

            webBrowserClient.OnLoadFinish += OnLoadFinish;

            return tcs.Task.AsUniTask();

            void OnLoadFinish(string url)
            {
                webBrowserClient.OnLoadFinish -= OnLoadFinish;
                tcs.SetResult(true);
            }
        }

        private bool CheckAvailableServerPort(int port)
        {
            var tcpConnInfoArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return tcpConnInfoArray.All(endpoint => endpoint.Port != port);
        }

        public void ExecuteJs(string js)
        {
            webBrowserClient?.ExecuteJs(js);
        }

        public void LaunchAuthURL(string url, string redirectUri)
        {
            Application.OpenURL(url);
        }

        public void Dispose()
        {
            if (webBrowserClient?.HasDisposed == true) return;

            webBrowserClient?.Dispose();
        }
    }
}

#endif