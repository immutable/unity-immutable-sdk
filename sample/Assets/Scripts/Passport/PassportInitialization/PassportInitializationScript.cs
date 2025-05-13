using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class PassportInitializationScript : MonoBehaviour
    {
        [Header("Passport Config")]
        [Tooltip("Client ID for Passport SDK")] 
        public string clientId;
        [Tooltip("Environment (e.g., 'production', 'sandbox')")] 
        public string environment;
        [Tooltip("Optional: UI Text to display errors")] 
        public Text errorOutput;

        [Header("Feature Buttons")]
        [Tooltip("All feature buttons to enable after Passport is initialized")]
        public Button[] featureButtons;

        private void Awake()
        {
            // Disable all feature buttons until Passport is initialized
            if (featureButtons != null)
            {
                foreach (var btn in featureButtons)
                    if (btn != null) btn.interactable = false;
            }
            // Use UniTask.Void to call async method from Awake
            InitializePassportAsync().Forget();
        }

        private async UniTaskVoid InitializePassportAsync()
        {
            try
            {
                await Passport.Init(clientId, environment);
                Debug.Log("[PassportInitializationScript] Passport initialized successfully.");
                // Enable all feature buttons
                if (featureButtons != null)
                {
                    foreach (var btn in featureButtons)
                        if (btn != null) btn.interactable = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PassportInitializationScript] Passport initialization failed: {ex.Message}");
                if (errorOutput != null)
                {
                    errorOutput.text = $"Passport initialization failed: {ex.Message}";
                }
            }
        }
    }
} 