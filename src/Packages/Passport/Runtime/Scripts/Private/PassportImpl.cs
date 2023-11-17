using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Immutable.Passport.Json;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;
using System.Web;
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
        private DeviceConnectResponse deviceConnectResponse;
        private UniTaskCompletionSource<bool> pkceCompletionSource;
        private string redirectUri = null;
        private string unityVersion = Application.unityVersion;
        private RuntimePlatform platform = Application.platform;
        private string osVersion = SystemInfo.operatingSystem;

#if UNITY_ANDROID
        internal static bool completingPKCE = false; // Used for the PKCE callback
#endif

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string redirectUri = null, string deeplink = null)
        {
            this.redirectUri = redirectUri;
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
            if (redirectUri != null)
            {
                InitRequestWithRedirectUri requestWithRedirectUri = new InitRequestWithRedirectUri()
                {
                    clientId = clientId,
                    environment = environment,
                    redirectUri = redirectUri,
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

        public async UniTask Connect(Nullable<long> timeoutMs = null)
        {
            try
            {
                bool connected = await ConnectSilent();
                if (connected)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{TAG} Unable to connect with stored credentials");
            }

            // Fallback to device code auth flow
            Debug.Log($"{TAG} Fallback to device code auth");
            ConnectResponse connectResponse = await InitialiseDeviceCodeAuth();

            if (connectResponse != null)
            {
                await ConfirmCode(timeoutMs);
            }
            else
            {
                throw new PassportException(
                    "Failed to retrieve auth url, please try again",
                    PassportErrorType.AUTHENTICATION_ERROR
                    );
            }
        }

        public async void OnDeepLinkActivated(string url)
        {
            Debug.Log($"{TAG} OnDeepLinkActivated: {url} starts with {redirectUri}");
            if (url.StartsWith(redirectUri))
                await CompletePKCEFlow(url);
        }

        public UniTask<bool> ConnectPKCE()
        {
            UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
            pkceCompletionSource = task;
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
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaClass customTabLauncher = new AndroidJavaClass("com.immutable.unity.ImmutableAndroid");
                    customTabLauncher.CallStatic("launchUrl", activity, url, new AndroidPKCECallback(this));
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

        public async UniTask CompletePKCEFlow(string uriString)
        {
#if UNITY_ANDROID
            completingPKCE = true;
#endif
            try
            {
                var uri = new Uri(uriString);
                var query = HttpUtility.ParseQueryString(uri.Query);
                var state = query.Get("state");
                var authCode = query.Get("code");

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
                            PassportFunction.CONNECT_PKCE,
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
        public void OnPKCEDismissed(bool completing)
        {
            Debug.Log($"{TAG} On PKCE Dismissed");
            if (!completing)
            {
                // User hasn't entered all required details (e.g. email address) into Passport yet
                Debug.Log($"{TAG} PKCE dismissed before completing the flow");
                TrySetPKCECanceled();
            }
            else
            {
                Debug.Log($"{TAG} PKCE dismissed by user or SDK");
            }
        }
#endif

        private async UniTask<ConnectResponse> InitialiseDeviceCodeAuth()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CONNECT);
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

            throw new PassportException(
                response.error ?? "Something went wrong, please call Connect() again",
                PassportErrorType.AUTHENTICATION_ERROR
            );
        }

        public async UniTask<bool> ConnectSilent()
        {
            try
            {
                TokenResponse tokenResponse = await GetStoredCredentials();
                if (tokenResponse != null)
                {
                    // Credentials exist in storage, try and connect with it
                    string callResponse = await communicationsManager.Call(
                        PassportFunction.CONNECT_WITH_CREDENTIALS,
                        JsonUtility.ToJson(tokenResponse)
                    );

                    BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                    return response != null ? response.success == true : false;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Failed to connect to Passport using saved credentials: {ex.Message}");

                if (!(ex is PassportException) || (ex is PassportException pEx && !pEx.IsNetworkError()))
                {
                    await Logout();
                }
            }

            return false;
        }

        private async UniTask ConfirmCode(Nullable<long> timeoutMs = null)
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
                    PassportFunction.CONFIRM_CODE,
                    JsonUtility.ToJson(request),
                    true // Ignore timeout, this flow can take minutes to complete. 15 minute expiry from Auth0.
                );
                BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                if (response.success == false)
                {
                    throw new PassportException(
                        response.error ?? "Unable to confirm code, call Connect() again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    );
                }
            }
            else
            {
                throw new PassportException("Call Connect() first", PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public async UniTask<string> GetAddress()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ADDRESS);
            return response.GetStringResult();
        }


        public async UniTask Logout()
        {
            await communicationsManager.Call(PassportFunction.LOGOUT);
        }

        private async UniTask<TokenResponse> GetStoredCredentials()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CHECK_STORED_CREDENTIALS);
            return callResponse.OptDeserializeObject<TokenResponse>();
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            TokenResponse savedCredentials = await GetStoredCredentials();
            return savedCredentials != null;
        }

        public async UniTask<string> GetEmail()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_EMAIL);
            return response.GetStringResult();
        }

        public async UniTask<string> GetAccessToken()
        {
            TokenResponse savedCredentials = await GetStoredCredentials();
            return savedCredentials != null ? savedCredentials.accessToken : null;
        }


        public async UniTask<string> GetIdToken()
        {
            TokenResponse savedCredentials = await GetStoredCredentials();
            return savedCredentials != null ? savedCredentials.idToken : null;
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
            string json = JsonUtility.ToJson(details);
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
    }

#if UNITY_ANDROID
    public interface PKCECallback
    {

        /// <summary>
        /// Called when the Android Chrome Custom Tabs is hidden. 
        /// Note that you won't be able to tell whether it was closed by the user or the SDK.
        /// <param name="completing">True if the user has entered everything required (e.g. email address),
        /// Chrome Custom Tabs have closed, and the SDK is trying to complete the PKCE flow.
        /// See <see cref="PassportImpl.CompletePKCEFlow"></param>
        /// </summary>
        void OnPKCEDismissed(bool completing);
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.immutable.unity.ImmutableAndroid$Callback")
        {
            this.callback = callback;
        }

        async void onCustomTabsDismissed()
        {
            await UniTask.SwitchToMainThread();
            callback.OnPKCEDismissed(PassportImpl.completingPKCE);
        }
    }
#endif
}