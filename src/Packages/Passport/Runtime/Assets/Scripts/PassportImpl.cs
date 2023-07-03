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
        private readonly IAuthManager auth;
        public readonly IBrowserCommunicationsManager communicationsManager;

        public PassportImpl(IAuthManager authManager, IBrowserCommunicationsManager communicationsManager)
        {
            this.auth = authManager;
            this.communicationsManager = communicationsManager;
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
            ConnectResponse? response = await auth.Login(token);
            User? user = auth.GetUser();
            if (response != null)
            {
                // Code confirmation required
                return response;
            }
            else if (user != null)
            {
                // Credentials are still valid, get provider
                await GetImxProvider(user);
                return null;
            }
            else
            {
                // Should never get to here, but if it happens, log the user out to reset everything
                auth.Logout();
                throw new InvalidOperationException("Something went wrong, call Connect() again");
            }
        }


        /// <summary>
        /// Similar to Connect, however if the saved access token is no longer valid and the refresh token cannot be used,
        /// it will not fallback to device code
        /// <returns>
        /// True if the user successfully logged in using the saved access or refresh token.
        /// </returns>
        /// </summary>
        public async UniTask ConnectSilent(CancellationToken? token = null)
        {
            bool response = await auth.LoginSilent(token);
            User? user = auth.GetUser();
            if (response && user != null)
            {
                await GetImxProvider(user);
            }
        }

        public async UniTask ConfirmCode(CancellationToken? token = null)
        {
            User user = await auth.ConfirmCode(token);
            await GetImxProvider(user);
        }


        private async UniTask GetImxProvider(User u)
        {
            // Only send necessary values
            GetImxProviderRequest request = new(u.idToken, u.accessToken, u.refreshToken, u.profile, u.etherKey);
            string data = JsonConvert.SerializeObject(request);

            string? response = await communicationsManager.Call(PassportFunction.GET_IMX_PROVIDER, data);
            bool success = JsonUtility.FromJson<Response>(response)?.success == true;
            if (!success)
            {
                throw new PassportException("Failed to get IMX provider", PassportErrorType.WALLET_CONNECTION_ERROR);
            }
        }


        public async UniTask<string?> GetAddress()
        {
            string response = await communicationsManager.Call(PassportFunction.GET_ADDRESS);
            return JsonUtility.FromJson<AddressResponse>(response)?.address;
        }


        public void Logout()
        {
            auth.Logout();
        }

        public bool HasCredentialsSaved()
        {
            return auth.HasCredentialsSaved();
        }

        public string? GetEmail()
        {
            return auth.GetEmail();
        }


        public string? GetAccessToken()
        {
            User? user = auth.GetUser();
            if (user != null)
            {
                return user.accessToken;
            }
            else
            {
                return null;
            }
        }


        public string? GetIdToken()
        {
            User? user = auth.GetUser();
            if (user != null)
            {
                return user.idToken;
            }
            else
            {
                return null;
            }
        }

        public async UniTask<string?> SignMessage(string message)
        {
            string response = await communicationsManager.Call(PassportFunction.SIGN_MESSAGE, message);
            return JsonUtility.FromJson<StringResponse>(response)?.result;
        }
    }
}