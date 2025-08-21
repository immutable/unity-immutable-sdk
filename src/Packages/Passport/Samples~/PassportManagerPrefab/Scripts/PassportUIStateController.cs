using UnityEngine;
using System.Collections.Generic;

namespace Immutable.Passport.UI
{
    /// <summary>
    /// Handles UI element visibility based on PassportManager login state.
    /// Automatically shows/hides login and logout elements based on authentication status.
    /// This component can be easily removed and replaced with custom state management logic.
    /// </summary>
    public class PassportUIStateController : MonoBehaviour
    {
        [Header("State Management")]
        [Tooltip("Enable automatic UI state management based on login status")]
        public bool enableStateManagement = true;

        private PassportManager passportManager;
        private GameObject[] loginElements;
        private GameObject[] logoutElements;

        private void Start()
        {
            if (!enableStateManagement) return;

            passportManager = GetComponent<PassportManager>();
            if (passportManager == null)
            {
                Debug.LogError("[PassportUIStateController] No PassportManager found on this GameObject!");
                return;
            }

            // Subscribe to PassportManager events
            passportManager.OnLoginSucceeded.AddListener(OnLoginSucceeded);
            passportManager.OnLogoutSucceeded.AddListener(OnLogoutSucceeded);
            passportManager.OnPassportInitialized.AddListener(OnPassportInitialized);

            Debug.Log("[PassportUIStateController] Subscribed to PassportManager events");

            // Find UI elements to control
            FindUIElements();

            // Set initial state (ensure logout elements are hidden initially)
            SetInitialState();

            // Use Invoke to ensure this runs after PassportManager's Start()
            Invoke(nameof(UpdateUIVisibility), 0.1f);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (passportManager != null)
            {
                passportManager.OnLoginSucceeded.RemoveListener(OnLoginSucceeded);
                passportManager.OnLogoutSucceeded.RemoveListener(OnLogoutSucceeded);
                passportManager.OnPassportInitialized.RemoveListener(OnPassportInitialized);
            }
        }

        /// <summary>
        /// Enable or disable state management at runtime
        /// </summary>
        public void SetStateManagementEnabled(bool enabled)
        {
            enableStateManagement = enabled;

            if (!enabled)
            {
                // Optionally restore all elements to visible when disabled
                ShowAllElements(true);
            }
            else if (passportManager != null)
            {
                UpdateUIVisibility();
            }
        }

        /// <summary>
        /// Manually update UI visibility (useful for custom logic)
        /// </summary>
        public void UpdateUIVisibility()
        {
            if (!enableStateManagement || passportManager == null) return;

            bool isLoggedIn = passportManager.IsLoggedIn;

            // Show/hide login elements (opposite of login state)
            foreach (GameObject loginElement in loginElements)
            {
                if (loginElement != null)
                {
                    loginElement.SetActive(!isLoggedIn);
                }
            }

            // Show/hide logout elements (same as login state)
            foreach (GameObject logoutElement in logoutElements)
            {
                if (logoutElement != null)
                {
                    logoutElement.SetActive(isLoggedIn);
                }
            }

            Debug.Log($"[PassportUIStateController] Updated UI visibility - Login state: {isLoggedIn}");
        }

        /// <summary>
        /// Show or hide all managed elements (useful for custom control)
        /// </summary>
        public void ShowAllElements(bool show)
        {
            foreach (GameObject loginElement in loginElements)
            {
                if (loginElement != null) loginElement.SetActive(show);
            }

            foreach (GameObject logoutElement in logoutElements)
            {
                if (logoutElement != null) logoutElement.SetActive(show);
            }
        }

        private void SetInitialState()
        {
            // Force logout elements to be hidden initially (regardless of PassportManager state)
            foreach (GameObject logoutElement in logoutElements)
            {
                if (logoutElement != null)
                {
                    logoutElement.SetActive(false);
                }
            }

            // Ensure login elements are visible initially
            foreach (GameObject loginElement in loginElements)
            {
                if (loginElement != null)
                {
                    loginElement.SetActive(true);
                }
            }

            Debug.Log("[PassportUIStateController] Set initial state - login elements visible, logout elements hidden");
        }

        private void OnLoginSucceeded()
        {
            Debug.Log("[PassportUIStateController] Login succeeded - showing logout UI");
            UpdateUIVisibility();
        }

        private void OnLogoutSucceeded()
        {
            Debug.Log("[PassportUIStateController] Logout succeeded - showing login UI");
            UpdateUIVisibility();
        }

        private void OnPassportInitialized()
        {
            Debug.Log("[PassportUIStateController] Passport initialized - updating UI state");
            UpdateUIVisibility();
        }

        private void FindUIElements()
        {
            // Find login elements (should be hidden when logged in)
            List<GameObject> loginElementsList = new List<GameObject>();

            Transform socialButtons = transform.Find("BackgroundOverlay/LoginPanel/SocialButtonsContainer");
            if (socialButtons != null) loginElementsList.Add(socialButtons.gameObject);

            Transform orDivider = transform.Find("BackgroundOverlay/LoginPanel/OrDivider");
            if (orDivider != null) loginElementsList.Add(orDivider.gameObject);

            Transform emailInput = transform.Find("BackgroundOverlay/LoginPanel/EmailInputField");
            if (emailInput != null) loginElementsList.Add(emailInput.gameObject);

            // Also hide wrapped containers
            Transform titleContainer = FindChildRecursive(transform, "TitleTextContainer");
            if (titleContainer != null) loginElementsList.Add(titleContainer.gameObject);

            Transform subtitleContainer = FindChildRecursive(transform, "SubtitleTextContainer");
            if (subtitleContainer != null) loginElementsList.Add(subtitleContainer.gameObject);

            Transform logoContainer = FindChildRecursive(transform, "ImmutableLogoContainer");
            if (logoContainer != null) loginElementsList.Add(logoContainer.gameObject);

            Transform legalText = FindChildRecursive(transform, "LegalTextContainer");
            if (legalText != null) loginElementsList.Add(legalText.gameObject);

            Transform marketingConsent = transform.Find("BackgroundOverlay/LoginPanel/MarketingConsentContainer");
            if (marketingConsent != null) loginElementsList.Add(marketingConsent.gameObject);

            loginElements = loginElementsList.ToArray();

            // Find logout elements (should be shown when logged in)
            List<GameObject> logoutElementsList = new List<GameObject>();

            Transform logoutButton = FindChildRecursive(transform, "LogoutButton");
            if (logoutButton != null) logoutElementsList.Add(logoutButton.gameObject);

            logoutElements = logoutElementsList.ToArray();

            Debug.Log($"[PassportUIStateController] Found {loginElements.Length} login elements and {logoutElements.Length} logout elements");
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
    }
}
