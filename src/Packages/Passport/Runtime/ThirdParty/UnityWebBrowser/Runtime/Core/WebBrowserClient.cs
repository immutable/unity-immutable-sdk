#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Immutable.Browser.Core;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Communication;
using VoltstroStudios.UnityWebBrowser.Core.Engines;
using VoltstroStudios.UnityWebBrowser.Events;
using VoltstroStudios.UnityWebBrowser.Helper;
using VoltstroStudios.UnityWebBrowser.Logging;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltstroStudios.UnityWebBrowser.Shared.Events;
using Object = UnityEngine.Object;
using Resolution = VoltstroStudios.UnityWebBrowser.Shared.Resolution;

namespace VoltstroStudios.UnityWebBrowser.Core
{
    /// <summary>
    ///     The main object responsible for UWB.
    ///     <para>
    ///         This class handles:
    ///         <list type="bullet">
    ///             <item>UWB process setup</item>
    ///             <item>Texture setup and rendering</item>
    ///             <item>Wrapper for invoking methods on the UWB process</item>
    ///             <item>Shutdown</item>
    ///         </list>
    ///         If you need to do something with UWB, its probably here.
    ///     </para>
    /// </summary>
    [Serializable]
    public class WebBrowserClient : IWebBrowserClient, IDisposable
    {
        private const string TAG = "[Web Browser Client]";

        #region Profile Markers

        internal static ProfilerMarker markerGetPixels = new("UWB.GetPixels");
        internal static ProfilerMarker markerGetPixelsRpc = new("UWB.GetPixels.RPC");

        internal static ProfilerMarker markerLoadTextureApply = new("UWB.LoadTexture.Apply");

        #endregion

        /// <summary>
        ///     The active browser engine this instance is using
        /// </summary>
        [Header("Browser Settings")]
        [Tooltip("The active browser engine this instance is using")]
        public Engine engine;

        private const string ENGINE_APP_NAME = "UnityWebBrowser.Engine.Cef";
        public static string ENGINE_FILE_LOCATION = "Packages/com.immutable.passport/Runtime/ThirdParty/UnityWebBrowser.Engine.Cef.Win-x64/Engine/";
        private const string INITIAL_URL = "https://www.immutable.com/";

        /// <summary>
        ///     The initial URl the browser will start at
        /// </summary>
        [Tooltip("The initial URl the browser will start at")]
        public string initialUrl = INITIAL_URL;

        #region Resoltuion

        [SerializeField] private Resolution resolution = new(1920, 1080);

        #endregion

        /// <summary>
        ///     The background <see cref="UnityEngine.Color32" /> of the webpage
        /// </summary>
        [Tooltip("The background color of the webpage")]
        public Color32 backgroundColor = new(255, 255, 255, 255);

        /// <summary>
        ///     Enable or disable JavaScript
        /// </summary>
        [Tooltip("Enable or disable JavaScript")]
        public bool javascript = true;

        /// <summary>
        ///     Enable or disable the cache
        /// </summary>
        [Tooltip("Enable or disable the cache")]
        public bool cache = true;

        /// <summary>
        ///     Enable or disable WebRTC
        /// </summary>
        [Tooltip("Enable or disable WebRTC")] public bool webRtc;

        /// <summary>
        ///     Enable or disable local storage
        /// </summary>
        [Tooltip("Enable or disable local storage")]
        public bool localStorage = true;

        /// <summary>
        ///     Proxy Settings
        /// </summary>
        [Tooltip("Proxy settings")] public ProxySettings proxySettings;

        /// <summary>
        ///     Enable or disable remote debugging
        /// </summary>
        [Tooltip("Enable or disable remote debugging")]
        public bool remoteDebugging;

        /// <summary>
        ///     The port to use for remote debugging
        /// </summary>
        [Tooltip("The port to use for remote debugging")]
        [Range(1024, 65353)]
        public uint remoteDebuggingPort = 9022;

        /// <summary>
        ///     The <see cref="CommunicationLayer" /> to use
        /// </summary>
        [Header("IPC Settings")]
        [Tooltip("The communication layer to use")]
        public CommunicationLayer communicationLayer;

        /// <summary>
        ///     Timeout time for waiting for the engine to start (in milliseconds)
        /// </summary>
        [Tooltip("Timeout time for waiting for the engine to start (in milliseconds)")]
        public int engineStartupTimeout = 30000;

        /// <summary>
        ///     The log severity. Only messages of this severity level or higher will be logged
        /// </summary>
        [Tooltip("The log severity. Only messages of this severity level or higher will be logged")]
        public LogSeverity logSeverity = LogSeverity.Info;

