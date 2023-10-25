using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
        private DeviceConnectResponse? deviceConnectResponse;
        private UniTaskCompletionSource<bool>? pkceCompletionSource;
        private string? redirectUri = null;
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

        public async UniTask Init(string clientId, string environment, string? redirectUri = null, string? deeplink = null)
        {
            this.redirectUri = redirectUri;
            this.communicationsManager.OnAuthPostMessage += OnDeepLinkActivated;
            this.communicationsManager.OnPostMessageError += OnPostMessageError;

            var versionInfo = new VersionInfo
            {
                Engine = "unity",
                EngineVersion = unityVersion,
                Platform = platform.ToString(),
                PlatformVersion = osVersion
            };

            InitRequest request = new() { ClientId = clientId, Environment = environment, RedirectUri = redirectUri, EngineVersion = versionInfo };

            string response = await communicationsManager.Call(
                PassportFunction.INIT,
                JsonConvert.SerializeObject(request));
            BrowserResponse? initResponse = JsonConvert.DeserializeObject<BrowserResponse>(response);

            if (initResponse?.Success == false)
            {
                throw new PassportException(initResponse?.Error ?? "Unable to initialise Passport");
            }
            else if (deeplink != null)
            {
                OnDeepLinkActivated(deeplink);
            }
        }

        public async UniTask Connect(long? timeoutMs = null)
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
            ConnectResponse? connectResponse = await InitialiseDeviceCodeAuth();

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
                StringResponse? response = callResponse.OptDeserializeObject<StringResponse>();

                if (response?.Success == true && response?.Result != null)
                {
                    string url = response.Result.Replace(" ", "+");
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
            pkceCompletionSource?.TrySetException(new PassportException(
                "Something went wrong, please call ConnectPKCE() again",
                PassportErrorType.AUTHENTICATION_ERROR
            ));
        }

        public async UniTask CompletePKCEFlow(string uriString)
        {
#if UNITY_ANDROID
            completingPKCE = true;
#endif
            var uri = new Uri(uriString);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var state = query.Get("state");
            var authCode = query.Get("code");

            if (String.IsNullOrEmpty(state) || String.IsNullOrEmpty(authCode))
            {
                pkceCompletionSource?.TrySetException(new PassportException(
                    "Uri was missing state and/or code. Please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }

            ConnectPKCERequest request = new()
            {
                AuthorizationCode = authCode,
                State = state
            };

            string callResponse = await communicationsManager.Call(
                    PassportFunction.CONNECT_PKCE,
                    JsonConvert.SerializeObject(request)
                );
            BrowserResponse? response = callResponse.OptDeserializeObject<BrowserResponse>();
            if (response?.Success != true)
            {
                pkceCompletionSource?.TrySetException(new PassportException(
                    response?.Error ?? "Something went wrong, please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }
            else
            {
                pkceCompletionSource.TrySetResult(true);
            }
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
                pkceCompletionSource.TrySetCanceled();
            }
            else
            {
                Debug.Log($"{TAG} PKCE dismissed by user or SDK");
            }
        }
#endif

        private async UniTask<ConnectResponse?> InitialiseDeviceCodeAuth()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CONNECT);
            BrowserResponse? response = JsonConvert.DeserializeObject<BrowserResponse>(callResponse);
            if (response?.Success == true)
            {
                deviceConnectResponse = JsonConvert.DeserializeObject<DeviceConnectResponse>(callResponse);
                if (deviceConnectResponse != null)
                {
                    return new ConnectResponse()
                    {
                        Url = deviceConnectResponse.Url,
                        Code = deviceConnectResponse.Code
                    };
                }
            }

            throw new PassportException(
                response?.Error ?? "Something went wrong, please call Connect() again",
                PassportErrorType.AUTHENTICATION_ERROR
            );
        }

        public async UniTask<bool> ConnectSilent()
        {
            try
            {
                TokenResponse? tokenResponse = await GetStoredCredentials();
                if (tokenResponse != null)
                {
                    // Credentials exist in storage, try and connect with it
                    string callResponse = await communicationsManager.Call(
                        PassportFunction.CONNECT_WITH_CREDENTIALS,
                        JsonConvert.SerializeObject(tokenResponse)
                    );

                    BrowserResponse? response = JsonConvert.DeserializeObject<BrowserResponse>(callResponse);
                    return response?.Success == true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to connect to Passport using saved credentials: {ex.Message}");

                if (!(ex is PassportException) || (ex is PassportException pEx && !pEx.IsNetworkError()))
                {
                    await Logout();
                }
            }

            return false;
        }

        private async UniTask ConfirmCode(long? timeoutMs = null)
        {
            if (deviceConnectResponse != null)
            {
                // Open URL for user to confirm
                Application.OpenURL(deviceConnectResponse.Url);

                // Start polling for token
                ConfirmCodeRequest request = new()
                {
                    DeviceCode = deviceConnectResponse.DeviceCode,
                    Interval = deviceConnectResponse.Interval,
                    TimeoutMs = timeoutMs
                };

                string callResponse = await communicationsManager.Call(
                    PassportFunction.CONFIRM_CODE,
                    JsonConvert.SerializeObject(request),
                    true // Ignore timeout, this flow can take minutes to complete. 15 minute expiry from Auth0.
                );
                BrowserResponse? response = JsonConvert.DeserializeObject<BrowserResponse>(callResponse);
                if (response?.Success == false)
                {
                    throw new PassportException(
                        response?.Error ?? "Unable to confirm code, call Connect() again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    );
                }
            }
            else
            {
                throw new PassportException("Call Connect() first", PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        public async UniTask<string?> GetAddress()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ADDRESS);
            return JsonConvert.DeserializeObject<StringResponse>(response)?.Result;
        }


        public async UniTask Logout()
        {
            await communicationsManager.Call(PassportFunction.LOGOUT);
        }

        private async UniTask<TokenResponse?> GetStoredCredentials()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CHECK_STORED_CREDENTIALS);
            return callResponse.OptDeserializeObject<TokenResponse>();
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials != null;
        }

        public async UniTask<string?> GetEmail()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_EMAIL);
            return JsonConvert.DeserializeObject<StringResponse>(response)?.Result;
        }

        public async UniTask<string?> GetAccessToken()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.AccessToken;
        }


        public async UniTask<string?> GetIdToken()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.IdToken;
        }

        // Imx
        public async UniTask<CreateTransferResponseV1> ImxTransfer(UnsignedTransferRequest request)
        {
            string json = JsonConvert.SerializeObject(request);
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.TRANSFER, json);
            return JsonConvert.DeserializeObject<CreateTransferResponseV1>(callResponse);
        }

        public async UniTask<CreateBatchTransferResponse> ImxBatchNftTransfer(NftTransferDetails[] details)
        {
            string json = JsonConvert.SerializeObject(details);
            string callResponse = await communicationsManager.Call(PassportFunction.IMX.BATCH_NFT_TRANSFER, json);
            return JsonConvert.DeserializeObject<CreateBatchTransferResponse>(callResponse);
        }

        // ZkEvm
        public async UniTask ConnectEvm()
        {
            await communicationsManager.Call(PassportFunction.ZK_EVM.CONNECT_EVM);
        }

        public async UniTask<string?> ZkEvmSendTransaction(TransactionRequest request)
        {
            string json = JsonConvert.SerializeObject(
                request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
                );
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.SEND_TRANSACTION, json);
            return JsonConvert.DeserializeObject<StringResponse>(callResponse).Result;
        }

        public async UniTask<List<string>> ZkEvmRequestAccounts()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.REQUEST_ACCOUNTS);
            string[] accounts = JsonConvert.DeserializeObject<RequestAccountsResponse>(callResponse).Accounts;
            return accounts.ToList();
        }

        public async UniTask<string> ZkEvmGetBalance(string address, string blockNumberOrTag)
        {
            string json = JsonConvert.SerializeObject(new GetBalanceRequest()
            {
                Address = address,
                BlockNumberOrTag = blockNumberOrTag
            });
            string callResponse = await communicationsManager.Call(PassportFunction.ZK_EVM.GET_BALANCE, json);
            return JsonConvert.DeserializeObject<StringResponse>(callResponse).Result ?? "0x0";
        }

        private void OnPostMessageError(string id, string message)
        {
            if (id == "CallFromAuthCallbackError" && pkceCompletionSource != null)
            {
                if (message == "")
                {
                    Debug.Log($"{TAG} Get PKCE Auth URL user cancelled");
                    pkceCompletionSource?.TrySetCanceled();
                }
                else
                {
                    Debug.Log($"{TAG} Get PKCE Auth URL error: {message}");
                    pkceCompletionSource?.TrySetException(new PassportException(
                        "Something went wrong, please call ConnectPKCE() again",
                        PassportErrorType.AUTHENTICATION_ERROR
                    ));
                }

                pkceCompletionSource = null;
                return;
            }

            Debug.LogError($"{TAG} id: {id} err: {message}");
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
        public void OnPKCEDismissed(bool completing);
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.immutable.unity.ImmutableAndroid$Callback") {
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