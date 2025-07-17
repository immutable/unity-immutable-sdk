using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
#if !IMMUTABLE_CUSTOM_BROWSER
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using VoltstroStudios.UnityWebBrowser.Shared;
using VoltstroStudios.UnityWebBrowser.Logging;
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
        private PassportImpl? _passportImpl;
        public string Environment { get; private set; }

        private IWebBrowserClient? _webBrowserClient;

        // Keeps track of the latest received deeplink
        private static string? _deeplink;
        private static bool _readySignalReceived;

        /// <summary>
        /// Passport auth events
        /// </summary>
        /// <seealso cref="Immutable.Passport.Event.PassportAuthEvent" />
        public event OnAuthEventDelegate? OnAuthEvent;

        /// <summary>
        /// Gets or sets the log level for the SDK.
        /// </summary>
        /// <remarks>
        /// The log level determines which messages are recorded based on their severity.  
        /// <para>
        /// The default value is <see cref="LogLevel.Info"/>.
        /// </para>
        /// <para>
        /// See <see cref="Immutable.Passport.Core.Logging.LogLevel"/> for valid log levels and their meanings.
        /// </para>
        /// <example>
        /// <code>Passport.LogLevel = LogLevel.Debug;</code>
        /// </example>
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

        /// <summary>
        /// Determines whether sensitive token values should be redacted from SDK logs.
        /// </summary>
        /// <remarks>
        /// When set to <c>true</c>, access tokens and ID tokens will be replaced with <code>[REDACTED]</code> in log messages to enhance security.
        /// This setting is useful for preventing sensitive data from appearing in logs, especially when debugging or sharing logs with others.
        /// <para>
        /// The default value is <c>false</c>, meaning tokens will be logged in full at appropriate log levels.
        /// </para>
        /// <example>
        /// <code>Passport.RedactTokensInLogs = true;</code>
        /// </example>
        /// </remarks>
        public static bool RedactTokensInLogs
        {
            get => _redactTokensInLogs;
            set
            {
                _redactTokensInLogs = value;
                PassportLogger.RedactionHandler = value ? RedactTokenValues : null;

#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))
                SetWindowsRedactionHandler();
#endif
            }
        }

        private static bool _redactTokensInLogs;

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
        /// <param name="redirectUri">The URL where the browser will redirect after successful authentication.</param>
        /// <param name="logoutRedirectUri">The URL where the browser will redirect after logout is complete.</param>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout duration in milliseconds to wait for the default Windows browser engine to start.</param>
        /// <param name="windowsWebBrowserClient">(Windows only) Custom Windows browser to use instead of the default browser in the SDK.</param>
        public static UniTask<Passport> Init(
            string clientId,
            string environment,
            string redirectUri,
            string logoutRedirectUri
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            , int engineStartupTimeoutMs = 60000,
            IWindowsWebBrowserClient? windowsWebBrowserClient = null
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
                Instance.Environment = environment;

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
                        await UniTask.WaitUntil(() => _readySignalReceived == true);
                    })
                    .ContinueWith(async () =>
                    {
                        if (_readySignalReceived)
                        {
                            // Initialise Passport with provided parameters
                            await Instance.GetPassportImpl().Init(clientId, environment, redirectUri, logoutRedirectUri, _deeplink);
                            return Instance;
                        }
                        else
                        {
                            PassportLogger.Error($"{TAG} Failed to initialise Passport");
                            throw new PassportException("Failed to initialise Passport", PassportErrorType.INITALISATION_ERROR);
                        }
                    });
            }

            // Return the existing instance if already initialised
            _readySignalReceived = true;
            return UniTask.FromResult(Instance);
        }

        /// <summary>
        /// Initialises the appropriate web browser and sets up browser communication.
        /// </summary>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout duration in milliseconds to wait for the default Windows browser engine to start.</param>
        /// <param name="windowsWebBrowserClient">(Windows only) Custom Windows browser to use instead of the default browser in the SDK.</param>
        private async UniTask Initialise(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            int engineStartupTimeoutMs, IWindowsWebBrowserClient? windowsWebBrowserClient
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
                    _webBrowserClient = new WindowsWebBrowserClientAdapter(windowsWebBrowserClient);
                    await ((WindowsWebBrowserClientAdapter)_webBrowserClient).Init();
                }
                else
                {
#if IMMUTABLE_CUSTOM_BROWSER
                    throw new PassportException("When 'IMMUTABLE_CUSTOM_BROWSER' is defined in Scripting Define Symbols, " + 
                        " 'windowsWebBrowserClient' must not be null.");
#else
                    _webBrowserClient = gameObject.AddComponent<UwbWebView>();
                    await ((UwbWebView)_webBrowserClient).Init(engineStartupTimeoutMs, _redactTokensInLogs, RedactTokenValues);
                    _readySignalReceived = true;
#endif
                }
