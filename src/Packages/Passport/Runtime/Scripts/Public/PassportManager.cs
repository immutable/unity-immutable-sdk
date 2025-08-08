using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;
using Immutable.Passport.Model;
using System;
using Cysharp.Threading.Tasks;

namespace Immutable.Passport
{
    /// <summary>
    /// A convenient manager component for Immutable Passport that can be dropped into any scene.
    /// Automatically handles Passport initialization and provides easy configuration options.
    /// </summary>
    public class PassportManager : MonoBehaviour
    {
        [Header("Passport Configuration")]
        [SerializeField] private string clientId = "your-client-id-here";
        
        [SerializeField] private string environment = Immutable.Passport.Model.Environment.SANDBOX;
        
        [Header("Redirect URIs (required for authentication)")]
        [SerializeField] 
        [Tooltip("The redirect URI for successful login (e.g., 'mygame://callback')")]
        private string redirectUri = "";
        [SerializeField] 
        [Tooltip("The redirect URI for logout (e.g., 'mygame://logout')")]
        private string logoutRedirectUri = "";
        
        [Header("Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool autoLogin = false;
        [SerializeField] private DirectLoginMethod directLoginMethod = DirectLoginMethod.None;
        [SerializeField] private LogLevel logLevel = LogLevel.Info;
        [SerializeField] private bool redactTokensInLogs = true;
        
        [Header("UI Integration (Optional)")]
        [SerializeField] 
        [Tooltip("Button to trigger default login (will be automatically configured)")]
        private Button loginButton;
        [SerializeField] 
        [Tooltip("Button to trigger Google login (will be automatically configured)")]
        private Button googleLoginButton;
        [SerializeField] 
        [Tooltip("Button to trigger Apple login (will be automatically configured)")]
        private Button appleLoginButton;
        [SerializeField] 
        [Tooltip("Button to trigger Facebook login (will be automatically configured)")]
        private Button facebookLoginButton;
        [SerializeField] 
        [Tooltip("Button to trigger logout (will be automatically configured)")]
        private Button logoutButton;
        [SerializeField] 
        [Tooltip("Legacy Text component to display authentication status. Use this OR TextMeshPro Status Text below.")]
        private Text statusText;
        [SerializeField] 
        [Tooltip("TextMeshPro component to display authentication status. Use this OR Legacy Status Text above.")]
        private TextMeshProUGUI statusTextTMP;
        [SerializeField] 
        [Tooltip("Legacy Text component to display user information after login. Use this OR TextMeshPro User Info Text below.")]
        private Text userInfoText;
        [SerializeField] 
        [Tooltip("TextMeshPro component to display user information after login. Use this OR Legacy User Info Text above.")]
        private TextMeshProUGUI userInfoTextTMP;
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPassportInitialized;
        public UnityEngine.Events.UnityEvent<string> OnPassportError;
        public UnityEngine.Events.UnityEvent OnLoginSucceeded;
        public UnityEngine.Events.UnityEvent<string> OnLoginFailed;
        public UnityEngine.Events.UnityEvent OnLogoutSucceeded;
        public UnityEngine.Events.UnityEvent<string> OnLogoutFailed;
        
        public static PassportManager Instance { get; private set; }
        public Passport PassportInstance { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsLoggedIn { get; private set; }
        
        // UI Builder integration
        private PassportUIBuilder uiBuilder;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            // Find UI Builder if present
            uiBuilder = GetComponent<PassportUIBuilder>();
            
            // Configure UI elements if provided
            ConfigureUIElements();
            
            if (autoInitialize)
            {
                InitializePassport();
            }
        }
        
        /// <summary>
        /// Initialize Passport with the configured settings
        /// </summary>
        public async void InitializePassport()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[PassportManager] Passport is already initialized.");
                return;
            }
            
            if (string.IsNullOrEmpty(clientId) || clientId == "your-client-id-here")
            {
                string error = "Please set a valid Client ID in the PassportManager component";
                Debug.LogError($"[PassportManager] {error}");
                OnPassportError?.Invoke(error);
                return;
            }
            
            try
            {
                // Configure logging
                Passport.LogLevel = logLevel;
                Passport.RedactTokensInLogs = redactTokensInLogs;
                
                // Auto-configure redirect URIs if not set
                string finalRedirectUri = GetRedirectUri();
                string finalLogoutRedirectUri = GetLogoutRedirectUri();
                
                Debug.Log($"[PassportManager] Initializing Passport with Client ID: {clientId}");
                
                // Initialize Passport
                PassportInstance = await Passport.Init(clientId, environment, finalRedirectUri, finalLogoutRedirectUri);
                
                IsInitialized = true;
                Debug.Log("[PassportManager] Passport initialized successfully!");
                OnPassportInitialized?.Invoke();
                
                // Update UI state after initialization
                UpdateUIState();
                
                // Auto-login if enabled
                if (autoLogin)
                {
                    Debug.Log("[PassportManager] Auto-login enabled, attempting login...");
                    await LoginAsync();
                    var accessToken = await PassportInstance.GetAccessToken();
                    Debug.Log($"[PassportManager] Access token: {accessToken}");
                }
            }
            catch (Exception ex)
            {
                string error = $"Failed to initialize Passport: {ex.Message}";
                Debug.LogError($"[PassportManager] {error}");
                OnPassportError?.Invoke(error);
            }
        }
        
