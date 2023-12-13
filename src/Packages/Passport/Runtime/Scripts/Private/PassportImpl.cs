using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Immutable.Passport.Helpers;
using Cysharp.Threading.Tasks;
using System.Threading;
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

        // Used for device code auth
        private DeviceConnectResponse deviceConnectResponse;

        // Used for PKCE
        private bool pkceLoginOnly = false; // Used to differentiate between a login and connect
        private UniTaskCompletionSource<bool> pkceCompletionSource;
        private string redirectUri = null;
        private string logoutRedirectUri = null;

        private string unityVersion = Application.unityVersion;
        private RuntimePlatform platform = Application.platform;
        private string osVersion = SystemInfo.operatingSystem;

#if UNITY_ANDROID
        // Used for the PKCE callback
        internal static bool completingPKCE = false;
        internal static string loginPKCEUrl;
#endif

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string redirectUri = null, string logoutRedirectUri = null, string deeplink = null)
        {
            this.redirectUri = redirectUri;
            this.logoutRedirectUri = logoutRedirectUri;
            this.communicationsManager.OnAuthPostMessage += OnDeepLinkActivated;
            this.communicationsManager.OnPostMessageError += OnPostMessageError;

            var versionInfo = new VersionInfo
            {
                engine = "unity",
                engineVersion = unityVersion,
                platform = platform.ToString(),
                platformVersion = osVersion
            };

            string initRequest;
            if (redirectUri != null && logoutRedirectUri != null)
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
                InitRequest request = new InitRequest()
                {
                    clientId = clientId,
                    environment = environment,
                    engineVersion = versionInfo
                };
                initRequest = JsonUtility.ToJson(request);
            }

            string response = await communicationsManager.Call(PassportFunction.INIT, initRequest);
            BrowserResponse initResponse = response.OptDeserializeObject<BrowserResponse>();

            if (initResponse.success == false)
            {
                throw new PassportException(initResponse.error ?? "Unable to initialise Passport");
            }
            else if (deeplink != null)
            {
                OnDeepLinkActivated(deeplink);
            }
        }

        public async UniTask<bool> Login(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            string functionName = "Login";
            if (useCachedSession)
            {
                return await Relogin();
            }
            else
            {
                ConnectResponse connectResponse = await InitialiseDeviceCodeAuth(functionName);
                if (connectResponse != null)
                {
                    await ConfirmCode(functionName, PassportFunction.LOGIN_CONFIRM_CODE, timeoutMs);
                    return true;
                }
                else
                {
                    throw new PassportException("Failed to login, please try again", PassportErrorType.AUTHENTICATION_ERROR);
                }
            }
        }

        private async UniTask<bool> Relogin()
        {
            try
            {
                string callResponse = await communicationsManager.Call(PassportFunction.RELOGIN);
                return callResponse.GetBoolResponse() ?? false;
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Failed to login to Passport using saved credentials: {ex.Message}");
            }
            return false;
        }

        public async UniTask<bool> ConnectImx(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            string functionName = "ConnectImx";
            if (useCachedSession)
            {
                return await Reconnect();
            }
            else
            {
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

                ConnectResponse connectResponse = await InitialiseDeviceCodeAuth(functionName);
                if (connectResponse != null)
                {
                    await ConfirmCode(functionName, PassportFunction.CONNECT_CONFIRM_CODE, timeoutMs);
                    return true;
                }
                else
                {
                    throw new PassportException("Failed to connect, please try again", PassportErrorType.AUTHENTICATION_ERROR);
                }
            }
        }

        private async UniTask<bool> Reconnect()
        {
            try
            {
                string callResponse = await communicationsManager.Call(PassportFunction.RECONNECT);
                return callResponse.GetBoolResponse() ?? false;
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Failed to connect to Passport using saved credentials: {ex.Message}");
            }
            return false;
        }

        private async UniTask<ConnectResponse> InitialiseDeviceCodeAuth(string callingFunction)
        {
            string callResponse = await communicationsManager.Call(PassportFunction.INIT_DEVICE_FLOW);
            BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
            if (response.success == true)
            {
                deviceConnectResponse = callResponse.OptDeserializeObject<DeviceConnectResponse>();
                if (deviceConnectResponse != null)
                {
                    return new ConnectResponse()
                    {
                        url = deviceConnectResponse.url,
                        code = deviceConnectResponse.code
                    };
                }
            }

            throw new PassportException(response.error ?? $"Something went wrong, please call {callingFunction} again", PassportErrorType.AUTHENTICATION_ERROR);
        }

        private async UniTask ConfirmCode(string callingFunction, string functionToCall, Nullable<long> timeoutMs = null)
        {
            if (deviceConnectResponse != null)
            {
                // Open URL for user to confirm
                Application.OpenURL(deviceConnectResponse.url);

                // Start polling for token
                ConfirmCodeRequest request = new ConfirmCodeRequest()
                {
                    deviceCode = deviceConnectResponse.deviceCode,
                    interval = deviceConnectResponse.interval,
                    timeoutMs = timeoutMs
                };

                string callResponse = await communicationsManager.Call(
                    functionToCall,
                    JsonUtility.ToJson(request),
                    true // Ignore timeout, this flow can take minutes to complete. 15 minute expiry from Auth0.
                );
                BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                if (response.success == false)
                {
                    throw new PassportException(
                        response.error ?? $"Unable to confirm code, call {callingFunction} again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    );
                }
            }
            else
            {
                throw new PassportException($"Call {callingFunction} first", PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public async void OnDeepLinkActivated(string url)
        {
            try
            {
                Debug.Log($"{TAG} OnDeepLinkActivated URL: {url}");

                Uri uri = new Uri(url);
                string domain = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                if (domain.EndsWith("/"))
                {
                    domain = domain.Remove(domain.Length - 1);
                }

                if (domain.Equals(logoutRedirectUri))
                {
                    await UniTask.SwitchToMainThread();
                    TrySetPKCEResult(true);
                    pkceCompletionSource = null;
                }
                else if (domain.Equals(redirectUri))
                {
                    await CompleteLoginPKCEFlow(url);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{TAG} OnDeepLinkActivated error {url}: {e.Message}");
            }
        }

        public UniTask<bool> LoginPKCE()
        {
            UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
            pkceCompletionSource = task;
            pkceLoginOnly = true;
            LaunchAuthUrl();
            return task.Task;
        }

        public UniTask<bool> ConnectImxPKCE()
        {
            UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
            pkceCompletionSource = task;
            pkceLoginOnly = false;
            LaunchAuthUrl();
            return task.Task;
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
#if UNITY_ANDROID
                    loginPKCEUrl = url;
                    LaunchAndroidUrl(url);
#else
                    communicationsManager.LaunchAuthURL(url, redirectUri);
#endif
                    return;
                }
                else
                {
                    Debug.Log($"{TAG} Failed to get PKCE Auth URL");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"{TAG} Get PKCE Auth URL error: {e.Message}");
            }

            await UniTask.SwitchToMainThread();
            TrySetPKCEException(new PassportException(
                "Something went wrong, please call ConnectPKCE() again",
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
                Uri uri = new Uri(uriString);
                string state = uri.GetQueryParameter("state");
                string authCode = uri.GetQueryParameter("code");

                if (String.IsNullOrEmpty(state) || String.IsNullOrEmpty(authCode))
                {
                    await UniTask.SwitchToMainThread();
                    TrySetPKCEException(new PassportException(
                        "Uri was missing state and/or code. Please call ConnectPKCE() again",
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
                        TrySetPKCEException(new PassportException(
                            response.error ?? "Something went wrong, please call ConnectPKCE() again",
                            PassportErrorType.AUTHENTICATION_ERROR
                        ));
                    }
                    else
                    {
                        TrySetPKCEResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
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
            Debug.Log($"{TAG} On Login PKCE Dismissed");
            if (!completing)
            {
                // User hasn't entered all required details (e.g. email address) into Passport yet
                Debug.Log($"{TAG} Login PKCE dismissed before completing the flow");
                TrySetPKCECanceled();
            }
            else
            {
                Debug.Log($"{TAG} Login PKCE dismissed by user or SDK");
            }
            loginPKCEUrl = null;
        }
#endif

        public async UniTask<string> GetAddress()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ADDRESS);
            return response.GetStringResult();
        }

        public async UniTask<string> GetLogoutUrl()
        {
            string response = await communicationsManager.Call(PassportFunction.LOGOUT);
            return response.GetStringResult();
        }

        public async UniTask Logout()
        {
            string logoutUrl = await GetLogoutUrl();
            Application.OpenURL(logoutUrl);
        }

        public UniTask LogoutPKCE()
        {
            UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
            pkceCompletionSource = task;
            LaunchLogoutPKCEUrl();
            return task.Task;
        }

        private async void LaunchLogoutPKCEUrl()
        {
            string logoutUrl = await GetLogoutUrl();

#if UNITY_ANDROID
            LaunchAndroidUrl(logoutUrl);
#else
            communicationsManager.LaunchAuthURL(logoutUrl, logoutRedirectUri);
#endif
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            string accessToken = await GetAccessToken();
            string idToken = await GetIdToken();
            return accessToken != null && idToken != null;
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

        // Imx
        public async UniTask<CreateTransferResponseV1> ImxTransfer(UnsignedTransferRequest request)
        {
            string json = JsonUtility.ToJson(request);
            Debug.Log($"{TAG} ImxTransfer json: {json}");
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.TRANSFER, json);
            return callResponse.OptDeserializeObject<CreateTransferResponseV1>();
        }

        public async UniTask<CreateBatchTransferResponse> ImxBatchNftTransfer(NftTransferDetails[] details)
        {
            string json = details.ToJson();
            Debug.Log($"{TAG} ImxBatchNftTransfer json: {json}");
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.BATCH_NFT_TRANSFER, json);
            return callResponse.OptDeserializeObject<CreateBatchTransferResponse>();
        }

        // ZkEvm
        public async UniTask ConnectEvm()
        {
            await communicationsManager.Call(PassportFunction.ZK_EVM.CONNECT_EVM);
        }

        public async UniTask<string> ZkEvmSendTransaction(TransactionRequest request)
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
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION, json);
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

        private void OnPostMessageError(string id, string message)
        {
            if (id == "CallFromAuthCallbackError" && pkceCompletionSource != null)
            {
                CallFromAuthCallbackError(id, message);
            }
            else
            {
                Debug.LogError($"{TAG} id: {id} err: {message}");
            }
        }

        private async UniTask CallFromAuthCallbackError(string id, string message)
        {
            await UniTask.SwitchToMainThread();

            if (message == "")
            {
                Debug.Log($"{TAG} Get PKCE Auth URL user cancelled");
                TrySetPKCECanceled();
            }
            else
            {
                Debug.Log($"{TAG} Get PKCE Auth URL error: {message}");
                TrySetPKCEException(new PassportException(
                    "Something went wrong, please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }

            pkceCompletionSource = null;
        }

        private void TrySetPKCEResult(bool result)
        {
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetResult(result);
            }
            else
            {
                Debug.LogError($"{TAG} PKCE completed with {result} but unable to bind result");
            }
        }

        private void TrySetPKCEException(Exception exception)
        {
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetException(exception);
            }
            else
            {
                Debug.LogError($"{TAG} {exception.Message}");
            }
        }

        private void TrySetPKCECanceled()
        {
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetCanceled();
            }
            else
            {
                Debug.LogError($"{TAG} PKCE canceled");
            }
        }

#if UNITY_ANDROID
        private void LaunchAndroidUrl(string url)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass customTabLauncher = new AndroidJavaClass("com.immutable.unity.ImmutableAndroid");
            customTabLauncher.CallStatic("launchUrl", activity, url, new AndroidPKCECallback(this));
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
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.immutable.unity.ImmutableAndroid$Callback")
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
    }
#endif
}