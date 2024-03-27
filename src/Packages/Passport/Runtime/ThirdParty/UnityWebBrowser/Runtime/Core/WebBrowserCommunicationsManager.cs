#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;
using UnityEngine.Scripting;
using VoltRpc.Communication;
using VoltstroStudios.UnityWebBrowser.Logging;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltstroStudios.UnityWebBrowser.Shared.Events;
using VoltstroStudios.UnityWebBrowser.Shared.ReadWriters;

namespace VoltstroStudios.UnityWebBrowser.Core
{
    /// <summary>
    ///     Handles the RPC methods and two-way communication between the UWB engine and Unity
    /// </summary>
    [Preserve]
    internal class WebBrowserCommunicationsManager : IEngineControls, IClientControls, IDisposable
    {
        private static ProfilerMarker sendEventMarker = new("UWB.SendEvent");
        public readonly WebBrowserClient client;

        private readonly IEngineControls engineProxy;
        public readonly Client ipcClient;

        private readonly Host ipcHost;

        public readonly IWebBrowserLogger logger;

        private readonly object threadLock;
        private readonly SynchronizationContext unityThread;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        ///     Creates a new <see cref="WebBrowserCommunicationsManager" /> instance
        /// </summary>
        /// <param name="browserClient"></param>
        public WebBrowserCommunicationsManager(WebBrowserClient browserClient, CancellationTokenSource cancellationTokenSource)
        {
            threadLock = new object();
            unityThread = SynchronizationContext.Current;

            logger = browserClient.logger;
            client = browserClient;

            ipcClient = browserClient.communicationLayer.CreateClient();
            ipcHost = browserClient.communicationLayer.CreateHost();

            ReadWriterUtils.AddBaseTypeReadWriters(ipcHost.TypeReaderWriterManager);
            ipcHost.AddService<IClientControls>(this);

            ReadWriterUtils.AddBaseTypeReadWriters(ipcClient.TypeReaderWriterManager);

            ipcClient.AddService<IEngineControls>();
            engineProxy = new EngineControls(ipcClient);
            this.cancellationTokenSource = cancellationTokenSource;
        }

        /// <summary>
        ///     Is our client connected to the UWB engine
        /// </summary>
        public bool IsConnected => ipcClient.IsConnected;

        public void Dispose()
        {
            lock (threadLock)
            {
                ipcHost?.Dispose();
                ipcClient?.Dispose();
            }
        }

        public void Shutdown()
        {
            lock (threadLock)
            {
                engineProxy.Shutdown();
            }
        }

        public void SendKeyboardEvent(KeyboardEvent keyboardEvent)
        {
            ExecuteTask(() => engineProxy.SendKeyboardEvent(keyboardEvent));
        }

        public void SendMouseMoveEvent(MouseMoveEvent mouseMoveEvent)
        {
            ExecuteTask(() => engineProxy.SendMouseMoveEvent(mouseMoveEvent));
        }

        public void SendMouseClickEvent(MouseClickEvent mouseClickEvent)
        {
            ExecuteTask(() => engineProxy.SendMouseClickEvent(mouseClickEvent));
        }

        public void SendMouseScrollEvent(MouseScrollEvent mouseScrollEvent)
        {
            ExecuteTask(() => engineProxy.SendMouseScrollEvent(mouseScrollEvent));
        }

        public void GoForward()
        {
            ExecuteTask(() => engineProxy.GoForward());
        }

        public void GoBack()
        {
            ExecuteTask(() => engineProxy.GoBack());
        }

        public void Refresh()
        {
            ExecuteTask(() => engineProxy.Refresh());
        }

        public void LoadUrl(string url)
        {
            ExecuteTask(() => engineProxy.LoadUrl(url));
        }

        public void LoadHtml(string html)
        {
            ExecuteTask(() => engineProxy.LoadHtml(html));
        }

        public void ExecuteJs(string js)
        {
            ExecuteTask(() => engineProxy.ExecuteJs(js));
        }

        public void Connect()
        {
            ipcClient.Connect();
        }

        public void Listen()
        {
            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    ipcHost.StartListening();
                }
                catch (Exception)
                {
                    await using (UniTask.ReturnToMainThread())
                    {
                        if(!cancellationTokenSource.IsCancellationRequested)
                            throw;
                    }
                }
            }, true, cancellationTokenSource.Token);
        }

        private void ExecuteOnUnity(Action action, [CallerMemberName] string memberName = "")
        {
            unityThread.Post(_ =>
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    logger.Error($"An error occured in {memberName}! {ex}");
                }
            }, null);
        }

        internal void ExecuteTask(Action action, [CallerMemberName] string memberName = "")
        {
            if (!IsConnected)
                return;

#pragma warning disable CS4014
            UniTask.RunOnThreadPool(() =>
            {
                sendEventMarker.Begin();
                try
                {
                    lock (threadLock)
                    {
                        action.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"An error occured while executing task {memberName}! {ex}");
                }

                sendEventMarker.End();
                return UniTask.CompletedTask;
            });
#pragma warning restore CS4014
        }

        #region Client Events

        public void UrlChange(string url)
        {
            ExecuteOnUnity(() => client.InvokeUrlChanged(url));
        }

        public void UnityPostMessage(string message)
        {
            ExecuteOnUnity(() => client.InvokeOnUnityPostMessage(message));
        }

        public void LoadStart(string url)
        {
            ExecuteOnUnity(() => client.InvokeLoadStart(url));
        }

        public void LoadFinish(string url)
        {
            ExecuteOnUnity(() => client.InvokeLoadFinish(url));
        }

        public void TitleChange(string title)
        {
            ExecuteOnUnity(() => client.InvokeTitleChange(title));
        }

        public void ProgressChange(double progress)
        {
            ExecuteOnUnity(() => client.InvokeLoadProgressChange(progress));
        }

        public void Fullscreen(bool fullScreen)
        {
            ExecuteOnUnity(() => client.InvokeFullscreen(fullScreen));
        }

        public void Ready()
        {
            client.EngineReady().Forget();
        }

        #endregion
    }
}

#endif