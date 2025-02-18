using System.Collections.Generic;
using System;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
#if !IMMUTABLE_CUSTOM_BROWSER
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Shared;
#endif
#elif (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
using Immutable.Browser.Gree;
#endif
using Immutable.Passport.Event;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Immutable.Passport.Core.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Immutable.Passport
{

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
    public class Passport : MonoBehaviour
#else
    public class Passport
#endif
    {
        private const string TAG = "[Passport]";

        public static Passport? Instance { get; private set; }
        private PassportImpl? passportImpl;
        public string environment { get; private set; }

        private IWebBrowserClient webBrowserClient;

        // Keeps track of the latest received deeplink
        private static string? deeplink;
        private static bool readySignalReceived;

        /// <summary>
        /// Passport auth events
        /// </summary>
        /// <seealso cref="Immutable.Passport.Event.PassportAuthEvent" />
        public event OnAuthEventDelegate OnAuthEvent;

        /// <summary>
        /// The log level for the SDK.
        /// </summary>
        /// <remarks>
        /// The log level determines which messages are recorded based on their severity. The default value is <see cref="LogLevel.Info"/>.
        /// <para>
        /// See <see cref="Immutable.Passport.Core.Logging.LogLevel"/> for valid log levels and their meanings.
        /// </para>
        /// </remarks>
        public static LogLevel LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                PassportLogger.CurrentLogLevel = _logLevel;

#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))
                SetDefaultWindowsBrowserLogLevel();
#endif
            }
        }

        private static LogLevel _logLevel = LogLevel.Info;

        private Passport()
        {
            // Handle clean-up tasks when the application is quitting
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            Application.quitting += OnQuit;
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Handle deeplinks for iOS and macOS
            Application.deepLinkActivated += OnDeepLinkActivated;

            // Check if there is a deep link URL provided on application start
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Handle the deep link if provided during a cold start
                OnDeepLinkActivated(Application.absoluteURL);
            }