        /// <summary>
        ///     Texture that the browser will paint to
        /// </summary>
        public Texture2D BrowserTexture { get; private set; }

        /// <summary>
        ///     Are we connected to the UW engine process
        /// </summary>
        public bool IsConnected => communicationsManager is { IsConnected: true };

        /// <summary>
        ///     The UWB engine has signaled that it is ready
        /// </summary>
        public bool ReadySignalReceived { get; internal set; }

        /// <summary>
        ///     Has UWB initialized
        /// </summary>
        public bool HasInitialized { get; internal set; }

        /// <summary>
        ///     Internal FPS of pixels communication between Unity and the Engine
        /// </summary>
        public int FPS { get; private set; }

        #region Log Path

        private FileInfo logPath;

        /// <summary>
        ///     The path that UWB engine will log to
        /// </summary>
        /// <exception cref="UwbIsConnectedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileInfo LogPath
        {
            get => logPath;
            set
            {
                if (IsConnected)
                    throw new UwbIsConnectedException(
                        "You cannot change the log path once the browser engine is connected");

                logPath = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        #endregion

        #region Cache Path

        private FileInfo cachePath;

        /// <summary>
        ///     The path to the cache
        /// </summary>
        /// <exception cref="UwbIsConnectedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileInfo CachePath
        {
            get => cachePath;
            set
            {
                if (IsConnected)
                    throw new UwbIsConnectedException(
                        "You cannot change the cache path once the browser engine is connected");

                if (!cache)
                    throw new ArgumentException("The cache is disabled!");

                cachePath = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        #endregion

        #region Logger

        public ProcessLogHandler processLogHandler;

        /// <summary>
        ///     Internal usage of <see cref="IWebBrowserLogger" />
        /// </summary>
        internal IWebBrowserLogger logger = new DefaultUnityWebBrowserLogger();

        /// <summary>
        ///     Gets the <see cref="IWebBrowserLogger" /> to use for logging
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public IWebBrowserLogger Logger
        {
            get => logger;
            set => logger = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        private EngineProcess engineProcess;
        private WebBrowserCommunicationsManager communicationsManager;
        private CancellationTokenSource cancellationSource;

        private NativeArray<byte> textureData;
        internal NativeArray<byte> nextTextureData;

        public WebBrowserClient()
        {
#if (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            UnityEngine.Debug.LogWarning("Native Android and iOS WebViews cannot run in the Editor, so the Windows WebView is currently used to save your development time." + 
                " Testing your game on an actual device or emulator is recommended to ensure proper functionality.");
#endif
        }

        /// <summary>
        ///     Inits the browser client
        /// </summary>
        /// <exception cref="FileNotFoundException"></exception>
        public async UniTask Init(int engineStartupTimeout = 30000)
        {
            this.engineStartupTimeout = engineStartupTimeout;
            UnityEngine.Debug.Log($"{TAG} Engine startup timeout: {engineStartupTimeout}");

            // Get the path to the Windows UWB process
            EngineConfiguration engineConfiguration = new EngineConfiguration();
            engineConfiguration.engineAppName = ENGINE_APP_NAME;
#if UNITY_EDITOR
            Engine.EnginePlatformFiles[] engineFiles = new Engine.EnginePlatformFiles[]
            {
                new Engine.EnginePlatformFiles()
                {
                    platform = Platform.Windows64,
                    engineFileLocation = ENGINE_FILE_LOCATION
                }
            };
            engineConfiguration.engineFiles = engineFiles;
#endif
            engine = engineConfiguration;

            string browserEnginePath = WebBrowserUtils.GetBrowserEngineProcessPath(engine);
            logger.Debug($"Starting browser engine process from '{browserEnginePath}'...");

            if (!File.Exists(browserEnginePath))
            {
                logger.Error("The engine process could not be found!");
                throw new FileNotFoundException("The engine process could not be found!");
            }

            if (communicationLayer == null)
            {
                communicationLayer = new TCPCommunicationLayer();
            }

            //Check communication layer
            if (communicationLayer.IsInUse)
                throw new InitializationException("The communication layer is already in use!");
            communicationLayer.IsInUse = true;

            //Setup texture
            BrowserTexture = new Texture2D((int)resolution.Width, (int)resolution.Height, TextureFormat.BGRA32, false,
                false);
            WebBrowserUtils.SetAllTextureColorToOne(BrowserTexture, backgroundColor);

            textureData = BrowserTexture.GetRawTextureData<byte>();
            nextTextureData = new NativeArray<byte>(textureData.ToArray(), Allocator.Persistent);

            string browserEngineMainDir = WebBrowserUtils.GetAdditionFilesDirectory();

            //Start to build our arguments
            WebBrowserArgsBuilder argsBuilder = new();

            //Initial URL
            initialUrl = GameBridge.GetFilePath();
            argsBuilder.AppendArgument("initial-url", initialUrl, true);

            //Width & Height
            argsBuilder.AppendArgument("width", resolution.Width);
            argsBuilder.AppendArgument("height", resolution.Height);

            //Javascript
            argsBuilder.AppendArgument("javascript", javascript);

            //Background color
            argsBuilder.AppendArgument("background-color", WebBrowserUtils.ColorToHex(backgroundColor));

            //Logging
            LogPath ??= new FileInfo($"{browserEngineMainDir}/{engine.GetEngineExecutableName()}.log");
            argsBuilder.AppendArgument("log-path", LogPath.FullName, true);
            argsBuilder.AppendArgument("log-severity", logSeverity);

            //IPC settings
            communicationLayer.GetIpcSettings(out object outLocation, out object inLocation,
                out string assemblyLocation);
            if (assemblyLocation != null)
            {
                if (!File.Exists(assemblyLocation))
                {
                    logger.Error("Failed to find provided communication layer assembly!");
                    throw new FileNotFoundException("Failed to find provided communication layer assembly!");
                }

                argsBuilder.AppendArgument("comms-layer-path", assemblyLocation, true);
                logger.Debug($"Using communication layer assembly at '{assemblyLocation}'.");
            }

            argsBuilder.AppendArgument("in-location", inLocation, true);
            argsBuilder.AppendArgument("out-location", outLocation, true);

            //If we have a cache, set the cache path
            if (cache)
            {
                cachePath ??= new FileInfo($"{browserEngineMainDir}/UWBCache");
                argsBuilder.AppendArgument("cache-path", cachePath.FullName, true);
            }

            //Setup web RTC
            if (webRtc)
                argsBuilder.AppendArgument("web-rtc", webRtc);

            argsBuilder.AppendArgument("local-storage", localStorage);

            //Setup remote debugging
            if (remoteDebugging)
                argsBuilder.AppendArgument("remote-debugging", remoteDebuggingPort);

            //Setup proxy
            argsBuilder.AppendArgument("proxy-server", proxySettings.ProxyServer);
            if (!string.IsNullOrWhiteSpace(proxySettings.Username))
                argsBuilder.AppendArgument("proxy-username", proxySettings.Username, true);

            if (!string.IsNullOrWhiteSpace(proxySettings.Password))
                argsBuilder.AppendArgument("proxy-password", proxySettings.Password, true);

            //Make sure not to include this, its for testing
#if UWB_ENGINE_PRJ //Define for backup, cause I am dumb as fuck and gonna accidentally include this in a release build one day 
            //argsBuilder.AppendArgument("start-delay", 2000);
#endif

            //Final built arguments
            string arguments = argsBuilder.ToString();

            //Setup communication manager
            cancellationSource = new CancellationTokenSource();
            communicationsManager = new WebBrowserCommunicationsManager(this, cancellationSource);
            communicationsManager.Listen();

#if UNITY_EDITOR
            //Install reload events handler
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif

            //Mark has initialized and invoke event
            HasInitialized = true;

            try
            {
                //Start the engine process
                await UniTask.Create(() =>
                    StartEngineProcess(arguments))
                    .ContinueWith(() => WaitForEngineReadyTask(cancellationSource.Token));
            }
            catch (Exception ex)
            {
                // Need to catch and then rethrow otherwise the exception
                // goes to UniTaskScheduler.UnobservedTaskException
                throw ex;
            }
        }

        #region Starting

        private UniTask StartEngineProcess(string engineProcessArguments)
        {
            try
            {
                processLogHandler = new ProcessLogHandler(this);
                engineProcess = new EngineProcess(engine, logger);
                engineProcess.StartProcess(engineProcessArguments, processLogHandler.HandleOutputProcessLog, processLogHandler.HandleErrorProcessLog);
            }
            catch (Exception ex)
            {
                logger.Error($"An error occured while setting up the engine process! {ex}");
                throw;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        ///     Will wait for <see cref="ReadySignalReceived" /> to be true
        /// </summary>
        internal async UniTask WaitForEngineReadyTask(CancellationToken cancellationToken)
        {
            try
            {
                //Wait until we get a ready signal, or timeout
                await UniTask.WaitUntil(() =>
                        ReadySignalReceived, cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromMilliseconds(engineStartupTimeout));
            }
            catch (TimeoutException ex)
            {
                logger.Error(engineProcess.HasExited
                    ? $"The engine did not get ready within engine startup timeout! The engine process is not even running! Exit code: {engineProcess.ExitCode}."
                    : "The engine did not get ready within engine startup timeout!");
                await using (UniTask.ReturnToMainThread())
                {
                    Dispose();
                }
                throw ex;
            }
            catch (OperationCanceledException)
            {
                //Token probs got canceled
            }
            catch (Exception ex)
            {
                logger.Error($"An unknown error occured while waiting for engine to get ready! {ex}");
                await using (UniTask.ReturnToMainThread())
                {
                    Dispose();
                }
                throw ex;
            }
        }

        /// <summary>
        ///     Called when the engine sends the ready signal
        /// </summary>
        internal async UniTaskVoid EngineReady()
        {
            ReadySignalReceived = true;

            try
            {
                logger.Debug("UWB startup success, connecting...");
                communicationsManager.Connect();
            }
            catch (Exception ex)
            {
                logger.Error($"An error occured while waiting to connect to the UWB engine process! {ex}");
                await using (UniTask.ReturnToMainThread())
                {
                    Dispose();
                }
            }
        }

        #endregion

        #region Main Loop

        /// <summary>
        ///     Loads the pixel data into the <see cref="BrowserTexture" />
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LoadTextureData()
        {
            if (!IsConnected)
                return;

            Texture2D texture = BrowserTexture;

            markerLoadTextureApply.Begin();
            texture.Apply(false);
            markerLoadTextureApply.End();
        }

        #region FPS

        private int frames;
        private float lastUpdateTime;

        /// <summary>
        ///     Updates FPS values
        ///     <para>Normal usage shouldn't require invoking this</para>
        /// </summary>
        public void UpdateFps()
        {
            float currentTime = Time.time;
            if (currentTime - lastUpdateTime > 1)
            {
                lastUpdateTime = currentTime;
                FPS = frames;
                frames = 0;
            }
        }

        #endregion

        #endregion

        #region Browser Events

        /// <summary>
        ///     Invoked when the url changes
        /// </summary>
        public event OnUrlChangeDelegate OnUrlChanged;

        internal void InvokeUrlChanged(string url)
        {
            OnUrlChanged?.Invoke(url);
        }

        /// <summary>
        ///     Invoked when JS calls back to the engine
        /// </summary>
        public event OnUnityPostMessageDelegate OnUnityPostMessage;

        internal void InvokeOnUnityPostMessage(string message)
        {
            OnUnityPostMessage?.Invoke(message);
        }

        /// <summary>
        ///     Invoked when the page starts to load
        /// </summary>
        public event OnLoadStartDelegate OnLoadStart;

        internal void InvokeLoadStart(string url)
        {
            OnLoadStart?.Invoke(url);
        }

        /// <summary>
        ///     Invoked when the page finishes loading
        /// </summary>
        public event OnLoadFinishDelegate OnLoadFinish;

        internal void InvokeLoadFinish(string url)
        {
            OnLoadFinish?.Invoke(url);
        }

        /// <summary>
        ///     Invoked when the title changes
        /// </summary>
        public event OnTitleChange OnTitleChange;

        internal void InvokeTitleChange(string title)
        {
            OnTitleChange?.Invoke(title);
        }

        /// <summary>
        ///     Invoked when the loading progress changes
        ///     <para>Progress goes from 0 to 1</para>
        /// </summary>
        public event OnLoadingProgressChange OnLoadProgressChange;

        internal void InvokeLoadProgressChange(double progress)
        {
            OnLoadProgressChange?.Invoke(progress);
        }

        /// <summary>
        ///     Invoked when the browser goes in or out of fullscreen
        /// </summary>
        public event OnFullscreenChange OnFullscreen;

        internal void InvokeFullscreen(bool fullscreen)
        {
            OnFullscreen?.Invoke(fullscreen);
        }

        #endregion

        #region Browser Controls

        /// <summary>
        ///     Sends a keyboard event
        /// </summary>
        /// <param name="keysDown"></param>
        /// <param name="keysUp"></param>
        /// <param name="chars"></param>
        public void SendKeyboardControls(WindowsKey[] keysDown, WindowsKey[] keysUp, char[] chars)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.SendKeyboardEvent(new KeyboardEvent
            {
                KeysDown = keysDown,
                KeysUp = keysUp,
                Chars = chars
            });
        }

        /// <summary>
        ///     Sends a mouse event
        /// </summary>
        /// <param name="mousePos"></param>
        public void SendMouseMove(Vector2 mousePos)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.SendMouseMoveEvent(new MouseMoveEvent
            {
                MouseX = (int)mousePos.x,
                MouseY = (int)mousePos.y
            });
        }

        /// <summary>
        ///     Sends a mouse click event
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="clickCount"></param>
        /// <param name="clickType"></param>
        /// <param name="eventType"></param>
        public void SendMouseClick(Vector2 mousePos, int clickCount, MouseClickType clickType,
            MouseEventType eventType)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.SendMouseClickEvent(new MouseClickEvent
            {
                MouseX = (int)mousePos.x,
                MouseY = (int)mousePos.y,
                MouseClickCount = clickCount,
                MouseClickType = clickType,
                MouseEventType = eventType
            });
        }

        /// <summary>
        ///     Sends a mouse scroll event
        /// </summary>
        /// <param name="mousePos"></param>
        /// <param name="mouseScroll"></param>
        public void SendMouseScroll(Vector2 mousePos, int mouseScroll)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.SendMouseScrollEvent(new MouseScrollEvent
            {
                MouseX = (int)mousePos.x,
                MouseY = (int)mousePos.y,
                MouseScroll = mouseScroll
            });
        }

        /// <summary>
        ///     Tells the browser to load a URL
        /// </summary>
        /// <param name="url"></param>
        public void LoadUrl(string url)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.LoadUrl(url);
        }

        /// <summary>
        ///     Tells the browser to go forward
        /// </summary>
        public void GoForward()
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.GoForward();
        }

