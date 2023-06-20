using System;
using Immutable.Passport.Auth;
using Immutable.Passport.Utility;
using UnityEngine;
using Newtonsoft.Json;

namespace Immutable.Passport.Storage
{

    public interface ICredentialsManager
    {
        void SaveCredentials(TokenResponse tokenResponse);
        TokenResponse? GetCredentials();
        bool HasValidCredentials();
        void ClearCredentials();
    }

    public class CredentialsManager : ICredentialsManager
    {
        private const string TAG = "[Credentials Manager]";

        public static string KEY_PREFS_CREDENTIALS = "prefs_credentials";

        public void SaveCredentials(TokenResponse tokenResponse)
        {
            Debug.Log($"{TAG} Save Credentials");
            string json = JsonConvert.SerializeObject(tokenResponse);
            Debug.Log($"{TAG} Save Credentials json {json}");
            SetStringToPlayerPrefs(KEY_PREFS_CREDENTIALS, json);
        }

        public TokenResponse? GetCredentials()
        {
            Debug.Log($"{TAG} Get Credentials");
            string json = GetStringFromPlayerPrefs(KEY_PREFS_CREDENTIALS, "");
            Debug.Log($"{TAG} Get Credentials json {json}");
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                return null;
            }
            else
            {
                return DeserializeTokenResponse(json);
            }
        }

        protected virtual TokenResponse DeserializeTokenResponse(string json)
        {
            return JsonConvert.DeserializeObject<TokenResponse>(json);
        }

        /// Checks whether the access token is still valid
        public bool HasValidCredentials()
        {
            Debug.Log($"{TAG} Has Valid Credentials");
            TokenResponse? response = GetCredentials();
            if (response != null)
            {
                Debug.Log($"{TAG} Decoding access token...");
                string? accessToken = JwtUtility.decodeJwt(response.access_token);
                if (accessToken == null)
                {
                    Debug.Log($"{TAG} Could not decode access token...");
                    return false;
                }

                // Grab the expiry time
                AccessTokenPayload? accessTokenPayload = JsonConvert.DeserializeObject<AccessTokenPayload>(accessToken);

                long expiresAt = accessTokenPayload?.exp ?? 0;
                long now = GetCurrentTimeSeconds();
                bool valid = expiresAt > now;
                Debug.Log($"{TAG} Access Token expires (UTC seconds): {expiresAt}");
                Debug.Log($"{TAG} Time now (UTC seconds): {now}");
                return valid;
            }
            else
            {
                Debug.Log($"{TAG} No Credentials");
                return false;
            }
        }

        protected virtual long GetCurrentTimeSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void ClearCredentials()
        {
            Debug.Log($"{TAG} Clear Credentials");
            DeleteKeyFromPlayerPrefs(KEY_PREFS_CREDENTIALS);
        }

        protected virtual void SetStringToPlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        protected virtual string GetStringFromPlayerPrefs(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        protected virtual void DeleteKeyFromPlayerPrefs(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}