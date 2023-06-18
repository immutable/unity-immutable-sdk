using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Immutable.Passport;
using Immutable.Passport.Storage;

namespace Immutable.Passport.Auth {
    public class AuthManager {
        private const string TAG = "[Device Code Auth]";
        private const string TAG_GET_DEVICE_CODE = "[Get Device Code]";
        private const string TAG_POLL_FOR_TOKEN = "[Poll For Token]";
        private const string TAG_GET_TOKEN = "[Get Token]";
        private const string TAG_GET_REFRESH_TOKEN = "[Refresh Token]";

        private const string DOMAIN = "https://auth.immutable.com";
        private const string PATH_AUTH_CODE = "/oauth/device/code";
        private const string PATH_TOKEN = "/oauth/token";

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
        private const string KEY_CONTENT_TYPE = "Content-Type";
        private const string CONTENT_TYPE_FORM_URL_ENCODED = "application/x-www-form-urlencoded";

        private const string ERROR_CODE_AUTH_PENDING = "authorization_pending";
        private const string ERROR_CODE_SLOW_DOWN = "slow_down";
        private const string ERROR_CODE_EXPIRED_TOKEN = "expired_token";
        private const string ERROR_CODE_ACCESS_DENIED = "access_denied";

        private DeviceCodeResponse? deviceCodeResponse;

        private User? user;
        private readonly HttpClient client;
        private ICredentialsManager manager;

        public AuthManager() : this(new HttpClient(), new CredentialsManager()) {}

        public AuthManager(HttpClient client, ICredentialsManager manager) {
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
        public async Task<string?> Login() {
            // If access token exists and is still valid, get saved credentials
            TokenResponse? savedCreds = manager.GetCredentials();
            if (manager.HasValidCredentials()) {
                Debug.Log($"{TAG} Access token exists and is still valid");
                user = savedCreds.ToUser();
                return null;
            } else if (savedCreds?.refresh_token != null) {
                // Access token does not exist or is not longer valid
                // Use refresh token if it exists
                Debug.Log($"{TAG} Refreshing token...");
                TokenResponse? tokenResponse = await RefreshToken(savedCreds.refresh_token);
                try {
                    HandleTokenResponse(tokenResponse);
                    return null;
                } catch (Exception e) {
                    Debug.Log($"{TAG} Token refresh failed");
                    // Fallback to device code below
                }
            } 
            
            // Can't use both access and refresh tokens
            // Perform device code authorisation
            Debug.Log($"{TAG} Starting device code authorisation...");
            deviceCodeResponse = await GetDeviceCodeTask();
            if (deviceCodeResponse != null) {
                return deviceCodeResponse.user_code;
            } else {
                throw new Exception($"Failed to get device code");
            }
        }

        private async Task<TokenResponse?> RefreshToken(string refreshToken) {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_GRANT_TYPE, GRANT_TYPE_REFRESH_TOKEN },
                { KEY_REFRESH_TOKEN, refreshToken }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                using var response = await client.PostAsync($"{DOMAIN}{PATH_TOKEN}", content);
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
        public async Task<User> ConfirmCode() {
            if (deviceCodeResponse != null) {
                // Poll for token
                Application.OpenURL(deviceCodeResponse.verification_uri_complete);
                var tokenResponse = await PollForTokenTask(deviceCodeResponse.device_code, deviceCodeResponse.interval);
                return HandleTokenResponse(tokenResponse);
            } else {
                throw new Exception($"Could not find device code response. Make sure to call login() first.");
            }
        }

        private User HandleTokenResponse(TokenResponse? tokenResponse) {
            if (tokenResponse != null) {
                var newUser = tokenResponse.ToUser();

                // Only persist credentials that contain the necessary data
                if (newUser?.MetadatExists() == true) {
                    user = newUser;
                    manager.SaveCredentials(tokenResponse);
                    return newUser;
                }    
            }

            throw new Exception($"Failed to login");
        }

        private async Task<DeviceCodeResponse> GetDeviceCodeTask() {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_SCOPE, SCOPE },
                { KEY_AUDIENCE, AUDIENCE }
            };

            var content = new FormUrlEncodedContent(values);

            try
            {
                using var response = await client.PostAsync($"{DOMAIN}{PATH_AUTH_CODE}", content);
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

        private async Task<TokenResponse> PollForTokenTask(string deviceCode, int interval) {
            bool needToPoll = true;
            while (needToPoll) {
                await Task.Delay(interval * 1000);

                var responseString = await GetTokenTask(deviceCode);
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);

                if (tokenResponse != null && tokenResponse.refresh_token != null) {
                    needToPoll = false;
                    return tokenResponse;
                } else if (errorResponse != null) {
                    ErrorResponse? response = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    switch (response.error) {
                        case ERROR_CODE_AUTH_PENDING:
                            Debug.Log($"{TAG} {TAG_POLL_FOR_TOKEN} Authorization still pending");
                            break;
                        case ERROR_CODE_SLOW_DOWN:
                            Debug.Log($"{TAG} {TAG_POLL_FOR_TOKEN} Polling too fast");
                            break;
                        case ERROR_CODE_EXPIRED_TOKEN:
                            needToPoll = false;
                            throw new InvalidOperationException("Token expired, please login again");
                            break;
                        case ERROR_CODE_ACCESS_DENIED:
                            needToPoll = false;
                            throw new UnauthorizedAccessException("User denied access");
                            break;
                        default:
                            throw new Exception("Error getting token");
                            break;
                    }
                } else {
                    throw new Exception("Error getting token");
                }
            }

            return null;
        }

        private async Task<string> GetTokenTask(string deviceCode) {
            var values = new Dictionary<string, string>
            {
                { KEY_CLIENT_ID, CLIENT_ID },
                { KEY_GRANT_TYPE, GRANT_TYPE_DEVICE_CODE },
                { KEY_DEVICE_CODE, deviceCode }
            };

            var content = new FormUrlEncodedContent(values);

            try {
                using var response = await client.PostAsync($"{DOMAIN}{PATH_TOKEN}", content);
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

        public void Logout() {
            manager.ClearCredentials();
        }

        public User? GetUser() {
            return user;
        }
    }
}