#endif

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        /// <summary>
        /// Initialises Passport with the specified parameters. 
        /// This sets up the Passport instance, configures the web browser, and waits for the ready signal.
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="environment">The environment to connect to</param>
        /// <param name="redirectUri">(Android, iOS, and macOS only) The URL where the browser will redirect after successful authentication.</param>
        /// <param name="logoutRedirectUri">The URL where the browser will redirect after logout is complete.</param>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout duration in milliseconds to wait for the default Windows browser engine to start.</param>
        /// <param name="webBrowserClient">(Windows only) Custom Windows browser to use instead of the default browser in the SDK.</param>
        public static UniTask<Passport> Init(
            string clientId,
            string environment,
            string redirectUri = null,
            string logoutRedirectUri = null
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            , int engineStartupTimeoutMs = 60000,
            IWindowsWebBrowserClient windowsWebBrowserClient = null
#endif
        )
        {
            if (Instance == null)
            {
                PassportLogger.Info($"{TAG} Initialising Passport...");

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                var obj = new GameObject("Passport");
                Instance = obj.AddComponent<Passport>();
                DontDestroyOnLoad(obj);
#else
                Instance = new Passport();
#endif
                Instance.environment = environment;

                // Start initialisation process
                return Instance.Initialise(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                        engineStartupTimeoutMs, windowsWebBrowserClient
#endif
                    )
                    .ContinueWith(async () =>
                    {
                        // Wait for the ready signal
                        PassportLogger.Info($"{TAG} Waiting for ready signal...");
                        await UniTask.WaitUntil(() => readySignalReceived == true);
                    })
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived)
                        {
                            // Initialise Passport with provided parameters
                            await Instance.GetPassportImpl().Init(clientId, environment, redirectUri, logoutRedirectUri, deeplink);
                            return Instance;
                        }
                        else
                        {
                            PassportLogger.Error($"{TAG} Failed to initialise Passport");
                            throw new PassportException("Failed to initialise Passport", PassportErrorType.INITALISATION_ERROR);
                        }
                    });
            }
            else
            {
                // Return the existing instance if already initialised
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
            }
        }

        /// <summary>
        /// Initialises the appropriate web browser and sets up browser communication.
        /// </summary>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout duration in milliseconds to wait for the default Windows browser engine to start.</param>
        /// <param name="webBrowserClient">(Windows only) Custom Windows browser to use instead of the default browser in the SDK.</param>
        private async UniTask Initialise(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            int engineStartupTimeoutMs, IWindowsWebBrowserClient windowsWebBrowserClient
#endif
        )
        {
            try
            {
                // Initialise the web browser client
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                if (windowsWebBrowserClient != null)
                {
                    // Use the provided custom Windows browser client
                    this.webBrowserClient = new WindowsWebBrowserClientAdapter(windowsWebBrowserClient);
                    await ((WindowsWebBrowserClientAdapter)this.webBrowserClient).Init();
                }
                else
                {
#if IMMUTABLE_CUSTOM_BROWSER
                    throw new PassportException("When 'IMMUTABLE_CUSTOM_BROWSER' is defined in Scripting Define Symbols, " + 
                        " 'windowsWebBrowserClient' must not be null.");
#else
                    webBrowserClient = gameObject.AddComponent<UwbWebView>();
                    await ((UwbWebView)webBrowserClient).Init(engineStartupTimeoutMs);
                    readySignalReceived = true;
#endif
                }
#elif (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
                // Initialise default browser client for Android, iOS, and macOS
                webBrowserClient = new GreeBrowserClient();
#else
                throw new PassportException("Platform not supported");
#endif

                // Set up browser communication
                BrowserCommunicationsManager communicationsManager = new BrowserCommunicationsManager(webBrowserClient);

#if UNITY_WEBGL
                readySignalReceived = true;
#else
                // Mark ready when browser is initialised and game bridge file is loaded
                communicationsManager.OnReady += () => readySignalReceived = true;
#endif
                // Set up Passport implementation
                passportImpl = new PassportImpl(communicationsManager);
                // Subscribe to Passport authentication events
                passportImpl.OnAuthEvent += OnPassportAuthEvent;
            }
            catch (Exception ex)
            {
                // Reset everything on error
                readySignalReceived = false;
                Instance = null;
                throw ex;
            }
        }

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
#endif

        /// <summary>
        /// Sets the timeout time for waiting for each call to respond (in milliseconds).
        /// This only applies to functions that use the browser communications manager.
        /// </summary>
        public void SetCallTimeout(int ms)
        {
            GetPassportImpl().communicationsManager.SetCallTimeout(ms);
        }

        /// <summary>
        /// Logs the user into Passport via device code auth. This will open the user's default browser and take them through Passport login.
        /// <param name="useCachedSession">If true, the saved access token or refresh token will be used to log the user in. If this fails, it will not fallback to device code auth.</param>
        /// <param name="timeoutMs">(Optional) The maximum time, in milliseconds, the function is allowed to take before a TimeoutException is thrown. If not set, the function will wait indefinitely.</param>
        /// </summary>
        /// <returns>
        /// Returns true if login is successful, otherwise false.
        /// </returns>
        public async UniTask<bool> Login(bool useCachedSession = false, long? timeoutMs = null)
        {
            return await GetPassportImpl().Login(useCachedSession, timeoutMs);
        }

        /// <summary>
        /// Logs the user into Passport via device code auth and sets up the Immutable X provider. This will open the user's
        /// default browser and take them through Passport login.
        /// <param name="useCachedSession">If true, the saved access token or refresh token will be used to connect the user. If this fails, it will not fallback to device code auth.</param>
        /// <param name="timeoutMs">(Optional) The maximum time, in milliseconds, the function is allowed to take before a TimeoutException is thrown. If not set, the function will wait indefinitely.</param>
        /// </summary>
        public async UniTask<bool> ConnectImx(bool useCachedSession = false, long? timeoutMs = null)
        {
            return await GetPassportImpl().ConnectImx(useCachedSession, timeoutMs);
        }

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        /// <summary>
        /// Connects the user into Passport via PKCE auth.
        /// </summary>
        public async UniTask LoginPKCE()
        {
            await GetPassportImpl().LoginPKCE();
        }

        /// <summary>
        /// Connects the user into Passport via PKCE auth and sets up the Immutable X provider.
        ///
        /// The user does not need to go through this flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// </summary>
        public async UniTask<bool> ConnectImxPKCE()
        {
            return await GetPassportImpl().ConnectImxPKCE();
        }
#endif

        /// <summary>
        /// Gets the wallet address of the logged in user.
        /// <returns>
        /// The wallet address
        /// </returns>
        /// </summary>
        public async UniTask<string> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        /// <summary>
        /// Logs the user out of Passport and removes any stored credentials.
        /// Recommended to use when logging in using device auth flow - ConnectImx()
        /// </summary>
        /// <param name="hardLogout">If false, the user will not be logged out of Passport in the browser. The default is true.</param>
        public async UniTask Logout(bool hardLogout = true)
        {
            await GetPassportImpl().Logout(hardLogout);
        }

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        /// <summary>
        /// Logs the user out of Passport and removes any stored credentials.
        /// Recommended to use when logging in using PKCE flow - ConnectImxPKCE()
        /// </summary>
        /// <param name="hardLogout">If false, the user will not be logged out of Passport in the browser. The default is true.</param>
        public async UniTask LogoutPKCE(bool hardLogout = true)
        {
            await GetPassportImpl().LogoutPKCE(hardLogout);
        }
