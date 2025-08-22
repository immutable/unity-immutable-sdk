#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

using System;
using System.Collections.Generic;
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
using VoltstroStudios.UnityWebBrowser.Logging;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;

// ReSharper disable Unity.IncorrectScriptableObjectInstantiation

namespace VoltstroStudios.UnityWebBrowser
{
    public class UwbWebView : MonoBehaviour, IWebBrowserClient
    {
        public event OnUnityPostMessageDelegate? OnUnityPostMessage;

        // Required for Gree browser only
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        public event OnUnityPostMessageDelegate? OnAuthPostMessage;
        public event OnUnityPostMessageErrorDelegate? OnPostMessageError;
#endif

        private WebBrowserClient? webBrowserClient;

        public async UniTask Init(int engineStartupTimeoutMs, bool redactTokensInLogs, Func<string, string> redactionHandler)
        {
            GameObject persistentObject = new GameObject("UWB");
            WebBrowserNoUi browser = persistentObject.AddComponent<WebBrowserNoUi>();
            DontDestroyOnLoad(persistentObject);

            WebBrowserClient webBrowserClient = browser.browserClient;

            webBrowserClient.engineStartupTimeout = engineStartupTimeoutMs;
            webBrowserClient.headless = true;
            webBrowserClient.noSandbox = true;

            // Log level
            var logSeverity = PassportLogger.CurrentLogLevel switch
            {
                LogLevel.Debug => LogSeverity.Debug,
                LogLevel.Warn => LogSeverity.Warn,
                LogLevel.Error => LogSeverity.Error,
                _ => LogSeverity.Info
            };
            webBrowserClient.logSeverity = logSeverity;

            // Logger
            webBrowserClient.Logger = new DefaultUnityWebBrowserLogger(logSeverity: logSeverity, redactionHandler: redactTokensInLogs ? redactionHandler : null);

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
            
            var engineFiles = new List<Engine.EnginePlatformFiles>();
            
            // Windows engine configuration
            engineFiles.Add(new Engine.EnginePlatformFiles()
            {
                platform = Platform.Windows64,
                engineBaseAppLocation = "",
                engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
                ,
                engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.win.x64@2.2.5-130.1.16/Engine~"
#endif
            });
            
                    // macOS engine configuration (Intel)
        engineFiles.Add(new Engine.EnginePlatformFiles()
        {
            platform = Platform.MacOS,
            engineBaseAppLocation = "UnityWebBrowser.Engine.Cef.app/Contents/MacOS",
            engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
            ,
            engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.macos.x64@2.2.5-130.1.16/Engine~"
#endif
        });

        // macOS engine configuration (ARM64 - Apple Silicon)
        engineFiles.Add(new Engine.EnginePlatformFiles()
        {
            platform = Platform.MacOSArm64,
            engineBaseAppLocation = "UnityWebBrowser.Engine.Cef.app/Contents/MacOS",
            engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
            ,
            engineEditorLocation = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser/dev.voltstro.unitywebbrowser.engine.cef.macos.arm64@2.2.5-130.1.16/Engine~"
#endif
        });
            
            engineConfig.engineFiles = engineFiles.ToArray();
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

        // Only available for mobile devices
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            // UWB doesn't have direct cache clearing methods for mobile
            // This would need to be implemented if mobile support is added
        }

        public void ClearStorage()
        {
            // UWB doesn't have direct storage clearing methods for mobile
            // This would need to be implemented if mobile support is added
        }
#endif

        // Required for Windows browser only
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