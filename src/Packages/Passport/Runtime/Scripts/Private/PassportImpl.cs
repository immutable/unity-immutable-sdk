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

namespace Immutable.Passport
{
    class PKCEResult
    {
        public bool success;
        public Exception? exception;
    }

    public class PassportImpl
    {
        private const string TAG = "[Passport Implementation]";
        public readonly IBrowserCommunicationsManager communicationsManager;
        private DeviceConnectResponse? deviceConnectResponse;
        private UniTaskCompletionSource<bool> pkceCompletionSource = new UniTaskCompletionSource<bool>();
        private string? redirectUri = null;
        private string unityVersion = Application.unityVersion;
        private RuntimePlatform platform = Application.platform;
        private string osVersion = SystemInfo.operatingSystem;

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string? redirectUri = null, string? deeplink = null)
        {
            this.redirectUri = redirectUri;
            // var engineVersion = $"engine-unity-{unityVersion},platform-{platform}-{osVersion}";
            var versionInfo = new Dictionary<string, string>
            {
                { "engine", "unity" },
                { "engineVersion", unityVersion },
                { "platform", $"{platform}" },
                { "platformVersion", osVersion }
            };
            var engineVersion = JsonConvert.SerializeObject(versionInfo);

            InitRequest request = new() { ClientId = clientId, Environment = environment, RedirectUri = redirectUri, EngineVersion = engineVersion };

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
            if (url.StartsWith(redirectUri))
                await CompletePKCEFlow(url);
        }

        public UniTask<bool> ConnectPKCE()
        {
            pkceCompletionSource = new UniTaskCompletionSource<bool>();
            LaunchAuthUrl();
            return pkceCompletionSource.Task;
        }

        private async UniTask LaunchAuthUrl()
        {
            try
            {
                string callResponse = await communicationsManager.Call(PassportFunction.GET_PKCE_AUTH_URL);
                StringResponse? response = callResponse.OptDeserializeObject<StringResponse>();

                if (response?.Success == true && response?.Result != null)
                {
                    Application.OpenURL(response.Result.Replace(" ", "+"));
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
            pkceCompletionSource.TrySetException(new PassportException(
                "Something went wrong, please call ConnectPKCE() again",
                PassportErrorType.AUTHENTICATION_ERROR
            ));
        }

        public async UniTask CompletePKCEFlow(string uriString)
        {
            var uri = new Uri(uriString);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var state = query.Get("state");
            var authCode = query.Get("code");

            if (String.IsNullOrEmpty(state) || String.IsNullOrEmpty(authCode))
            {
                pkceCompletionSource.TrySetException(new PassportException(
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
                pkceCompletionSource.TrySetException(new PassportException(
                    response?.Error ?? "Something went wrong, please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
                return;
            }

            pkceCompletionSource.TrySetResult(true);
        }

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
            string json = JsonConvert.SerializeObject(request);
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
    }
}