        /// <summary>
        /// Get the redirect URI - must be configured in Inspector
        /// </summary>
        private string GetRedirectUri()
        {
            if (string.IsNullOrEmpty(redirectUri))
            {
                throw new System.InvalidOperationException(
                    "Redirect URI must be configured in the PassportManager Inspector. " +
                    "Example: 'yourapp://callback'");
            }
            
            return redirectUri;
        }
        
        /// <summary>
        /// Get the logout redirect URI - must be configured in Inspector
        /// </summary>
        private string GetLogoutRedirectUri()
        {
            if (string.IsNullOrEmpty(logoutRedirectUri))
            {
                throw new System.InvalidOperationException(
                    "Logout Redirect URI must be configured in the PassportManager Inspector. " +
                    "Example: 'yourapp://logout'");
            }
            
            return logoutRedirectUri;
        }
        
        /// <summary>
        /// Quick access to login functionality using the configured direct login method
        /// </summary>
        public async void Login()
        {
            await LoginAsync();
        }
        
        /// <summary>
        /// Login with a specific direct login method
        /// </summary>
        /// <param name="loginMethod">The login method to use (Google, Apple, Facebook, or None for default)</param>
        public async void Login(DirectLoginMethod loginMethod)
        {
            await LoginAsync(loginMethod);
        }
        
        /// <summary>
        /// Internal async login method
        /// </summary>
        /// <param name="loginMethod">Optional login method override. If not provided, uses the configured directLoginMethod</param>
        private async UniTask LoginAsync(DirectLoginMethod? loginMethod = null)
        {
            if (!IsInitialized || PassportInstance == null)
            {
                Debug.LogError("[PassportManager] Passport not initialized. Call InitializePassport() first.");
                return;
            }
            
            try
            {
                DirectLoginMethod methodToUse = loginMethod ?? directLoginMethod;
                string loginMethodText = methodToUse == DirectLoginMethod.None 
                    ? "default method" 
                    : methodToUse.ToString();
                Debug.Log($"[PassportManager] Attempting login with {loginMethodText}...");
                
                bool loginSuccess = await PassportInstance.Login(useCachedSession: false, directLoginMethod: methodToUse);
                if (loginSuccess)
                {
                    IsLoggedIn = true;
                    Debug.Log("[PassportManager] Login successful!");
                    OnLoginSucceeded?.Invoke();
                    
                    // Update UI state after successful login
                    UpdateUIState();
                    
                    // Switch to logged-in panel if UI builder is present
                    if (uiBuilder != null)
                    {
                        uiBuilder.ShowLoggedInPanel();
                    }
                }
                else
                {
                    string failureMessage = "Login was cancelled or failed";
                    Debug.LogWarning($"[PassportManager] {failureMessage}");
                    OnLoginFailed?.Invoke(failureMessage);
                    
                    // Update UI state after failed login
                    UpdateUIState();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Login failed: {ex.Message}";
                Debug.LogError($"[PassportManager] {errorMessage}");
                OnLoginFailed?.Invoke(errorMessage);
            }
        }
        
        /// <summary>
        /// Quick access to logout functionality
        /// </summary>
        public async void Logout()
        {
            if (!IsInitialized || PassportInstance == null)
            {
                string errorMessage = "Passport not initialized";
                Debug.LogError($"[PassportManager] {errorMessage}");
                OnLogoutFailed?.Invoke(errorMessage);
                return;
            }
            
            try
            {
                await PassportInstance.Logout();
                IsLoggedIn = false;
                Debug.Log("[PassportManager] Logout successful!");
                OnLogoutSucceeded?.Invoke();
                
                // Update UI state after logout
                UpdateUIState();
                
                // Switch back to login panel if UI builder is present
                if (uiBuilder != null)
                {
                    uiBuilder.ShowLoginPanel();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Logout failed: {ex.Message}";
                Debug.LogError($"[PassportManager] {errorMessage}");
                OnLogoutFailed?.Invoke(errorMessage);
            }
        }
        
        #region UI Integration
        
        /// <summary>
        /// Configure UI elements if they have been assigned
        /// </summary>
        private void ConfigureUIElements()
        {
            // Set up button listeners (clear existing first to prevent duplicates)
            if (loginButton != null)
            {
                loginButton.onClick.RemoveAllListeners();
                loginButton.onClick.AddListener(() => Login());
                loginButton.interactable = IsInitialized && !IsLoggedIn;
                Debug.Log("[PassportManager] Configured login button");
            }
            
            if (googleLoginButton != null)
            {
                googleLoginButton.onClick.RemoveAllListeners();
                googleLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Google));
                googleLoginButton.interactable = IsInitialized && !IsLoggedIn;
                Debug.Log("[PassportManager] Configured Google login button");
            }
            
            if (appleLoginButton != null)
            {
                appleLoginButton.onClick.RemoveAllListeners();
                appleLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Apple));
                appleLoginButton.interactable = IsInitialized && !IsLoggedIn;
                Debug.Log("[PassportManager] Configured Apple login button");
            }
            
