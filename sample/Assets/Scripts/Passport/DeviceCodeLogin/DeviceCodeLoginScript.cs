using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class DeviceCodeLoginScript : MonoBehaviour
    {
        [Header("Device Code Login UI")]
        public Button deviceCodeLoginButton;
        public Text output;
        public InputField deviceCodeTimeoutMs;

        // Passport is initialized by PassportInitializationScript in the scene.
        // This script assumes Passport.Instance is ready before device code login is triggered.

        public void StartDeviceCodeLogin()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport not initialized. Please ensure PassportInitializationScript runs first.");
                Debug.LogWarning("[DeviceCodeLoginScript] Passport.Instance is null. Initialization must complete before device code login.");
                return;
            }
            StartDeviceCodeLoginAsync().Forget();
        }

        private async UniTaskVoid StartDeviceCodeLoginAsync()
        {
            var timeoutMs = GetDeviceCodeTimeoutMs();
            string formattedTimeout = timeoutMs != null ? $"{timeoutMs} ms" : "none";
            ShowOutput($"Logging in (timeout: {formattedTimeout})...");
            try
            {
                await Passport.Instance.Login(timeoutMs: timeoutMs);
                ShowOutput("Device code login successful.");
                AuthenticatedSceneManager.NavigateToAuthenticatedScene();
            }
            catch (System.OperationCanceledException)
            {
                ShowOutput("Device code login cancelled.");
                AuthenticatedSceneManager.NavigateToUnauthenticatedScene();
            }
            catch (System.Exception ex)
            {
                ShowOutput($"Device code login failed: {ex.Message}");
                Debug.LogException(ex);
                AuthenticatedSceneManager.NavigateToUnauthenticatedScene();
            }
        }

        private long? GetDeviceCodeTimeoutMs()
        {
            return string.IsNullOrEmpty(deviceCodeTimeoutMs?.text) ? 10000 : long.Parse(deviceCodeTimeoutMs.text); // 10 seconds default
        }

        private void ShowOutput(string message)
        {
            Debug.Log($"[DeviceCodeLoginScript] {message}");
            if (output != null)
                output.text = message;
        }
    }
} 