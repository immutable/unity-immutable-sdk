#if UWB_WEBVIEW && !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Core.Js;
using VoltstroStudios.UnityWebBrowser.Helper;
using VoltstroStudios.UnityWebBrowser.Input;

namespace Immutable.Passport
{
    /// <summary>
    /// UnityWebBrowser-based implementation of IWebBrowserClient used by Passport on Windows.
    /// </summary>
    public class UwbWebView : MonoBehaviour, IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate? OnUnityPostMessage;

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;
#endif

        private WebBrowserClient? webBrowserClient;

        public async UniTask Init(int engineStartupTimeoutMs, bool redactTokensInLogs, Func<string, string> redactionHandler)
        {
            GameObject persistentObject = new GameObject("UWB_Bridge");

            var rawImage = persistentObject.AddComponent<RawImage>();
            rawImage.color = Color.clear;

            var ui = persistentObject.AddComponent<WebBrowserUIFull>();

            // Assign a basic input handler so UWB doesn't throw
            var inputHandler = ScriptableObject.CreateInstance<WebBrowserOldInputHandler>();
            ui.inputHandler = inputHandler;

            DontDestroyOnLoad(persistentObject);

            WebBrowserClient browserClient = ui.browserClient;
            browserClient.engineStartupTimeout = engineStartupTimeoutMs;

            // Disable sandbox for Windows VM compatibility
            browserClient.noSandbox = true;

            // Apply Passport logging preferences to the UWB client
            UwbLogConfig.ApplyTo(browserClient);

            // Js
            browserClient.jsMethodManager = new JsMethodManager { jsMethodsEnable = true };
            browserClient.RegisterJsMethod<string>("callback",
                (message) => { OnUnityPostMessage?.Invoke(message); });

            // Cache
            var browserEngineMainDir = WebBrowserUtils.GetAdditionFilesDirectory();
            browserClient.CachePath = new FileInfo(Path.Combine(browserEngineMainDir, "ImmutableSDK/UWBCache"));

            // Game bridge path
            browserClient.initialUrl = GameBridge.GetFilePath();

            // Set up engine from standard UWB configuration asset
            var engineConfigAsset = Resources.Load<EngineConfiguration>("Cef Engine Configuration");
            if (engineConfigAsset == null)
            {
                Debug.LogError("[UwbWebView] Could not find 'Cef Engine Configuration' Resources asset. " +
                               "Ensure the UnityWebBrowser engine package is installed.");
            }
            else
            {
                var engineConfig = ScriptableObject.Instantiate(engineConfigAsset);
                browserClient.engine = engineConfig;
            }

            // Find available ports
            TCPCommunicationLayer tcpCommunicationLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();
            var rnd = new System.Random();
            do
            {
                tcpCommunicationLayer.inPort = rnd.Next(1024, 65353);
                tcpCommunicationLayer.outPort = tcpCommunicationLayer.inPort + 1;
            } while (!CheckAvailableServerPort(tcpCommunicationLayer.inPort) || !CheckAvailableServerPort(tcpCommunicationLayer.outPort));

            browserClient.communicationLayer = tcpCommunicationLayer;

            await WaitForClientConnected(browserClient);

            this.webBrowserClient = browserClient;
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

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
        }

        public void ClearStorage()
        {
        }
#endif

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        public void Dispose()
        {
            if (webBrowserClient?.HasDisposed == true) return;

            webBrowserClient?.Dispose();
        }
#endif
    }
}

#endif
