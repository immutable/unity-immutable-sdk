using System;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;
using Immutable.Passport.Auth;
using Newtonsoft.Json;
using System.IO;
using Immutable.Passport.Model;
using Immutable.Passport.Core;
using Cysharp.Threading.Tasks;

namespace Immutable.Passport
{
    internal class PassportImpl
    {
        private IAuthManager auth;
        private IBrowserCommunicationsManager communicationsManager;

        public PassportImpl(IAuthManager authManager, IBrowserCommunicationsManager communicationsManager)
        {
            this.auth = authManager;
            this.communicationsManager = communicationsManager;
        }

        public async UniTask<string?> Connect()
        {
            string? code = await auth.Login();
            User? user = auth.GetUser();
            if (code != null)
            {
                // Code confirmation required
                return code;
            }
            else if (user != null)
            {
                // Credentials are still valid, get provider
                await GetImxProvider(user);
                return null;
            }
            else
            {
                // Should never get to here, but if it happens, log the user to reset everything
                auth.Logout();
                throw new InvalidOperationException("Something went wrong, call Connect() again");
            }
        }

        public async UniTask ConfirmCode()
        {
            User user = await auth.ConfirmCode();
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