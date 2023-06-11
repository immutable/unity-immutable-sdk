using System;
using Immutable.Passport.Auth;
using Immutable.Passport.Utility;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Immutable.Passport.Storage {
    public class CredentialsManager {
        private const string TAG = "[Credentials Manager]";

        private const string KEY_PREFS_CREDENTIALS = "prefs_credentials";
        
        public void SaveCredentials(TokenResponse tokenResponse) {
            Debug.Log($"{TAG} Save Credentials");
            string json = JsonConvert.SerializeObject(tokenResponse);
            Debug.Log($"{TAG} Save Credentials json {json}");
            PlayerPrefs.SetString(KEY_PREFS_CREDENTIALS, json);
        }

        public TokenResponse? GetCredentials() {
            try {
                Debug.Log($"{TAG} Get Credentials");
                PlayerPrefs.Save();
                Debug.Log($"{TAG} Get Credentials Saved");
                string json = PlayerPrefs.GetString(KEY_PREFS_CREDENTIALS, "");
                Debug.Log($"{TAG} Get Credentials json {json}");
                if (string.IsNullOrWhiteSpace(json) || json == "{}") {
                    return null;
                } else {
                    return JsonConvert.DeserializeObject<TokenResponse>(json);
                }
            } catch (Exception ex) {
                Debug.Log($"{TAG} Get Credentials error: {ex.Message}");
                return null;
            }
        }

        /// Checks whether the access token is still valid
        public bool HasValidCredentials() {
            Debug.Log($"{TAG} Has Valid Credentials");
            TokenResponse? response = GetCredentials();
            if (response != null) {
                Debug.Log($"{TAG} Decoding access token...");
                string? accessToken = JwtUtility.decodeJwt(response.access_token);
                if (accessToken == null) {
                    Debug.Log($"{TAG} Could not decode access token...");
                    return false;
                }

                // Grab the expiry time
                AccessTokenPayload? accessTokenPayload = JsonConvert.DeserializeObject<AccessTokenPayload>(accessToken);
                Debug.Log($"{TAG} Access token payload is not null? {accessTokenPayload != null}");

                long expiresAt = accessTokenPayload?.exp ?? 0;
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                bool valid = expiresAt > now;
                Debug.Log($"{TAG} Access Token expires (UTC seconds): {expiresAt}");
                Debug.Log($"{TAG} Time now (UTC seconds): {now}");
                return valid;
            } else {
                return false;
            }
        }

        public void ClearCredentials() {
            Debug.Log($"{TAG} Clear Credentials");
            PlayerPrefs.DeleteKey(KEY_PREFS_CREDENTIALS);
        }
    }
}