        /// <summary>
        ///     Tells the browser to go back
        /// </summary>
        public void GoBack()
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.GoBack();
        }

        /// <summary>
        ///     Refreshes the browser
        /// </summary>
        public void Refresh()
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.Refresh();
        }

        /// <summary>
        ///     Makes the browser load html
        /// </summary>
        /// <param name="html"></param>
        public void LoadHtml(string html)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.LoadHtml(html);
        }

        /// <summary>
        ///     Executes JS in the browser
        /// </summary>
        /// <param name="js"></param>
        public void ExecuteJs(string js)
        {
            CheckIfIsReadyAndConnected();

            communicationsManager.ExecuteJs(js);
        }

        public void LaunchAuthURL(string url, string? redirectUri)
        {
            Application.OpenURL(url);
        }

        [DebuggerStepThrough]
        private void CheckIfIsReadyAndConnected()
        {
            if (!ReadySignalReceived)
                throw new UwbIsNotReadyException("UWB is not currently ready!");

            if (!IsConnected)
                throw new UwbIsNotConnectedException("UWB is not currently connected!");
        }

        #endregion

        #region Destroying

#if UNITY_EDITOR
        private void OnBeforeAssemblyReload()
        {
            if (HasInitialized && !HasDisposed)
            {
                logger.Warn("UWB is shutting down due to incoming domain reload. UWB does not support domain reloading while running.");
                Dispose();
            }
        }
