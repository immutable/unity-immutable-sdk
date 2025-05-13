using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class PkceAuthScript : MonoBehaviour
    {
        [Header("PKCE Auth UI")]
        public Button pkceAuthButton;
        public Text output;

        // Passport is initialized by PassportInitializationScript in the scene.
        // This script assumes Passport.Instance is ready before PKCE auth is triggered.

        public void StartPkceAuth()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport not initialized. Please ensure PassportInitializationScript runs first.");
                Debug.LogWarning("[PkceAuthScript] Passport.Instance is null. Initialization must complete before PKCE auth.");
                return;
            }
            StartPkceAuthAsync().Forget();
        }

        private async UniTaskVoid StartPkceAuthAsync()
        {
            ShowOutput("Starting PKCE authentication...");
            try
            {
                await Passport.Instance.LoginPKCE();
                ShowOutput("PKCE authentication successful.");
            }
            catch (System.Exception ex)
            {
                ShowOutput($"PKCE authentication failed: {ex.Message}");
            }
        }

        private void ShowOutput(string message)
        {
            Debug.Log($"[PkceAuthScript] {message}");
            if (output != null)
                output.text = message;
        }
    }
} 