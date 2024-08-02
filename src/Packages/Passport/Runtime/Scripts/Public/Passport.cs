using System.Collections.Generic;
using System;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
#if !IMMUTABLE_CUSTOM_BROWSER
using VoltstroStudios.UnityWebBrowser.Core;
#endif
#elif (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
using Immutable.Browser.Gree;
#endif
using Immutable.Passport.Event;
using Immutable.Browser.Core;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immutable.Passport
{

    public class Passport
    {
        private const string TAG = "[Passport]";

        public static Passport Instance { get; private set; }
        private PassportImpl passportImpl = null;

        private IWebBrowserClient webBrowserClient;

        // Keeps track of the latest received deeplink
        private static string deeplink = null;
        private static bool readySignalReceived = false;

        /// <summary>
        /// Passport auth events
        /// </summary>
        /// <seealso cref="Immutable.Passport.Event.PassportAuthEvent" />
        public event OnAuthEventDelegate OnAuthEvent;

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
        }

        /// <summary>
        /// Initialises Passport with the specified parameters. 
        /// This sets up the Passport instance, configures the web browser, and waits for the ready signal.
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="environment">The environment to connect to</param>
        /// <param name="redirectUri">(Android, iOS, and macOS only) The URL where the browser will redirect after successful authentication.</param>
        /// <param name="logoutRedirectUri">(Android, iOS, and macOS only) The URL where the browser will redirect after logout is complete.</param>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout duration in milliseconds to wait for the default Windows browser engine to start.</param>
        /// <param name="webBrowserClient">(Windows only) Custom Windows browser to use instead of the default browser in the SDK.</param>
        public static UniTask<Passport> Init(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            string clientId, 
            string environment, 
            string redirectUri = null, 
            string logoutRedirectUri = null,
            int engineStartupTimeoutMs = 30000,
            IWindowsWebBrowserClient windowsWebBrowserClient = null
#else
            string clientId,
            string environment,
            string redirectUri = null,
            string logoutRedirectUri = null
#endif
        )
        {
            if (Instance == null)
            {
                Debug.Log($"{TAG} Initialising Passport...");
                Instance = new Passport();

                // Start initialisation process
                return Instance.Initialise(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                        engineStartupTimeoutMs, windowsWebBrowserClient
#endif
                    )
                    .ContinueWith(async () =>
                    {
                        // Wait for the ready signal
                        Debug.Log($"{TAG} Waiting for ready signal...");
                        await UniTask.WaitUntil(() => readySignalReceived == true);
                    })
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived == true)
                        {
                            // Initialise Passport with provided parameters
                            await Instance.GetPassportImpl().Init(clientId, environment, redirectUri, logoutRedirectUri, deeplink);
                            return Instance;
                        }
                        else
                        {
                            Debug.Log($"{TAG} Failed to initialise Passport");
                            throw new PassportException("Failed to initiliase Passport", PassportErrorType.INITALISATION_ERROR);
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
                    // Initialise with default Windows browser client
                    this.webBrowserClient = new WebBrowserClient();
                    await ((WebBrowserClient)this.webBrowserClient).Init(engineStartupTimeoutMs);
#endif
                }
#elif (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                // Initialise default browser client for Android, iOS, and macOS
                webBrowserClient = new GreeBrowserClient();
#else
                throw new PassportException("Platform not supported");
#endif

                // Set up browser communication
                BrowserCommunicationsManager communicationsManager = new BrowserCommunicationsManager(webBrowserClient);
                // Mark ready when browser is initialised and game bridge file is loaded
                communicationsManager.OnReady += () => readySignalReceived = true;

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
        /// <summary>
        /// Handles clean-up when the application quits.
        /// </summary>
        private void OnQuit()
        {
            Debug.Log($"{TAG} Quitting the Player");
            webBrowserClient.Dispose();
            Instance = null;
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
        public async UniTask<bool> Login(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            return await GetPassportImpl().Login(useCachedSession, timeoutMs);
        }

        /// <summary>
        /// Logs the user into Passport via device code auth and sets up the Immutable X provider. This will open the user's
        /// default browser and take them through Passport login.
        /// <param name="useCachedSession">If true, the saved access token or refresh token will be used to connect the user. If this fails, it will not fallback to device code auth.</param>
        /// <param name="timeoutMs">(Optional) The maximum time, in milliseconds, the function is allowed to take before a TimeoutException is thrown. If not set, the function will wait indefinitely.</param>
        /// </summary>
        public async UniTask<bool> ConnectImx(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            return await GetPassportImpl().ConnectImx(useCachedSession, timeoutMs);
        }

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
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

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
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
    }
}