#elif (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
                // Initialise default browser client for Android, iOS, and macOS
                _webBrowserClient = new GreeBrowserClient();
#else
                throw new PassportException("Platform not supported");
#endif

                // Set up browser communication
                BrowserCommunicationsManager communicationsManager = new BrowserCommunicationsManager(_webBrowserClient);

#if UNITY_WEBGL
                _readySignalReceived = true;
#else
                // Mark ready when browser is initialised and game bridge file is loaded
                communicationsManager.OnReady += () => _readySignalReceived = true;
#endif
                // Set up Passport implementation
                _passportImpl = new PassportImpl(communicationsManager);
                // Subscribe to Passport authentication events
                _passportImpl.OnAuthEvent += OnPassportAuthEvent;
            }
            catch (Exception)
            {
                // Reset everything on error
                _readySignalReceived = false;
                Instance = null;
                throw;
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
            GetPassportImpl().SetCallTimeout(ms);
        }

        /// <summary>
        /// Logs into Passport using Authorisation Code Flow with Proof Key for Code Exchange (PKCE).
        /// This opens the user's default browser on desktop or an in-app browser on mobile.
        /// <param name="useCachedSession">If true, Passport will attempt to re-authenticate the player using stored credentials. If re-authentication fails, it won't automatically prompt the user to log in again.</param>
        /// <param name="directLoginMethod">Optional direct login method to use (google, apple, facebook). If None, the user will see the standard login page.
        /// </summary>
        /// <returns>
        /// Returns true if login is successful, otherwise false.
        /// </returns>
        public async UniTask<bool> Login(bool useCachedSession = false, DirectLoginMethod directLoginMethod = DirectLoginMethod.None)
        {
            return await GetPassportImpl().Login(useCachedSession, directLoginMethod);
        }

        /// <summary>
        /// Logs the user into Passport using Authorisation Code Flow with Proof Key for Code Exchange (PKCE) and sets up the Immutable X provider.
        /// This opens the user's default browser on desktop or an in-app browser on mobile.
        /// <param name="useCachedSession">If true, Passport will attempt to re-authenticate the player using stored credentials. If re-authentication fails, it won't automatically prompt the user to log in again.</param>
        /// <param name="directLoginMethod">Optional direct login method to use (google, apple, facebook). If None, the user will see the standard login page.
        /// </summary>
        public async UniTask<bool> ConnectImx(bool useCachedSession = false, DirectLoginMethod directLoginMethod = DirectLoginMethod.None)
        {
            return await GetPassportImpl().ConnectImx(useCachedSession, directLoginMethod);
        }

        /// <summary>
        /// Gets the wallet address of the logged in user.
        /// <returns>
        /// The wallet address
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        /// <summary>
        /// Logs the user out of Passport and removes any stored credentials.
        /// </summary>
        /// <param name="hardLogout">If false, the user will not be logged out of Passport in the browser. The default is true.</param>
        public async UniTask Logout(bool hardLogout = true)
        {
            await GetPassportImpl().Logout(hardLogout);
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// <returns>
        /// True if there are credentials saved
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
        public async UniTask<RegisterUserResponse?> RegisterOffchain()
        {
            return await GetPassportImpl().RegisterOffchain();
        }

        /// <summary>
        /// Retrieves the email address of the user whose credentials are currently stored.
        /// <returns>
        /// The email address
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetEmail()
        {
            return await GetPassportImpl().GetEmail();
        }

        /// <summary>
        /// Retrieves the Passport ID of the user whose credentials are currently stored.
        /// <returns>
        /// The Passport ID
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetPassportId()
        {
            return await GetPassportImpl().GetPassportId();
        }

        /// <summary>
        /// Gets the currently saved access token without verifying its validity.
        /// <returns>
        /// The access token
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetAccessToken()
        {
            return await GetPassportImpl().GetAccessToken();
        }

        /// <summary>
        /// Gets the currently saved ID token without verifying its validity.
        /// <returns>
        /// The ID token
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetIdToken()
        {
            return await GetPassportImpl().GetIdToken();
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
        public async UniTask<CreateTransferResponseV1?> ImxTransfer(UnsignedTransferRequest request)
        {
            return await GetPassportImpl().ImxTransfer(request);
        }

        /// <summary>
        /// Create a new batch nft transfer request with the given transfer details.
        /// <returns>
        /// The transfer response if successful
        /// </returns>
        /// </summary>
        public async UniTask<CreateBatchTransferResponse?> ImxBatchNftTransfer(NftTransferDetails[] details)
        {
            return await GetPassportImpl().ImxBatchNftTransfer(details);
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
        public async UniTask<string?> ZkEvmSendTransaction(TransactionRequest request)
        {
            return await GetPassportImpl().ZkEvmSendTransaction(request);
        }

        /// <summary>
        /// Similar to <code>ZkEvmSendTransaction</code>. Sends a transaction to the network, signs it using the logged-in Passport account, and waits for the transaction to be included in a block.
        /// <returns>
        /// The receipt of the transaction or null if it is still processing.
        /// </returns>
        /// </summary>
        public async UniTask<TransactionReceiptResponse?> ZkEvmSendTransactionWithConfirmation(TransactionRequest request)
        {
            return await GetPassportImpl().ZkEvmSendTransactionWithConfirmation(request);
        }

        /// <summary>
        /// Retrieves the transaction information of a given transaction hash. This function uses the Ethereum JSON-RPC <c>eth_getTransactionReceipt</c> method.
        /// <returns>
        /// The receipt of the transaction or null if it is still processing.
        /// </returns>
        /// </summary>
        public async UniTask<TransactionReceiptResponse?> ZkEvmGetTransactionReceipt(string hash)
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
        public async UniTask<string?> ZkEvmSignTypedDataV4(string payload)
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
            if (Instance?._webBrowserClient is WebBrowserClient browserClient)
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

        private static void SetWindowsRedactionHandler()
        {
            if (Instance?._webBrowserClient is WebBrowserClient browserClient)
            {
                browserClient.Logger = new DefaultUnityWebBrowserLogger(redactionHandler: _redactTokensInLogs ? RedactTokenValues : null);
            }
        }
#endif

        /// <summary>
        /// Redacts access and ID token data from a log message if found.
        /// </summary>
        private static string RedactTokenValues(string message)
        {
            try
            {
                var match = Regex.Match(message, @"({.*})");
                if (match.Success)
                {
                    var jsonPart = match.Groups[1].Value;
                    var response = JsonUtility.FromJson<StringResponse>(jsonPart);
                    if (response?.responseFor is PassportFunction.GET_ACCESS_TOKEN or PassportFunction.GET_ID_TOKEN && !string.IsNullOrEmpty(response.result))
                    {
                        response.result = "[REDACTED]";
                        return message.Replace(jsonPart, JsonUtility.ToJson(response));
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return message;
        }

        private PassportImpl GetPassportImpl()
        {
            if (_passportImpl != null)
            {
                return _passportImpl;
            }
            throw new PassportException("Passport not initialised");
        }

        private void OnDeepLinkActivated(string url)
        {
            _deeplink = url;

            if (_passportImpl != null)
            {
                GetPassportImpl().OnDeepLinkActivated(url);
            }
        }

        private void OnPassportAuthEvent(PassportAuthEvent authEvent)
        {
            OnAuthEvent?.Invoke(authEvent);
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
            if (_webBrowserClient != null)
            {
                _webBrowserClient.Dispose();
                _webBrowserClient = null;
            }
#endif

            // Unsubscribe from Passport authentication events 
            // and dispose of the Passport implementation
            if (_passportImpl != null)
            {
                _passportImpl.OnAuthEvent -= OnPassportAuthEvent;
                _passportImpl = null;
            }

            // Unsubscribe from application quitting event
            Application.quitting -= OnQuit;

#if UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            // Unsubscribe from deep link activation events on iOS and macOS
            Application.deepLinkActivated -= OnDeepLinkActivated;
#endif

            // Reset static fields
            Instance = null;
            _deeplink = null;
            _readySignalReceived = false;
        }
    }
}
