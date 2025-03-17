#if UNITY_STANDALONE_WIN

using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Core.Js;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;

public class UwbWebView : IWindowsWebBrowserClient
{
    public event OnUnityPostMessageDelegate? OnUnityPostMessage;
    private WebBrowserClient? _webBrowserClient;

    public async UniTask Init()
    {
        var client = new WebBrowserClient(headless: true);

        client.engineStartupTimeout = 60000;
        client.noSandbox = true;

        // Log level
        client.logSeverity = LogSeverity.Info;

        // Js
        client.jsMethodManager = new JsMethodManager { jsMethodsEnable = true };
        client.RegisterJsMethod<string>("callback",
            (message) =>
            {
                OnUnityPostMessage?.Invoke(message);
            });

        // Game bridge path
        client.initialUrl = GameBridge.GetFilePath();

        // Set up engine
        var engineConfig = ScriptableObject.CreateInstance<EngineConfiguration>();
        engineConfig.engineAppName = "UnityWebBrowser.Engine.Cef";
        engineConfig.engineFiles = new Engine.EnginePlatformFiles[]
        {
            new()
            {
                platform = Platform.Windows64,
                engineBaseAppLocation = "",
                engineRuntimeLocation = "UWB/"
#if UNITY_EDITOR
                ,
                engineEditorLocation = "Packages/dev.voltstro.unitywebbrowser.engine.cef.win.x64/Engine~/"
#endif
            }
        };
        client.engine = engineConfig;

        // Find available ports
        var tcpCommunicationLayer = ScriptableObject.CreateInstance<TCPCommunicationLayer>();
        var rnd = new System.Random();
        do
        {
            tcpCommunicationLayer.inPort = rnd.Next(1024, 65353);
            tcpCommunicationLayer.outPort = tcpCommunicationLayer.inPort + 1;
        }
        while (!CheckAvailableServerPort(tcpCommunicationLayer.inPort) || !CheckAvailableServerPort(tcpCommunicationLayer.outPort));

        client.communicationLayer = tcpCommunicationLayer;

        // Start browser client
        client.Init();

        // Wait for UWB to be connected
        await WaitForClientConnected(client);

        _webBrowserClient = client;
    }

    private static UniTask WaitForClientConnected(WebBrowserClient webBrowserClient)
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

    private static bool CheckAvailableServerPort(int port)
    {
        var tcpConnInfoArray = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        return tcpConnInfoArray.All(endpoint => endpoint.Port != port);
    }

    public void LoadUrl(string url)
    {
        _webBrowserClient?.LoadUrl(url);
    }

    public void ExecuteJavaScript(string js)
    {
        _webBrowserClient?.ExecuteJs(js);
    }

    public string GetPostMessageApiCall()
    {
        return ""; // Not required as we are using UWB RegisterJsMethod
    }

    public void Dispose()
    {
        if (_webBrowserClient?.HasDisposed == true) return;

        _webBrowserClient?.Dispose();
    }
}

#endif