#endif

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// <returns>
        /// True if there are crendentials saved
        /// </returns>
        /// </summary>
        public UniTask<bool> HasCredentialsSaved()
        {
            return GetPassportImpl().HasCredentialsSaved();
        }

        /// <summary>
        /// Checks if the user is registered off-chain
        /// <returns>
        /// True if the user is registered with Immutable X, false otherwise
        /// </returns>
        /// </summary>
        public async UniTask<bool> IsRegisteredOffchain()
        {
            return await GetPassportImpl().IsRegisteredOffchain();
        }

        /// <summary>
        /// Registers the user to Immutable X if they are not already registered
        /// </summary>
        public async UniTask<RegisterUserResponse> RegisterOffchain()
        {
            return await GetPassportImpl().RegisterOffchain();
        }

        /// <summary>
        /// Retrieves the email address of the user whose credentials are currently stored.
        /// <returns>
        /// The email address
        /// </returns>
        /// </summary>
        public async UniTask<string> GetEmail()
        {
            string email = await GetPassportImpl().GetEmail();
            return email;
        }

        /// <summary>
        /// Retrieves the Passport ID of the user whose credentials are currently stored.
        /// <returns>
        /// The Passport ID
        /// </returns>
        /// </summary>
        public async UniTask<string> GetPassportId()
        {
            string passportId = await GetPassportImpl().GetPassportId();
            return passportId;
        }

        /// <summary>
        /// Gets the currently saved access token without verifying its validity.
        /// <returns>
        /// The access token
        /// </returns>
        /// </summary>
        public UniTask<string> GetAccessToken()
        {
            return GetPassportImpl().GetAccessToken();
        }

        /// <summary>
        /// Gets the currently saved ID token without verifying its validity.
        /// <returns>
        /// The ID token
        /// </returns>
        /// </summary>
        public UniTask<string> GetIdToken()
        {
            return GetPassportImpl().GetIdToken();
        }

        /// <summary>
        /// Gets the list of external wallets the user has linked to their Passport account via the 
        /// <see href="https://passport.immutable.com/">Dashboard</see>.
        /// <returns>
        /// Linked addresses
        /// </returns>
        /// </summary>
        public async UniTask<List<string>> GetLinkedAddresses()
        {
            return await GetPassportImpl().GetLinkedAddresses();
        }

        /// <summary>
        /// Create a new transfer request with the given unsigned transfer request.
        /// <returns>
        /// The transfer response if successful
        /// </returns>
        /// </summary>
        public async UniTask<CreateTransferResponseV1> ImxTransfer(UnsignedTransferRequest request)
        {
            CreateTransferResponseV1 response = await GetPassportImpl().ImxTransfer(request);
            return response;
        }

        /// <summary>
        /// Create a new batch nft transfer request with the given transfer details.
        /// <returns>
        /// The transfer response if successful
        /// </returns>
        /// </summary>
        public async UniTask<CreateBatchTransferResponse> ImxBatchNftTransfer(NftTransferDetails[] details)
        {
            CreateBatchTransferResponse response = await GetPassportImpl().ImxBatchNftTransfer(details);
            return response;
        }

        /// <summary>
        /// Instantiates the zkEVM provider
        /// </summary>
        /// <returns></returns>
        public async UniTask ConnectEvm()
        {
            await GetPassportImpl().ConnectEvm();
        }

        /// <summary>
        /// Sends a transaction to the network and signs it using the logged-in Passport account.
        /// <returns>
        /// The transaction hash, or the zero hash if the transaction is not yet available.
        /// </returns>
        /// </summary>
        public async UniTask<string> ZkEvmSendTransaction(TransactionRequest request)
        {
            return await GetPassportImpl().ZkEvmSendTransaction(request);
        }

        /// <summary>
        /// Similar to <code>ZkEvmSendTransaction</code>. Sends a transaction to the network, signs it using the logged-in Passport account, and waits for the transaction to be included in a block.
        /// <returns>
        /// The receipt of the transaction or null if it is still processing.
        /// </returns>
        /// </summary>
        public async UniTask<TransactionReceiptResponse> ZkEvmSendTransactionWithConfirmation(TransactionRequest request)
        {
            return await GetPassportImpl().ZkEvmSendTransactionWithConfirmation(request);
        }

        /// <summary>
        /// Retrieves the transaction information of a given transaction hash. This function uses the Ethereum JSON-RPC <c>eth_getTransactionReceipt</c> method.
        /// <returns>
        /// The receipt of the transaction or null if it is still processing.
        /// </returns>
        /// </summary>
        public async UniTask<TransactionReceiptResponse> ZkEvmGetTransactionReceipt(string hash)
        {
            return await GetPassportImpl().ZkEvmGetTransactionReceipt(hash);
        }

        /// <summary>
        /// Signs the EIP-712 structured message in JSON string format using the logged-in Passport account.
        /// See <see href="https://eips.ethereum.org/EIPS/eip-712">EIP-712</see>.
        /// <param name="payload">The EIP-712 structured data in JSON string format</param>
        /// <returns>
        /// The signed payload string.
        /// </returns>
        /// </summary>
        public async UniTask<string> ZkEvmSignTypedDataV4(string payload)
        {
            return await GetPassportImpl().ZkEvmSignTypedDataV4(payload);
        }

        /// <summary>
        /// Returns a list of addresses owned by the user
        /// <returns>
        /// Addresses owned by the user
        /// </returns>
        /// </summary>
        public async UniTask<List<string>> ZkEvmRequestAccounts()
        {
            return await GetPassportImpl().ZkEvmRequestAccounts();
        }

        /// <summary>
        /// Returns the balance of the account of given address.
        /// </summary>
        /// <param name="address">Address to check for balance</param>
        /// <param name="blockNumberOrTag">Integer block number, or the string "latest", "earliest" or "pending"</param>
        /// <returns>
        /// The balance in wei
        /// </returns>
        public async UniTask<string> ZkEvmGetBalance(string address, string blockNumberOrTag = "latest")
        {
            return await GetPassportImpl().ZkEvmGetBalance(address, blockNumberOrTag);
        }

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        /// <summary>
        /// Clears the underlying WebView resource cache
        /// Android: Note that the cache is per-application, so this will clear the cache for all WebViews used.
        /// <param name="includeDiskFiles">if false, only the RAM/in-memory cache is cleared</param>
        /// </summary>
        /// <returns></returns>
        public void ClearCache(bool includeDiskFiles)
        {
            GetPassportImpl().ClearCache(includeDiskFiles);
        }

        /// <summary>
        /// Clears all the underlying WebView storage currently being used by the JavaScript storage APIs. 
        /// This includes Web SQL Database and the HTML5 Web Storage APIs.
        /// </summary>
        /// <returns></returns>
        public void ClearStorage()
        {
            GetPassportImpl().ClearStorage();
        }