#endif

#if !UNITY_EDITOR
        ~WebBrowserClient()
        {
            ReleaseResources();
        }
#endif

        /// <summary>
        ///     Has this object been disposed
        /// </summary>
        public bool HasDisposed { get; private set; }

        /// <summary>
        ///     Destroys this <see cref="WebBrowserClient" /> instance
        /// </summary>
        public void Dispose()
        {
            if (HasDisposed)
                return;

            ReleaseResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseResources()
        {
            if (HasDisposed)
                return;

            HasDisposed = true;
            logger.Debug("UWB shutdown...");

            cancellationSource?.Cancel();

            //Destroy textures
            if (BrowserTexture != null)
                Object.Destroy(BrowserTexture);

            //Engine shutdown
            try
            {
                if (ReadySignalReceived && IsConnected)
                    communicationsManager.Shutdown();
            }
            catch (Exception ex)
            {
                logger.Error($"Some error occured while shutting down the engine! {ex}");
            }

            //Communication manager destruction
            try
            {
                communicationsManager?.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error($"Some error occured while destroying the communications manager! {ex}");
            }

            //We are no longer using our communication manager
            if (communicationLayer != null)
                communicationLayer.IsInUse = false;

            //Kill the process if we haven't already
            if (engineProcess != null)
            {
                if (!engineProcess.HasExited)
                    engineProcess.KillProcess();
                    
                engineProcess.Dispose();
                engineProcess = null;
            }

#if UNITY_EDITOR
            //Install reload events handler
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
#endif

            //Dispose of buffers
            if (nextTextureData.IsCreated)
                nextTextureData.Dispose();
        }

        #endregion
    }
}

#endif