using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Immutable.Passport.Storage;
using Immutable.Passport.Model;

namespace Immutable.Passport.Auth
{
    public interface IAuthManager
    {
        public UniTask<ConnectResponse?> Login(CancellationToken? token = null);
        public void Logout();
        public UniTask<User> ConfirmCode(CancellationToken? token = null);
        public User? GetUser();
        public bool HasCredentialsSaved();

    }
    public class AuthManager : IAuthManager
    {
        private const string TAG = "[Device Code Auth]";
        private const string TAG_GET_DEVICE_CODE = "[Get Device Code]";
        private const string TAG_POLL_FOR_TOKEN = "[Poll For Token]";
        private const string TAG_GET_TOKEN = "[Get Token]";
        private const string TAG_GET_REFRESH_TOKEN = "[Refresh Token]";

        public const string DOMAIN = "https://auth.immutable.com";
        public const string PATH_AUTH_CODE = "/oauth/device/code";
        public const string PATH_TOKEN = "/oauth/token";

        private const string CLIENT_ID = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
        private const string SCOPE = "openid offline_access profile email transact";
        private const string AUDIENCE = "platform_api";
        private const string GRANT_TYPE_DEVICE_CODE = "urn:ietf:params:oauth:grant-type:device_code";
        private const string GRANT_TYPE_REFRESH_TOKEN = "refresh_token";

        private const string KEY_CLIENT_ID = "client_id";
        private const string KEY_SCOPE = "scope";
        private const string KEY_AUDIENCE = "audience";
        private const string KEY_GRANT_TYPE = "grant_type";
        private const string KEY_DEVICE_CODE = "device_code";
        private const string KEY_REFRESH_TOKEN = "refresh_token";

        private const string ERROR_CODE_AUTH_PENDING = "authorization_pending";
        private const string ERROR_CODE_SLOW_DOWN = "slow_down";
        private const string ERROR_CODE_EXPIRED_TOKEN = "expired_token";
        private const string ERROR_CODE_ACCESS_DENIED = "access_denied";

        private DeviceCodeResponse? deviceCodeResponse;

        private User? user;
        private readonly HttpClient client;
        private readonly ICredentialsManager manager;

        public AuthManager() : this(new HttpClient(), new CredentialsManager()) { }

        public AuthManager(HttpClient client, ICredentialsManager manager)
        {
            this.client = client;
            this.manager = manager;
        }