#endif

#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))
        /// <summary>
        /// Updates the log severity for the default Windows browser based on the current SDK log level.
        /// </summary>
        private static void SetDefaultWindowsBrowserLogLevel()
        {
            if (Instance?.webBrowserClient is WebBrowserClient browserClient)
            {
                browserClient.logSeverity = _logLevel switch
                {
                    LogLevel.Debug => LogSeverity.Debug,
                    LogLevel.Warn => LogSeverity.Warn,
                    LogLevel.Error => LogSeverity.Error,
                    _ => LogSeverity.Info
                };
            }
        }
#endif

        private PassportImpl GetPassportImpl()
        {
            if (passportImpl != null)
            {
                return passportImpl;
            }
            throw new PassportException("Passport not initialised");
        }

        private void OnDeepLinkActivated(string url)
        {
            deeplink = url;

            if (passportImpl != null)
            {
                GetPassportImpl().OnDeepLinkActivated(url);
            }
        }

        private void OnPassportAuthEvent(PassportAuthEvent authEvent)
        {
            if (OnAuthEvent != null)
            {
                OnAuthEvent.Invoke(authEvent);
            }
        }

        /// <summary>
        /// Handles clean-up when the application quits
        /// </summary>
        private void OnQuit()
        {
            PassportLogger.Info($"{TAG} Cleaning up Passport...");

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

            DisposeAll();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Handles play mode state changes in the editor
        /// </summary>
        /// <param name="state">The current play mode state</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Dispose of all resources when exiting play mode
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                DisposeAll();
            }
        }
#endif

        /// <summary>
        /// Disposes of all resources and unsubscribes from events
        /// </summary>
        private void DisposeAll()
        {
            // Dispose of the web browser client for Windows only
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            if (webBrowserClient != null)
            {
                webBrowserClient.Dispose();
                webBrowserClient = null;
            }
#endif

            // Unsubscribe from Passport authentication events 
            // and dispose of the Passport implementation
            if (passportImpl != null)
            {
                passportImpl.OnAuthEvent -= OnPassportAuthEvent;
                passportImpl = null;
            }

            // Unsubscribe from application quitting event
            Application.quitting -= OnQuit;

#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Unsubscribe from deep link activation events on iOS and macOS
            Application.deepLinkActivated -= OnDeepLinkActivated;
#endif

            // Reset static fields
            Instance = null;
            deeplink = null;
            readySignalReceived = false;
        }
    }
}
