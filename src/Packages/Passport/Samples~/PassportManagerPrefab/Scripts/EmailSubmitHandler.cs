using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport.Model;

namespace Immutable.Passport.UI
{
    /// <summary>
    /// Handles email submission for Passport login.
    /// Connects the email input field to the submit button and PassportManager.
    /// </summary>
    public class EmailSubmitHandler : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The email input field to get the email from")]
        public InputField emailInputField;

        private Button submitButton;
        private PassportManager passportManager;
        private Toggle marketingConsentToggle;

        private void Start()
        {
            submitButton = GetComponent<Button>();
            passportManager = GetComponentInParent<PassportManager>();

            if (submitButton == null)
            {
                Debug.LogError("[EmailSubmitHandler] No Button component found!");
                return;
            }

            if (passportManager == null)
            {
                Debug.LogError("[EmailSubmitHandler] No PassportManager found in parent!");
                return;
            }

            // Find marketing consent toggle
            marketingConsentToggle = FindMarketingConsentToggle();

            // Wire up the submit button
            submitButton.onClick.AddListener(OnSubmitClicked);

            // Also allow Enter key to submit
            if (emailInputField != null)
            {
                emailInputField.onEndEdit.AddListener(OnEmailEndEdit);
            }

            Debug.Log("[EmailSubmitHandler] Email submit handler initialized");
        }

        private void OnSubmitClicked()
        {
            SubmitEmail();
        }

        private void OnEmailEndEdit(string email)
        {
            // Submit when user presses Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitEmail();
            }
        }

        private void SubmitEmail()
        {
            if (emailInputField == null || passportManager == null)
            {
                Debug.LogError("[EmailSubmitHandler] Missing required components!");
                return;
            }

            string email = emailInputField.text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                Debug.LogWarning("[EmailSubmitHandler] Email field is empty!");
                return;
            }

            // Basic email validation
            if (!IsValidEmail(email))
            {
                Debug.LogWarning($"[EmailSubmitHandler] Invalid email format: {email}");
                return;
            }

            // Get marketing consent status
            MarketingConsentStatus consentStatus = MarketingConsentStatus.Unsubscribed;
            if (marketingConsentToggle != null)
            {
                // Note: The toggle text says "I don't want to receive..." so we invert the logic
                consentStatus = marketingConsentToggle.isOn ? MarketingConsentStatus.Unsubscribed : MarketingConsentStatus.OptedIn;
            }

            Debug.Log($"[EmailSubmitHandler] Submitting email login - Email: {email}, Consent: {consentStatus}");

            // Create login options with email and consent
            DirectLoginOptions loginOptions = new DirectLoginOptions
            {
                directLoginMethod = DirectLoginMethod.Email, // Default email login
                email = email,
                marketingConsentStatus = consentStatus
            };

            // Trigger login via PassportManager
            passportManager.Login(loginOptions);
        }

        private Toggle FindMarketingConsentToggle()
        {
            // Look for marketing consent toggle in the UI hierarchy
            Transform root = transform.root;
            Transform consentContainer = FindChildRecursive(root, "MarketingConsentContainer");

            if (consentContainer != null)
            {
                Toggle toggle = consentContainer.GetComponentInChildren<Toggle>();
                if (toggle != null)
                {
                    Debug.Log("[EmailSubmitHandler] Found marketing consent toggle");
                    return toggle;
                }
            }

            Debug.LogWarning("[EmailSubmitHandler] Marketing consent toggle not found");
            return null;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