        /// <summary>
        /// Logs the user into Passport using Device Authorisation Grant.
        ///
        /// The user does not need to log in if the previously issued access token is still valid or
        /// if the refresh token can be used to get a new access token.
        /// <returns>
        /// The end-user verification code if confirmation is required, otherwise null;
        /// </returns>
        /// </summary>
        public async UniTask<ConnectResponse?> Login(CancellationToken? token = null)
        {
            // If access token exists and is still valid, get saved credentials
            TokenResponse? savedCreds = manager.GetCredentials();
            if (savedCreds != null && manager.HasValidCredentials())
            {
                Debug.Log($"{TAG} Tokens exist and are still valid");
                user = savedCreds.ToUser();
                return null;
            }
            else if (savedCreds?.refresh_token != null)
            {
                // Access token does not exist or is not longer valid
                // Use refresh token if it exists
                Debug.Log($"{TAG} Refreshing token...");
                TokenResponse? tokenResponse = await RefreshToken(savedCreds.refresh_token, token);

                token?.ThrowIfCancellationRequested();

                try
                {
                    HandleTokenResponse(tokenResponse);
                    return null;
                }
                catch (Exception)
                {
                    Debug.Log($"{TAG} Token refresh failed");
                    // Clear everything and fallback to device code below
                    manager.ClearCredentials();
                }
            }

            // Can't use both access and refresh tokens
            // Perform device code authorisation
            Debug.Log($"{TAG} Starting device code authorisation...");
            deviceCodeResponse = await GetDeviceCodeTask(token);
            if (deviceCodeResponse != null)
            {
                return new ConnectResponse()
                {
                    code = deviceCodeResponse.user_code,
                    url = deviceCodeResponse.verification_uri_complete
                };
            }
            else
            {
                throw new PassportException($"Failed to get device code", PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async UniTask<TokenResponse?> RefreshToken(string refreshToken, CancellationToken? token = null)
        {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_GRANT_TYPE, GRANT_TYPE_REFRESH_TOKEN },
                { KEY_REFRESH_TOKEN, refreshToken }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                using HttpResponseMessage response = await Post($"{DOMAIN}{PATH_TOKEN}", content, token);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.Log($"{TAG} Refresh token response: {responseString}");
                return JsonConvert.DeserializeObject<TokenResponse>(responseString);
            }
            catch (Exception ex)
            {
                // When the token has been canceled, it is not a timeout.
                Debug.Log($"{TAG} {TAG_GET_REFRESH_TOKEN} {ex}");
                throw ex;
            }
        }

        /// <summary>
        /// Opens the browser with <see cref="DeviceCodeResponse"/> verification_uri_complete.
        /// <return>
        /// The token response
        /// </return>
        /// </summary>
        public async UniTask<User> ConfirmCode(CancellationToken? token = null)
        {
            if (deviceCodeResponse != null)
            {
                // Poll for token
                Application.OpenURL(deviceCodeResponse.verification_uri_complete);
                TokenResponse? tokenResponse = await PollForTokenTask(deviceCodeResponse.device_code, deviceCodeResponse.interval, token);
                return HandleTokenResponse(tokenResponse);
            }
            else
            {
                throw new PassportException($"Could not find device code response. Make sure to call login() first.", PassportErrorType.AUTHENTICATION_ERROR);
            }
        }

        private User HandleTokenResponse(TokenResponse? tokenResponse)
        {
            if (tokenResponse != null)
            {
                var newUser = tokenResponse.ToUser();

                // Only persist credentials that contain the necessary data
                if (newUser?.MetadatExists() == true)
                {
                    user = newUser;
                    manager.SaveCredentials(tokenResponse);
                    return newUser;
                }
            }

            throw new PassportException($"Failed to login", PassportErrorType.AUTHENTICATION_ERROR);
        }

        private async UniTask<DeviceCodeResponse?> GetDeviceCodeTask(CancellationToken? token = null)
        {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_SCOPE, SCOPE },
                { KEY_AUDIENCE, AUDIENCE }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                using HttpResponseMessage response = await Post($"{DOMAIN}{PATH_AUTH_CODE}", content, token);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.Log($"{TAG} Device code response: {responseString}");
                return JsonConvert.DeserializeObject<DeviceCodeResponse>(responseString);
            }
            catch (Exception ex)
            {
                // When the token has been canceled, it is not a timeout.
                Debug.Log($"{TAG} {TAG_GET_DEVICE_CODE} {ex}");
                throw ex;
            }
        }

#pragma warning disable IDE0059
        private async UniTask<TokenResponse?> PollForTokenTask(string deviceCode, int interval, CancellationToken? token)
        {
            bool needToPoll = true;
            while (needToPoll)
            {
                Task.Delay(interval * 1000).Wait();

                var responseString = await GetTokenTask(deviceCode, token);
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);

                token?.ThrowIfCancellationRequested();

                if (tokenResponse != null && tokenResponse.refresh_token != null)
                {
                    needToPoll = false;
                    return tokenResponse;
                }
                else if (errorResponse != null)
                {
                    ErrorResponse? response = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    if (response != null)
                    {
                        switch (response.error)
                        {
                            case ERROR_CODE_AUTH_PENDING:
                                Debug.Log($"{TAG} {TAG_POLL_FOR_TOKEN} Authorization still pending");
                                break;
                            case ERROR_CODE_SLOW_DOWN:
                                Debug.Log($"{TAG} {TAG_POLL_FOR_TOKEN} Polling too fast");
                                break;
                            case ERROR_CODE_EXPIRED_TOKEN:
                                needToPoll = false;
                                throw new InvalidOperationException("Token expired, please login again");
                            case ERROR_CODE_ACCESS_DENIED:
                                needToPoll = false;
                                throw new UnauthorizedAccessException("User denied access");
                            default:
                                throw new PassportException("Error getting token", PassportErrorType.AUTHENTICATION_ERROR);
                        }
                    }
                    else
                    {
                        throw new PassportException("Error getting token", PassportErrorType.AUTHENTICATION_ERROR);
                    }
                }
                else
                {
                    throw new PassportException("Error getting token", PassportErrorType.AUTHENTICATION_ERROR);
                }
            }

            return null;
        }
#pragma warning restore IDE0059

        private async UniTask<string> GetTokenTask(string deviceCode, CancellationToken? token)
        {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_GRANT_TYPE, GRANT_TYPE_DEVICE_CODE },
                { KEY_DEVICE_CODE, deviceCode }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                using HttpResponseMessage response = await Post($"{DOMAIN}{PATH_TOKEN}", content, token);
                var responseString = await response.Content.ReadAsStringAsync();
                Debug.Log($"{TAG} Token response: {responseString}");
                return responseString;
            }
            catch (Exception ex)
            {
                // When the token has been canceled, it is not a timeout.
                Debug.Log($"{TAG} {TAG_GET_TOKEN} {ex.Message}");
                throw ex;
            }
        }

        private async UniTask<HttpResponseMessage> Post(string requestUri, HttpContent content, CancellationToken? token)
        {

            HttpResponseMessage response = token != null ? await client.PostAsync(requestUri, content, (CancellationToken)token) : await client.PostAsync(requestUri, content);
            return response;
        }

        public void Logout()
        {
            manager.ClearCredentials();
        }

        public User? GetUser()
        {
            return user;
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// </summary>
        public bool HasCredentialsSaved()
        {
            return manager.GetCredentials() != null;
        }
    }
}