using System;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
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

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string environment, string? redirectUri = null, string? deeplink = null)
        {
            this.redirectUri = redirectUri;
            InitRequest request = new() { clientId = clientId, environment = environment, redirectUri = redirectUri };

            string response = await communicationsManager.Call(
                PassportFunction.INIT,
                JsonConvert.SerializeObject(request));
            Response? initResponse = JsonConvert.DeserializeObject<Response>(response);

            if (initResponse?.success == false)
            {
                throw new PassportException(initResponse?.error ?? "Unable to initialise Passport");
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
            string callResponse = await communicationsManager.Call(PassportFunction.GET_PKCE_AUTH_URL);
            StringResponse? response = callResponse.OptDeserializeObject<StringResponse>();

            if (response?.success == true && response?.result != null)
            {
                Application.OpenURL(response.result.Replace(" ", "+"));
            }
            else
            {
                pkceCompletionSource.TrySetException(new PassportException(
                    "Something went wrong, please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
            }
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
                authorizationCode = authCode,
                state = state
            };

            string callResponse = await communicationsManager.Call(
                    PassportFunction.CONNECT_PKCE,
                    JsonConvert.SerializeObject(request)
                );
            Response? response = callResponse.OptDeserializeObject<Response>();
            if (response?.success != true)
            {
                pkceCompletionSource.TrySetException(new PassportException(
                    response?.error ?? "Something went wrong, please call ConnectPKCE() again",
                    PassportErrorType.AUTHENTICATION_ERROR
                ));
                return;
            }

            pkceCompletionSource.TrySetResult(true);
        }

        private async UniTask<ConnectResponse?> InitialiseDeviceCodeAuth()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CONNECT);
            Response? response = JsonConvert.DeserializeObject<Response>(callResponse);
            if (response?.success == true)
            {
                deviceConnectResponse = JsonConvert.DeserializeObject<DeviceConnectResponse>(callResponse);
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
                response?.error ?? "Something went wrong, please call Connect() again",
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

                    Response? response = JsonConvert.DeserializeObject<Response>(callResponse);
                    return response?.success == true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Failed to connect to Passport using saved credentials: {ex.Message}");
            }
            await Logout();
            return false;
        }

        private async UniTask ConfirmCode(long? timeoutMs = null)
        {
            if (deviceConnectResponse != null)
            {
                // Open URL for user to confirm
                Application.OpenURL(deviceConnectResponse.url);

                // Start polling for token
                ConfirmCodeRequest request = new()
                {
                    deviceCode = deviceConnectResponse.deviceCode,
                    interval = deviceConnectResponse.interval,
                    timeoutMs = timeoutMs
                };

                string callResponse = await communicationsManager.Call(
                    PassportFunction.CONFIRM_CODE,
                    JsonConvert.SerializeObject(request),
                    true // Ignore timeout, this flow can take minutes to complete. 15 minute expiry from Auth0.
                );
                Response? response = JsonConvert.DeserializeObject<Response>(callResponse);
                if (response?.success == false)
                {
                    throw new PassportException(
                        response?.error ?? "Unable to confirm code, call Connect() again",
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
            return JsonConvert.DeserializeObject<StringResponse>(response)?.result;
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
            return JsonConvert.DeserializeObject<StringResponse>(response)?.result;
        }

        public async UniTask<string?> GetAccessToken()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.accessToken;
        }


        public async UniTask<string?> GetIdToken()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.idToken;
        }
    }
}