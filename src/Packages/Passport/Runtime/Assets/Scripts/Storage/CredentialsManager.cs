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
        // The minimum time in seconds that the access token should last before expiration
        private const long VALID_CREDENTIALS_MIN_TTL_SEC = 3600; // 1 hour

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

        protected virtual TokenResponse? DeserializeTokenResponse(string json)
        {
            return JsonConvert.DeserializeObject<TokenResponse>(json);
        }

        /// Checks if both the access token and id token are still valid
        public bool HasValidCredentials()
        {
            Debug.Log($"{TAG} Has Valid Credentials");
            TokenResponse? response = GetCredentials();
            if (response != null)
            {
                Debug.Log($"{TAG} Checking access token: {response.access_token}");
                bool accessTokenValid = IsTokenValid(response.access_token);
                Debug.Log($"{TAG} Checking ID token: {response.id_token}");
                bool idTokenValid = IsTokenValid(response.id_token);
                return accessTokenValid && idTokenValid;
            }
            else
            {
                Debug.Log($"{TAG} No Credentials");
                return false;
            }
        }

        private bool IsTokenValid(string jwt) {
            Debug.Log($"{TAG} Decoding {jwt}...");
            string? token = JwtUtility.DecodeJwt(jwt);
            if (token == null)
            {
                Debug.Log($"{TAG} Could not decode token...");
                return false;
            }
            // Grab expiry time
            TokenPayload? tokenPayload = JsonConvert.DeserializeObject<TokenPayload>(token);
            long expiresAt = tokenPayload?.exp ?? 0;
            Debug.Log($"{TAG} Token expires (UTC seconds): {expiresAt}");
            
            long now = GetCurrentTimeSeconds() + VALID_CREDENTIALS_MIN_TTL_SEC;
            Debug.Log($"{TAG} Time now (UTC seconds): {now}");

            return expiresAt > now;
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