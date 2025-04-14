using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Immutable.Passport.Event;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Immutable.Passport.Helpers;
using Cysharp.Threading.Tasks;
using Immutable.Passport.Core.Logging;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Immutable.Passport
{
#if UNITY_ANDROID
    public class PassportImpl : PKCECallback
#else
    public class PassportImpl
#endif
    {
        private const string TAG = "[Passport Implementation]";
        public readonly IBrowserCommunicationsManager communicationsManager;
        private PassportAnalytics analytics = new();

        // Used for device code auth
        private DeviceConnectResponse? deviceConnectResponse;

        // Used for PKCE
        private bool pkceLoginOnly; // Used to differentiate between a login and connect
        private UniTaskCompletionSource<bool>? pkceCompletionSource;
        private string? redirectUri;
        private string? logoutRedirectUri;

#if UNITY_ANDROID
        // Used for the PKCE callback
        internal static bool completingPKCE = false;
        internal static string loginPKCEUrl;
#endif

        // Used to prevent calling login/connect functions multiple times
        private bool isLoggedIn = false;

        public event OnAuthEventDelegate? OnAuthEvent;

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string? redirectUri = null,
            string? logoutRedirectUri = null, string? deeplink = null)
        {
            this.redirectUri = redirectUri;
            this.logoutRedirectUri = logoutRedirectUri;

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
            this.communicationsManager.OnAuthPostMessage += OnDeepLinkActivated;
            this.communicationsManager.OnPostMessageError += OnPostMessageError;
#endif

            var versionInfo = new VersionInfo
            {
                engine = "unity",
                engineVersion = Application.unityVersion,
                engineSdkVersion = SdkVersionInfoHelpers.GetSdkVersionInfo(),
                platform = Application.platform.ToString(),
                platformVersion = SystemInfo.operatingSystem,
                deviceModel = SystemInfo.deviceModel
            };

            string initRequest;
            if (redirectUri != null)
            {
                InitRequestWithRedirectUri requestWithRedirectUri = new InitRequestWithRedirectUri()
                {
                    clientId = clientId,
                    environment = environment,
                    redirectUri = redirectUri,
                    logoutRedirectUri = logoutRedirectUri,
                    engineVersion = versionInfo
                };
                initRequest = JsonUtility.ToJson(requestWithRedirectUri);
            }
            else
            {
                InitRequest request = new InitRequest
                {
                    clientId = clientId,
                    environment = environment,
                    logoutRedirectUri = logoutRedirectUri,
                    engineVersion = versionInfo
                };
                initRequest = JsonUtility.ToJson(request);
            }

            string response = await communicationsManager.Call(PassportFunction.INIT, initRequest);
            BrowserResponse initResponse = response.OptDeserializeObject<BrowserResponse>();

            if (initResponse.success == false)
            {
                Track(PassportAnalytics.EventName.INIT_PASSPORT, success: false);
                throw new PassportException(initResponse.error ?? "Unable to initialise Passport");
            }

            if (deeplink != null)
            {
                OnDeepLinkActivated(deeplink);
            }

            Track(PassportAnalytics.EventName.INIT_PASSPORT, success: true);
        }

        public async UniTask<bool> Login(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            if (useCachedSession)
            {
                return await Relogin();
            }

            try
            {
                const string functionName = "Login";
                Track(PassportAnalytics.EventName.START_LOGIN);
                SendAuthEvent(PassportAuthEvent.LoggingIn);

                await InitialiseDeviceCodeAuth(functionName);
                await ConfirmCode(
                    PassportAuthEvent.LoginOpeningBrowser, PassportAuthEvent.PendingBrowserLogin, functionName,
                    PassportFunction.LOGIN_CONFIRM_CODE, timeoutMs);

                Track(PassportAnalytics.EventName.COMPLETE_LOGIN, success: true);
                SendAuthEvent(PassportAuthEvent.LoginSuccess);
                isLoggedIn = true;
                return true;
            }
            catch (Exception ex)
            {
                Track(PassportAnalytics.EventName.COMPLETE_LOGIN, success: false);
                SendAuthEvent(PassportAuthEvent.LoginFailed);
                throw ex;
            }
        }

        /// <summary>
        /// Attempts to re-login using saved credentials.
        /// </summary>
        /// <returns>
        /// Returns true if re-login is successful, otherwise false.
        /// </returns>
        private async UniTask<bool> Relogin()
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.ReloggingIn);

                string callResponse = await communicationsManager.Call(PassportFunction.RELOGIN);
                Track(PassportAnalytics.EventName.COMPLETE_RELOGIN, success: true);
                SendAuthEvent(PassportAuthEvent.ReloginSuccess);
                isLoggedIn = true;

                return true;
            }
            catch (Exception ex)
            {
                // Log a warning if re-login fails.
                PassportLogger.Warn($"{TAG} Failed to login using saved credentials. " +
                    $"Please check if user has saved credentials first by calling HasCredentialsSaved() : {ex.Message}");
                isLoggedIn = false;
            }

            Track(PassportAnalytics.EventName.COMPLETE_RELOGIN, success: false);
            SendAuthEvent(PassportAuthEvent.ReloginFailed);
            return false;
        }

        public async UniTask<bool> ConnectImx(bool useCachedSession = false, long? timeoutMs = null)
        {
            if (useCachedSession)
            {
                return await Reconnect();
            }

            // If the user called Login before and then ConnectImx, there is no point triggering device flow again
            bool hasCredsSaved = await HasCredentialsSaved();
            if (hasCredsSaved)
            {
                bool reconnected = await Reconnect();
                if (reconnected)
                {
                    // Successfully reconnected
                    return reconnected;
                }
                // Otherwise fallback to device code flow
            }

            try
            {
                const string functionName = "ConnectImx";
                Track(PassportAnalytics.EventName.START_CONNECT_IMX);
                SendAuthEvent(PassportAuthEvent.ConnectingImx);

                await InitialiseDeviceCodeAuth(functionName);
                await ConfirmCode(
                    PassportAuthEvent.ConnectImxOpeningBrowser, PassportAuthEvent.PendingBrowserLoginAndProviderSetup,
                    functionName, PassportFunction.CONNECT_CONFIRM_CODE, timeoutMs);

                Track(PassportAnalytics.EventName.COMPLETE_CONNECT_IMX, success: true);
                SendAuthEvent(PassportAuthEvent.ConnectImxSuccess);
                isLoggedIn = true;
                return true;
            }
            catch (Exception ex)
            {
                Track(PassportAnalytics.EventName.COMPLETE_CONNECT_IMX, success: false);
                SendAuthEvent(PassportAuthEvent.ConnectImxFailed);
                throw ex;
            }
        }

        /// <summary>
        /// Attempts to reconnect using saved credentials.
        /// </summary>
        /// <returns>True if reconnect is successful, otherwise false.</returns>
        private async UniTask<bool> Reconnect()
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.Reconnecting);

                string callResponse = await communicationsManager.Call(PassportFunction.RECONNECT);

                Track(PassportAnalytics.EventName.COMPLETE_RECONNECT, success: true);
                SendAuthEvent(PassportAuthEvent.ReconnectSuccess);
                isLoggedIn = true;
                return true;
            }
            catch (Exception ex)
            {
                // Log a warning if reconnect fails.
                PassportLogger.Warn($"{TAG} Failed to connect using saved credentials. " +
                    $"Please check if user has saved credentials first by calling HasCredentialsSaved() : {ex.Message}");

                isLoggedIn = false;
            }

            Track(PassportAnalytics.EventName.COMPLETE_RECONNECT, success: false);
            SendAuthEvent(PassportAuthEvent.ReconnectFailed);
            return false;
        }

        private async UniTask<ConnectResponse> InitialiseDeviceCodeAuth(string callingFunction)
        {
            string callResponse = await communicationsManager.Call(PassportFunction.INIT_DEVICE_FLOW);
            deviceConnectResponse = callResponse.OptDeserializeObject<DeviceConnectResponse>();
            if (deviceConnectResponse != null && deviceConnectResponse.success == true)
            {
                return new ConnectResponse()
                {
                    url = deviceConnectResponse.url,
                    code = deviceConnectResponse.code
                };
            }

            throw new PassportException(
                    deviceConnectResponse?.error ?? $"Something went wrong, please call {callingFunction} again",
                    PassportErrorType.AUTHENTICATION_ERROR
                );
        }

        private async UniTask ConfirmCode(
            PassportAuthEvent openingBrowserAuthEvent, PassportAuthEvent pendingAuthEvent,
            string callingFunction, string functionToCall, long? timeoutMs)
        {
            if (deviceConnectResponse != null)
            {
                long intervalMs = deviceConnectResponse.interval * 1000;
                if (timeoutMs != null && timeoutMs < intervalMs)
                {
                    throw new ArgumentException($"timeoutMs should be longer than {intervalMs}ms");
                }

                // Open URL for user to confirm
                SendAuthEvent(openingBrowserAuthEvent);
                OpenUrl(deviceConnectResponse.url);

                // Start polling for token
                SendAuthEvent(pendingAuthEvent);
                ConfirmCodeRequest request = new ConfirmCodeRequest()
                {
                    deviceCode = deviceConnectResponse.deviceCode,
                    interval = deviceConnectResponse.interval,
                    timeoutMs = timeoutMs
                };

                string callResponse = await communicationsManager.Call(
                    functionToCall,
                    JsonUtility.ToJson(request),
                    ignoreTimeout: timeoutMs == null,
                    timeoutMs);
            }
            else
            {
                throw new PassportException($"Unable to confirm code, call {callingFunction} again",
                    PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public async void OnDeepLinkActivated(string url)
        {
            try
            {
                PassportLogger.Info($"{TAG} Received deeplink URL: {url}");

                Uri uri = new Uri(url);
                string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";

                string domain = $"{uri.Scheme}://{hostWithPort}{uri.AbsolutePath}";

                if (domain.EndsWith("/"))
                {
                    domain = domain.Remove(domain.Length - 1);
                }

                if (domain.Equals(logoutRedirectUri))
                {
                    HandleLogoutPKCESuccess();
                }
                else if (domain.Equals(redirectUri))
                {
                    await CompleteLoginPKCEFlow(url);
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Deeplink error {url}: {ex.Message}");
            }
        }

        public UniTask<bool> LoginPKCE()
        {
            try
            {
                Track(PassportAnalytics.EventName.START_LOGIN_PKCE);
                SendAuthEvent(PassportAuthEvent.LoggingInPKCE);

                UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
                pkceCompletionSource = task;
                pkceLoginOnly = true;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                WindowsDeepLink.Initialise(redirectUri, OnDeepLinkActivated);
#endif
                _ = LaunchAuthUrl();
                return task.Task;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log in using PKCE flow: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.LoginPKCEFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public UniTask<bool> ConnectImxPKCE()
        {
            try
            {
                Track(PassportAnalytics.EventName.START_CONNECT_IMX_PKCE);
                SendAuthEvent(PassportAuthEvent.ConnectingImxPKCE);
                UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
                pkceCompletionSource = task;
                pkceLoginOnly = false;

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                WindowsDeepLink.Initialise(redirectUri, OnDeepLinkActivated);
#endif

                _ = LaunchAuthUrl();
                return task.Task;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to connect using PKCE flow: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_CONNECT_IMX_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.ConnectImxPKCEFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async UniTask LaunchAuthUrl()
        {
            try
            {
                string callResponse = await communicationsManager.Call(PassportFunction.GET_PKCE_AUTH_URL);
                StringResponse response = callResponse.OptDeserializeObject<StringResponse>();

                if (response != null && response.success == true && response.result != null)
                {
                    string url = response.result.Replace(" ", "+");
#if UNITY_ANDROID && !UNITY_EDITOR
                    loginPKCEUrl = url;
                    SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCELaunchingCustomTabs : PassportAuthEvent.ConnectImxPKCELaunchingCustomTabs);
                    LaunchAndroidUrl(url);
#else
                    SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCEOpeningWebView : PassportAuthEvent.ConnectImxPKCEOpeningWebView);
                    communicationsManager.LaunchAuthURL(url, redirectUri);
#endif
                    return;
                }
                else
                {
                    PassportLogger.Error($"{TAG} Failed to get PKCE Auth URL");
                }
            }
            catch (Exception e)
            {
                PassportLogger.Error($"{TAG} Get PKCE Auth URL error: {e.Message}");
            }

            await UniTask.SwitchToMainThread();
            TrySetPKCEException(new PassportException(
                "Something went wrong, please call LoginPKCE() or ConnectImxPKCE() again",
                PassportErrorType.AUTHENTICATION_ERROR
            ));
        }

        public async UniTask CompleteLoginPKCEFlow(string uriString)
        {
#if UNITY_ANDROID
            completingPKCE = true;
#endif
            try
            {
                SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.CompletingLoginPKCE : PassportAuthEvent.CompletingConnectImxPKCE);
                Uri uri = new Uri(uriString);
                string state = uri.GetQueryParameter("state");
                string authCode = uri.GetQueryParameter("code");

                if (String.IsNullOrEmpty(state) || String.IsNullOrEmpty(authCode))
                {
                    Track(
                        pkceLoginOnly ? PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE : PassportAnalytics.EventName.COMPLETE_CONNECT_IMX_PKCE,
                        success: false
                    );
                    SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCEFailed : PassportAuthEvent.ConnectImxPKCEFailed);
                    await UniTask.SwitchToMainThread();
                    TrySetPKCEException(new PassportException(
                        "Uri was missing state and/or code. Please call ConnectImxPKCE() again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    ));
                }
                else
                {
                    ConnectPKCERequest request = new ConnectPKCERequest()
                    {
                        authorizationCode = authCode,
                        state = state
                    };

                    string callResponse = await communicationsManager.Call(
                            pkceLoginOnly ? PassportFunction.LOGIN_PKCE : PassportFunction.CONNECT_PKCE,
                            JsonUtility.ToJson(request)
                        );

                    BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                    await UniTask.SwitchToMainThread();

                    if (response != null && response.success != true)
                    {
                        Track(
                            pkceLoginOnly ? PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE : PassportAnalytics.EventName.COMPLETE_CONNECT_IMX_PKCE,
                            success: false
                        );
                        SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCEFailed : PassportAuthEvent.ConnectImxPKCEFailed);
                        TrySetPKCEException(new PassportException(
                            response.error ?? "Something went wrong, please call ConnectImxPKCE() again",
                            PassportErrorType.AUTHENTICATION_ERROR
                        ));
                    }
                    else
                    {
                        if (!isLoggedIn)
                        {
                            TrySetPKCEResult(true);
                        }

                        Track(
                            pkceLoginOnly ? PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE : PassportAnalytics.EventName.COMPLETE_CONNECT_IMX_PKCE,
                            success: true
                        );
                        SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCESuccess : PassportAuthEvent.ConnectImxPKCESuccess);
                        isLoggedIn = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Track(
                    pkceLoginOnly ? PassportAnalytics.EventName.COMPLETE_LOGIN_PKCE : PassportAnalytics.EventName.COMPLETE_CONNECT_IMX_PKCE,
                    success: false
                );
                SendAuthEvent(pkceLoginOnly ? PassportAuthEvent.LoginPKCEFailed : PassportAuthEvent.ConnectImxPKCEFailed);
                // Ensure any failure results in completing the flow regardless.
                TrySetPKCEException(ex);
            }

            pkceCompletionSource = null;
#if UNITY_ANDROID
            completingPKCE = false;
#endif
        }

#if UNITY_ANDROID
        public void OnLoginPKCEDismissed(bool completing)
        {
            if (!completing && !isLoggedIn)
            {
                // User hasn't entered all required details (e.g. email address) into Passport yet
                PassportLogger.Info($"{TAG} Login PKCE dismissed before completing the flow");
                TrySetPKCECanceled();
            }
            else
            {
                PassportLogger.Info($"{TAG} Login PKCE dismissed by user or SDK");
            }
            loginPKCEUrl = null;
        }

        public void OnDeeplinkResult(string url)
        {
            OnDeepLinkActivated(url);
        }
#endif

        public async UniTask<string> GetLogoutUrl()
        {
            string response = await communicationsManager.Call(PassportFunction.LOGOUT);
            string logoutUrl = response.GetStringResult();
            if (string.IsNullOrEmpty(logoutUrl))
            {
                throw new PassportException("Failed to get logout URL", PassportErrorType.AUTHENTICATION_ERROR);
            }
            else
            {
                return response.GetStringResult();
            }
        }

        public async UniTask Logout(bool hardLogout = true)
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.LoggingOut);

                if (hardLogout)
                {
                    var logoutUrl = await GetLogoutUrl();
                    OpenUrl(logoutUrl);
                }

                Track(PassportAnalytics.EventName.COMPLETE_LOGOUT, success: true);
                SendAuthEvent(PassportAuthEvent.LogoutSuccess);
                isLoggedIn = false;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log out: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_LOGOUT, success: false);
                SendAuthEvent(PassportAuthEvent.LogoutFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public UniTask LogoutPKCE(bool hardLogout = true)
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.LoggingOutPKCE);

                UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
                pkceCompletionSource = task;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                WindowsDeepLink.Initialise(logoutRedirectUri, OnDeepLinkActivated);
#endif
                LaunchLogoutPKCEUrl(hardLogout);
                return task.Task;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log out: {ex.Message}";
                PassportLogger.Error($"{TAG} {errorMessage}");

                Track(PassportAnalytics.EventName.COMPLETE_LOGOUT_PKCE, success: false);
                SendAuthEvent(PassportAuthEvent.LogoutPKCEFailed);
                throw new PassportException(errorMessage, PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async void HandleLogoutPKCESuccess()
        {
            await UniTask.SwitchToMainThread();
            if (isLoggedIn)
            {
                TrySetPKCEResult(true);
            }
            Track(PassportAnalytics.EventName.COMPLETE_LOGOUT_PKCE, success: true);
            SendAuthEvent(PassportAuthEvent.LogoutPKCESuccess);
            isLoggedIn = false;
            pkceCompletionSource = null;
        }

        private async void LaunchLogoutPKCEUrl(bool hardLogout)
        {
            string logoutUrl = await GetLogoutUrl();
            if (hardLogout)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                LaunchAndroidUrl(logoutUrl);
#else
                communicationsManager.LaunchAuthURL(logoutUrl, logoutRedirectUri);
#endif
            }
            else
            {
                HandleLogoutPKCESuccess();
            }
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            try
            {
                SendAuthEvent(PassportAuthEvent.CheckingForSavedCredentials);
                string accessToken = await GetAccessToken();
                string idToken = await GetIdToken();
                SendAuthEvent(PassportAuthEvent.CheckForSavedCredentialsSuccess);
                return accessToken != null && idToken != null;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to check if there are credentials saved: {ex.Message}";
                PassportLogger.Debug($"{TAG} {errorMessage}");
                SendAuthEvent(PassportAuthEvent.CheckForSavedCredentialsFailed);
                return false;
            }
        }

        public async UniTask<string> GetAddress()
        {
            string response = await communicationsManager.Call(PassportFunction.IMX.GET_ADDRESS);
            return response.GetStringResult();
        }

        public async UniTask<bool> IsRegisteredOffchain()
        {
            string response = await communicationsManager.Call(PassportFunction.IMX.IS_REGISTERED_OFFCHAIN);
            return response.GetBoolResponse() ?? false;
        }

        public async UniTask<RegisterUserResponse> RegisterOffchain()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.REGISTER_OFFCHAIN);
            return callResponse.OptDeserializeObject<RegisterUserResponse>();
        }

        public async UniTask<string> GetEmail()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_EMAIL);
            return response.GetStringResult();
        }

        public async UniTask<string> GetPassportId()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_PASSPORT_ID);
            return response.GetStringResult();
        }

        public async UniTask<string> GetAccessToken()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ACCESS_TOKEN);
            return response.GetStringResult();
        }


        public async UniTask<string> GetIdToken()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ID_TOKEN);
            return response.GetStringResult();
        }

        public async UniTask<List<string>> GetLinkedAddresses()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_LINKED_ADDRESSES);
            string[] addresses = response.GetStringListResult();
            return addresses != null ? addresses.ToList() : new List<string>();
        }

        // Imx
        public async UniTask<CreateTransferResponseV1> ImxTransfer(UnsignedTransferRequest request)
        {
            string json = JsonUtility.ToJson(request);
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.TRANSFER, json);
            return callResponse.OptDeserializeObject<CreateTransferResponseV1>();
        }

        public async UniTask<CreateBatchTransferResponse> ImxBatchNftTransfer(NftTransferDetails[] details)
        {
            string json = details.ToJson();
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.BATCH_NFT_TRANSFER, json);
            return callResponse.OptDeserializeObject<CreateBatchTransferResponse>();
        }

        // ZkEvm
        public async UniTask ConnectEvm()
        {
            await communicationsManager.Call(PassportFunction.ZK_EVM.CONNECT_EVM);
        }

        private string SerialiseTransactionRequest(TransactionRequest request)
        {
            string json;
            // Nulls are serialised as empty strings when using JsonUtility
            // so we need to use another model that doesn't have the 'data' field instead
            if (String.IsNullOrEmpty(request.data))
            {
                json = JsonUtility.ToJson(new TransactionRequestNoData()
                {
                    to = request.to,
                    value = request.value
                });
            }
            else
            {
                json = JsonUtility.ToJson(request);
            }
            return json;
        }

        public async UniTask<string> ZkEvmSendTransaction(TransactionRequest request)
        {
            string json = SerialiseTransactionRequest(request);
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION, json);
            return callResponse.GetStringResult();
        }

        public async UniTask<TransactionReceiptResponse> ZkEvmSendTransactionWithConfirmation(TransactionRequest request)
        {
            string json = SerialiseTransactionRequest(request);
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION_WITH_CONFIRMATION, json);
            return callResponse.OptDeserializeObject<TransactionReceiptResponse>();
        }

        public async UniTask<TransactionReceiptResponse> ZkEvmGetTransactionReceipt(string hash)
        {
            string json = JsonUtility.ToJson(new TransactionReceiptRequest()
            {
                txHash = hash
            });
            string jsonResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.GET_TRANSACTION_RECEIPT, json);
            return jsonResponse.OptDeserializeObject<TransactionReceiptResponse>();
        }

        public async UniTask<string> ZkEvmSignTypedDataV4(string payload)
        {
            var callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.SIGN_TYPED_DATA_V4, payload);
            return callResponse.GetStringResult();
        }

        public async UniTask<List<string>> ZkEvmRequestAccounts()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.REQUEST_ACCOUNTS);
            RequestAccountsResponse accountsResponse = callResponse.OptDeserializeObject<RequestAccountsResponse>();
            return accountsResponse != null ? accountsResponse.accounts.ToList() : new List<string>();
        }

        public async UniTask<string> ZkEvmGetBalance(string address, string blockNumberOrTag)
        {
            string json = JsonUtility.ToJson(new GetBalanceRequest()
            {
                address = address,
                blockNumberOrTag = blockNumberOrTag
            });
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.GET_BALANCE, json);
            return callResponse.GetStringResult() ?? "0x0";
        }

        private async void OnPostMessageError(string id, string message)
        {
            if (id == "CallFromAuthCallbackError" && pkceCompletionSource != null)
            {
                await CallFromAuthCallbackError(id, message);
            }
            else
            {
                PassportLogger.Error($"{TAG} id: {id} err: {message}");
            }
        }

        private async UniTask CallFromAuthCallbackError(string id, string message)
        {
            await UniTask.SwitchToMainThread();

            if (message == "")
            {
                PassportLogger.Warn($"{TAG} Get PKCE Auth URL user cancelled");
                TrySetPKCECanceled();
            }
            else
            {
                PassportLogger.Error($"{TAG} Get PKCE Auth URL error: {message}");
                TrySetPKCEException(new PassportException(
                    "Something went wrong, please call LoginPKCE() or ConnectPKCEImx() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }

            pkceCompletionSource = null;
        }

        private void TrySetPKCEResult(bool result)
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE result to {result}...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetResult(result);
            }
            else
            {
                PassportLogger.Error($"{TAG} PKCE completed with {result} but unable to bind result");
            }
        }

        private void TrySetPKCEException(Exception exception)
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE exception...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetException(exception);
            }
            else
            {
                PassportLogger.Error($"{TAG} {exception.Message}");
            }
        }

        private void TrySetPKCECanceled()
        {
            PassportLogger.Debug($"{TAG} Trying to set PKCE canceled...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetCanceled();
            }
            else
            {
                PassportLogger.Warn($"{TAG} PKCE canceled");
            }
        }

        private void SendAuthEvent(PassportAuthEvent authEvent)
        {
            PassportLogger.Debug($"{TAG} Send auth event: {authEvent}");
            OnAuthEvent?.Invoke(authEvent);
        }

        protected virtual void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

