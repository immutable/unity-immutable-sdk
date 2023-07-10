using System;
using System.Threading;
using UnityEngine;
using Immutable.Passport.Auth;
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
            Response? initResponse = JsonUtility.FromJson<Response>(response);
            if (initResponse?.success == false)
            {
                throw new PassportException(initResponse?.error ?? "Unable to initialise Passport");
            }
        }

        /// <summary>
        /// Connects the user into Passport via device code auth and sets up the IMX provider.
        ///
        /// The user does not need to go through the device code auth flow if the saved access token is still valid or
        /// the refresh token can be used to get a new access token.
        /// <returns>
        /// The end-user verification code and url if the user has to go through device code auth, otherwise null;
        /// </returns>
        /// </summary>
        public async UniTask<ConnectResponse?> Connect(CancellationToken? token = null)
        {
            try
            {
                await ConnectSilent();
                return null;
            }
            catch (Exception)
            {
                Debug.Log($"{TAG} Unable to connect with stored credentials");
            }
            
            // Fallback to device code auth flow
            Debug.Log($"{TAG} Fallback to device code auth");
            ConnectResponse? connectResponse = await InitialiseDeviceCodeAuth();
            return connectResponse;
        }

        private async UniTask<ConnectResponse?> InitialiseDeviceCodeAuth()
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CONNECT);
            Response? response = JsonUtility.FromJson<Response>(callResponse);
            if (response?.success == true)
            {
                deviceConnectResponse = JsonUtility.FromJson<DeviceConnectResponse>(callResponse);
                if (deviceConnectResponse != null)
                {
                    return new ConnectResponse(){
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


        /// <summary>
        /// Similar to Connect, however if the saved access token is no longer valid and the refresh token cannot be used,
        /// it will not fallback to device code
        /// </summary>
        public async UniTask ConnectSilent(CancellationToken? token = null)
        {
            string callResponse = await communicationsManager.Call(PassportFunction.CHECK_STORED_CREDENTIALS);
            TokenResponse? tokenResponse = JsonUtility.FromJson<TokenResponse>(callResponse);
            if (tokenResponse != null)
            {
                // Credentials exist in storage, try and connect with it
                callResponse = await communicationsManager.Call(
                    PassportFunction.CONNECT_WITH_CREDENTIALS,
                    JsonConvert.SerializeObject(tokenResponse)
                );

                Response? response = JsonUtility.FromJson<Response>(callResponse);
                if (response?.success == false)
                {
                    throw new PassportException(
                        response?.error ?? "Unable to connect using stored credentials", 
                        PassportErrorType.AUTHENTICATION_ERROR
                    );
                }
            }
        }

        public async UniTask ConfirmCode(long? timeoutMs = null, CancellationToken? token = null)
        {
            if (deviceConnectResponse != null)
            {
                // Open URL for user to confirm
                Application.OpenURL(deviceConnectResponse.url);

                // Start polling for token
                ConfirmCodeRequest request = new(){
                    deviceCode = deviceConnectResponse.deviceCode,
                    interval = deviceConnectResponse.interval,
                    timeoutMs = timeoutMs
                };
            
                string callResponse = await communicationsManager.Call(
                    PassportFunction.CONFIRM_CODE,
                    JsonConvert.SerializeObject(request)
                );
                Response? response = JsonUtility.FromJson<Response>(callResponse);
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
            return JsonUtility.FromJson<StringResponse>(response)?.result;
        }


        public async UniTask Logout()
        {
            await communicationsManager.Call(PassportFunction.LOGOUT);
        }

        private async UniTask<TokenResponse?> GetStoredCredentials()
        {
            Debug.Log($"{TAG} Get stored credentials...");
            string callResponse = await communicationsManager.Call(PassportFunction.CHECK_STORED_CREDENTIALS);
            return JsonUtility.FromJson<TokenResponse>(callResponse);
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            TokenResponse? savedCredentials = await GetStoredCredentials();
            return savedCredentials?.accessToken != null && savedCredentials?.idToken != null;
        }

        public async UniTask<string?> GetEmail()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_EMAIL);
            return JsonUtility.FromJson<StringResponse>(response)?.result;
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