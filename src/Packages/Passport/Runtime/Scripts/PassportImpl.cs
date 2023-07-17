using System;
using UnityEngine;
using Newtonsoft.Json;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;

namespace Immutable.Passport
{
    public class PassportImpl
    {
        private const string TAG = "[Passport Implementation]";
        public readonly IBrowserCommunicationsManager communicationsManager;
        private DeviceConnectResponse? deviceConnectResponse;

        public PassportImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId)
        {
            string response = await communicationsManager.Call(PassportFunction.INIT, clientId);
            Response? initResponse = JsonConvert.DeserializeObject<Response>(response);
            if (initResponse?.success == false)
            {
                throw new PassportException(initResponse?.error ?? "Unable to initialise Passport");
            }
        }

        public async UniTask<ConnectResponse?> Connect()
        {
            try
            {
                bool connected = await ConnectSilent();
                if (connected)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                Debug.LogError($"{TAG} Unable to connect with stored credentials");
            }

            // Fallback to device code auth flow
            Debug.Log($"{TAG} Fallback to device code auth");
            ConnectResponse? connectResponse = await InitialiseDeviceCodeAuth();
            return connectResponse;
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

            await Logout();
            throw new PassportException(
                response?.error ?? "Something went wrong, please call Connect() again",
                PassportErrorType.AUTHENTICATION_ERROR
            );
        }

        public async UniTask<bool> ConnectSilent()
        {
            try
            {
                string callResponse = await communicationsManager.Call(PassportFunction.CHECK_STORED_CREDENTIALS);
                TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(callResponse);
                if (tokenResponse != null)
                {
                    // Credentials exist in storage, try and connect with it
                    callResponse = await communicationsManager.Call(
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

        public async UniTask ConfirmCode(long? timeoutMs = null)
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
                    JsonConvert.SerializeObject(request)
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
            return JsonConvert.DeserializeObject<TokenResponse>(callResponse);
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.accessToken != null && savedCredentials?.idToken != null;
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