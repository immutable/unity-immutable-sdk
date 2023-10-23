using System.Collections.Generic;
using System;
#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
using VoltstroStudios.UnityWebBrowser.Core;
#else
using Immutable.Browser.Gree;
#endif
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

        public static Passport? Instance { get; private set; }

#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
        private readonly IWebBrowserClient webBrowserClient = new WebBrowserClient();
#else
        private readonly IWebBrowserClient webBrowserClient = new GreeBrowserClient();
#endif

        // Keeps track of the latest received deeplink
        private static string? deeplink = null;
        private static bool? readySignalReceived = null;
        private PassportImpl? passportImpl = null;

        private Passport()
        {
#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
            Application.quitting += OnQuit;
#elif UNITY_IPHONE || UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                OnDeepLinkActivated(Application.absoluteURL);
            }
#endif
        }

        /// <summary>
        /// Initialises Passport
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="environment">The environment to connect to</param>
        /// <param name="redirectUri">(Currently, mobile only) The URL to which auth will redirect the browser after authorisation has been granted by the user</param>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout time for waiting for the engine to start (in milliseconds)</param>
        public static UniTask<Passport> Init(
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string clientId, string environment, string? redirectUri = null, int engineStartupTimeoutMs = 4000
#else
            string clientId, string environment, string? redirectUri = null
#endif
        )
        {
            if (Instance == null)
            {
                Debug.Log($"{TAG} Initialising Passport...");
                Instance = new Passport();
                // Wait until we get a ready signal
                return Instance.Initialise(
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                        engineStartupTimeoutMs
#endif
                    )
                    .ContinueWith(async () =>
                    {
                        await UniTask.WaitUntil(() => readySignalReceived != null);
                    })
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived == true)
                        {
                            await Instance.GetPassportImpl().Init(clientId, environment, redirectUri, deeplink);
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
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
            }
        }

        private async UniTask Initialise(
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            int engineStartupTimeoutMs
#endif
        )
        {
            try
            {
                BrowserCommunicationsManager communicationsManager = new(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
                await ((WebBrowserClient)webBrowserClient).Init(engineStartupTimeoutMs);
#endif
                passportImpl = new PassportImpl(communicationsManager);
            }
            catch (Exception ex)
            {
                // Reset values
                readySignalReceived = false;
                Instance = null;
                throw ex;
            }
        }

#if UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN
        private void OnQuit()
        {
            // Need to clean up UWB resources when quitting the game in the editor
            // as the child engine process would still be alive
            Debug.Log($"{TAG} Quitting the Player");
            ((WebBrowserClient)webBrowserClient).Dispose();
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
        /// Connects the user into Passport via device code auth and sets up the IMX provider. This will open the user's
        /// default browser and take them through Passport login.
        /// 
        /// Once authenticated, this function will set up the IMX provider.
        ///
        /// The user does not need to go through the device code auth flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// </summary>
        public async UniTask Connect(long? timeoutMs = null)
        {
            await GetPassportImpl().Connect(timeoutMs);
        }

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        /// <summary>
        /// Connects the user into Passport via PKCE auth and sets up the IMX provider.
        ///
        /// The user does not need to go through this flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// </summary>
        public async UniTask ConnectPKCE()
        {
            await GetPassportImpl().ConnectPKCE();
        }
#endif

        /// <summary>
        /// Similar to Connect, however if the saved access token is no longer valid and the refresh token cannot be used,
        /// it will not fallback to device code.
        /// <returns>
        /// True if the user is connected to Passport
        /// </returns>
        /// </summary>
        public async UniTask<bool> ConnectSilent()
        {
            return await GetPassportImpl().ConnectSilent();
        }

        /// <summary>
        /// Gets the wallet address of the logged in user.
        /// <returns>
        /// The wallet address, otherwise null
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetAddress()
        {
            return await GetPassportImpl().GetAddress();
        }

        /// <summary>
        /// Logs the user out of Passport and removes any stored credentials
        /// </summary>
        public async UniTask Logout()
        {
            await GetPassportImpl().Logout();
        }

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
        /// Retrieves the email address of the user whose credentials are currently stored.
        /// <returns>
        /// The email address, otherwise null
        /// </returns>
        /// </summary>
        public async UniTask<string?> GetEmail()
        {
            string? email = await GetPassportImpl().GetEmail();
            return email;
        }

        /// <summary>
        /// Gets the currently saved access token without verifying its validity.
        /// <returns>
        /// The access token, otherwise null
        /// </returns>
        /// </summary>
        public UniTask<string?> GetAccessToken()
        {
            return GetPassportImpl().GetAccessToken();
        }

        /// <summary>
        /// Gets the currently saved ID token without verifying its validity.
        /// <returns>
        /// The ID token, otherwise null
        /// </returns>
        /// </summary>
        public UniTask<string?> GetIdToken()
        {
            return GetPassportImpl().GetIdToken();
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

        // ZkEvm
        public async UniTask ConnectEvm()
        {
            await GetPassportImpl().ConnectEvm();
        }

        /// <summary>
        /// Creates new message call transaction or a contract creation, if the data field contains code, 
        /// and signs it using the account specified in from.
        /// <returns>
        /// The transaction hash, or the zero hash if the transaction is not yet available.
        /// </returns>
        /// </summary>
        public async UniTask<string?> ZkEvmSendTransaction(TransactionRequest request)
        {
            return await GetPassportImpl().ZkEvmSendTransaction(request);
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
    }
}
