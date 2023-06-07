using Immutable.Passport.Auth;
using UnityEngine;

namespace Immutable.Passport.Storage {
    public class CredentialsManager {
        private const string KEY_PREFS_CREDENTIALS = "prefs_credentials";

        public void SaveCredentials(TokenResponse tokenResponse) {
            string json = JsonUtility.ToJson(tokenResponse);
            PlayerPrefs.SetString(KEY_PREFS_CREDENTIALS, json);
        }

        public TokenResponse? GetCredentials() {
            string json = PlayerPrefs.GetString(KEY_PREFS_CREDENTIALS, "");
            if (string.IsNullOrWhiteSpace(json)) {
                return null;
            } else {
                return JsonUtility.FromJson<TokenResponse>(json);
            }
        }

        public bool HasValidCredentials() {
            // TODO
            return true;
        }
    }
}