#if UNITY_ANDROID
        private void LaunchAndroidUrl(string url)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass customTabLauncher = new AndroidJavaClass("com.immutable.unity.ImmutableActivity");
            customTabLauncher.CallStatic("startActivity", activity, url, new AndroidPKCECallback(this));
        }
#endif

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            communicationsManager.ClearCache(includeDiskFiles);
        }

        public void ClearStorage()
        {
            communicationsManager.ClearStorage();
        }
#endif

        protected virtual async void Track(string eventName, bool? success = null, Dictionary<string, object> properties = null)
        {
            await analytics.Track(communicationsManager, eventName, success, properties);
        }
    }

#if UNITY_ANDROID
    public interface PKCECallback
    {

        /// <summary>
        /// Called when the Android Chrome Custom Tabs is hidden. 
        /// Note that you won't be able to tell whether it was closed by the user or the SDK.
        /// <param name="completing">True if the user has entered everything required (e.g. email address),
        /// Chrome Custom Tabs have closed, and the SDK is trying to complete the PKCE flow.
        /// See <see cref="PassportImpl.CompleteLoginPKCEFlow"></param>
        /// </summary>
        void OnLoginPKCEDismissed(bool completing);

        void OnDeeplinkResult(string url);
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.immutable.unity.ImmutableActivity$Callback")
        {
            this.callback = callback;
        }

        async void onCustomTabsDismissed(string url)
        {
            await UniTask.SwitchToMainThread();

            // To differentiate what triggered this
            if (url == PassportImpl.loginPKCEUrl)
            {
                // Custom tabs dismissed for login flow
                callback.OnLoginPKCEDismissed(PassportImpl.completingPKCE);
            }
        }

        async void onDeeplinkResult(string url)
        {
            await UniTask.SwitchToMainThread();
            callback.OnDeeplinkResult(url);
        }
    }
#endif
}