            if (facebookLoginButton != null)
            {
                facebookLoginButton.onClick.RemoveAllListeners();
                facebookLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Facebook));
                facebookLoginButton.interactable = IsInitialized && !IsLoggedIn;
                Debug.Log("[PassportManager] Configured Facebook login button");
            }
            
            if (logoutButton != null)
            {
                logoutButton.onClick.RemoveAllListeners();
                logoutButton.onClick.AddListener(() => Logout());
                logoutButton.interactable = IsInitialized && IsLoggedIn;
                Debug.Log("[PassportManager] Configured logout button");
            }
            
            // Update initial UI state
            UpdateUIState();
        }
        
        /// <summary>
        /// Update the state of UI elements based on current authentication status
        /// </summary>
        private void UpdateUIState()
        {
            bool isInitialized = IsInitialized;
            bool isLoggedIn = IsLoggedIn;
            
            // Update button states
            if (loginButton != null)
                loginButton.interactable = isInitialized && !isLoggedIn;
            if (googleLoginButton != null)
                googleLoginButton.interactable = isInitialized && !isLoggedIn;
            if (appleLoginButton != null)
                appleLoginButton.interactable = isInitialized && !isLoggedIn;
            if (facebookLoginButton != null)
                facebookLoginButton.interactable = isInitialized && !isLoggedIn;
            if (logoutButton != null)
                logoutButton.interactable = isInitialized && isLoggedIn;
            
            // Update status text (supports both Legacy Text and TextMeshPro)
            string statusMessage;
            Color statusColor;
            
            if (!isInitialized)
            {
                statusMessage = "Initializing Passport...";
                statusColor = Color.yellow;
            }
            else if (isLoggedIn)
            {
                statusMessage = "[LOGGED IN] Logged In";
                statusColor = Color.green;
            }
            else
            {
                statusMessage = "Ready to login";
                statusColor = Color.white;
            }
            
            SetStatusText(statusMessage, statusColor);
            
            // Update user info (supports both Legacy Text and TextMeshPro)
            if (isLoggedIn)
            {
                UpdateUserInfoDisplay();
            }
            else
            {
                SetUserInfoText("");
            }
        }
        
        /// <summary>
        /// Update the user info display with current user data
        /// </summary>
        private async void UpdateUserInfoDisplay()
        {
            if ((userInfoText != null || userInfoTextTMP != null) && PassportInstance != null)
            {
                try
                {
                    string accessToken = await PassportInstance.GetAccessToken();
                    string tokenPreview = accessToken.Length > 20 ? accessToken.Substring(0, 20) + "..." : accessToken;
                    string userInfo = $"Logged in (Token: {tokenPreview})";
                    SetUserInfoText(userInfo);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error loading user info: {ex.Message}";
                    SetUserInfoText(errorMessage);
                    Debug.LogWarning($"[PassportManager] Failed to load user info: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Set status text on both Legacy Text and TextMeshPro components
        /// </summary>
        private void SetStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            
            if (statusTextTMP != null)
            {
                statusTextTMP.text = message;
                statusTextTMP.color = color;
            }
        }
        
        /// <summary>
        /// Set user info text on both Legacy Text and TextMeshPro components
        /// </summary>
        private void SetUserInfoText(string message)
        {
            if (userInfoText != null)
            {
                userInfoText.text = message;
            }
            
            if (userInfoTextTMP != null)
            {
                userInfoTextTMP.text = message;
            }
        }
        
        #endregion
        
        #region UI Builder Integration
        
        /// <summary>
        /// Set UI references from the UI Builder (used internally)
        /// </summary>
        public void SetUIReferences(Button login, Button google, Button apple, Button facebook, Button logout, Text status, Text userInfo)
        {
            loginButton = login;
            googleLoginButton = google;
            appleLoginButton = apple;
            facebookLoginButton = facebook;
            logoutButton = logout;
            statusText = status;
            userInfoText = userInfo;
            
            // Re-configure UI elements with new references
            ConfigureUIElements();
        }
        
        #endregion
    }
} 