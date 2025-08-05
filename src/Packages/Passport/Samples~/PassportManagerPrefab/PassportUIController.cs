using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Immutable.Passport;

/// <summary>
/// UI Controller that can be wired up to PassportManager events through the Inspector.
/// This demonstrates the Inspector-based event handling approach.
/// 
/// ⚠️ IMPORTANT: This controller uses aggressive cursor management for demo purposes.
/// It continuously unlocks the cursor to ensure UI remains clickable. For production games,
/// review and customize the cursor behavior to match your game's requirements.
/// </summary>
public class PassportUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Button to trigger login")]
    public Button loginButton;
    
    [Tooltip("Button to trigger logout")]
    public Button logoutButton;
    
    [Header("Text Components (use Legacy Text OR TextMeshPro)")]
    [Tooltip("Legacy Text component to show current status")]
    public Text statusText;
    
    [Tooltip("TextMeshPro component to show current status (alternative to Legacy Text)")]
    public TextMeshProUGUI statusTextTMP;
    
    [Tooltip("Legacy Text component to show user information")]
    public Text userInfoText;
    
    [Tooltip("TextMeshPro component to show user information (alternative to Legacy Text)")]
    public TextMeshProUGUI userInfoTextTMP;
    
    [Header("Settings")]
    [Tooltip("Automatically manage button states based on authentication")]
    public bool autoManageButtons = true;
    
    [Tooltip("⚠️ DEMO FEATURE: Aggressively keeps cursor unlocked for demo purposes. Disable for production games that need cursor control.")]
    public bool forceCursorAlwaysAvailable = true;
    
    [Tooltip("Show detailed debug logs (enable for troubleshooting)")]
    public bool debugLogging = false;
    
    void Start()
    {
        // Set up button listeners
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        }
        
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);
        }
        
        // Initial UI update
        UpdateUIState();
        
        // Start continuous cursor management if enabled
        if (forceCursorAlwaysAvailable)
        {
            InvokeRepeating(nameof(EnsureCursorAvailable), 1.0f, 1.0f);
        }
        
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] Started and ready");
        }

        FixCursorState();
    }
    
    void OnDestroy()
    {
        // Cancel continuous cursor management
        CancelInvoke(nameof(EnsureCursorAvailable));
    }
    
    #region Button Handlers
    
    void OnLoginButtonClicked()
    {
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] Login button clicked");
        }
        
        var manager = PassportManager.Instance;
        if (manager != null && manager.IsInitialized)
        {
            SetStatus("Logging in...", Color.yellow);
            Debug.Log("[PassportUIController] Calling PassportManager.Login()...");
            Debug.Log($"[PassportUIController] Manager state - IsLoggedIn: {manager.IsLoggedIn}, PassportInstance: {(manager.PassportInstance != null ? "Valid" : "NULL")}");
            manager.Login();
        }
        else
        {
            SetStatus("[ERROR] Passport not ready", Color.red);
            Debug.Log($"[PassportUIController] Cannot login - Manager: {(manager != null ? "Found" : "NULL")}, Initialized: {(manager?.IsInitialized ?? false)}");
        }
    }
    
    void OnLogoutButtonClicked()
    {
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] Logout button clicked");
        }
        
        var manager = PassportManager.Instance;
        if (manager != null && manager.IsInitialized)
        {
            SetStatus("Logging out...", Color.yellow);
            Debug.Log("[PassportUIController] Calling PassportManager.Logout()...");
            manager.Logout();
        }
        else
        {
            SetStatus("[ERROR] Passport not ready", Color.red);
            Debug.Log($"[PassportUIController] Cannot logout - Manager: {(manager != null ? "Found" : "NULL")}, Initialized: {(manager?.IsInitialized ?? false)}");
        }
    }
    
    #endregion
    
    #region Event Handlers (Wire these up in PassportManager Inspector!)
    
    /// <summary>
    /// Call this from PassportManager's OnPassportInitialized event
    /// </summary>
    public void OnPassportInitialized()
    {
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] 🎉 Passport initialized!");
        }
        
        SetStatus("[READY] Passport ready", Color.green);
        UpdateUIState();
        
        // Fix cursor state after initialization in case it was left in a bad state
        FixCursorState();
    }
    
    /// <summary>
    /// Call this from PassportManager's OnPassportError event
    /// </summary>
    public void OnPassportError(string error)
    {
        if (debugLogging)
        {
            Debug.LogError($"[PassportUIController] ❌ Passport error: {error}");
        }
        
        SetStatus($"[ERROR] {error}", Color.red);
        UpdateUIState();
    }
    
    /// <summary>
    /// Call this from PassportManager's OnLoginSucceeded event
    /// </summary>
    public void OnLoginSucceeded()
    {
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] 🎉 Login successful!");
        }
        
        SetStatus("[SUCCESS] Logged in successfully!", Color.green);
        UpdateUIState();
        LoadUserInfo();
        
        // Fix cursor issues after authentication
        FixCursorState();
    }
    
    /// <summary>
    /// Call this from PassportManager's OnLoginFailed event
    /// </summary>
    public void OnLoginFailed(string error)
    {
        if (debugLogging)
        {
            Debug.LogWarning($"[PassportUIController] ⚠️ Login failed: {error}");
        }
        
        SetStatus($"[FAILED] Login failed: {error}", Color.red);
        UpdateUIState();
        
        // Fix cursor issues after failed authentication
        FixCursorState();
    }
    
    /// <summary>
    /// Call this from PassportManager's OnLogoutSucceeded event
    /// </summary>
    public void OnLogoutSucceeded()
    {
        if (debugLogging)
        {
            Debug.Log("[PassportUIController] 👋 Logout successful!");
        }
        
        SetStatus("[LOGGED OUT] Logged out", Color.blue);
        UpdateUIState();
        ClearUserInfo();
    }
    
    /// <summary>
    /// Call this from PassportManager's OnLogoutFailed event
    /// </summary>
    public void OnLogoutFailed(string error)
    {
        if (debugLogging)
        {
            Debug.LogError($"[PassportUIController] ❌ Logout failed: {error}");
        }
        
        SetStatus($"[ERROR] Logout failed: {error}", Color.red);
        UpdateUIState();
    }
    
    #endregion
    
    #region UI Management
    
    void UpdateUIState()
    {
        if (!autoManageButtons) return;
        
        var manager = PassportManager.Instance;
        bool isInitialized = manager != null && manager.IsInitialized;
        bool isLoggedIn = manager != null && manager.IsLoggedIn;
        
        if (loginButton != null)
        {
            loginButton.interactable = isInitialized && !isLoggedIn;
            if (debugLogging)
            {
                Debug.Log($"[PassportUIController] Login button: interactable={loginButton.interactable}");
            }
        }
        
        if (logoutButton != null)
        {
            logoutButton.interactable = isInitialized && isLoggedIn;
            if (debugLogging)
            {
                Debug.Log($"[PassportUIController] Logout button: interactable={logoutButton.interactable}");
            }
        }
    }
    
    void SetStatus(string message, Color color)
    {
        // Update Legacy Text
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        
        // Update TextMeshPro
        if (statusTextTMP != null)
        {
            statusTextTMP.text = message;
            statusTextTMP.color = color;
        }
        
        if (debugLogging)
        {
            Debug.Log($"[PassportUIController] Status: {message}");
        }
    }
    
    async void LoadUserInfo()
    {
        var manager = PassportManager.Instance;
        if (manager?.PassportInstance == null || (userInfoText == null && userInfoTextTMP == null))
            return;
        
        try
        {
            string accessToken = await manager.PassportInstance.GetAccessToken();
            string tokenPreview = accessToken.Length > 20 ? accessToken.Substring(0, 20) + "..." : accessToken;
            string userInfo = $"Logged in (Token: {tokenPreview})";
            SetUserInfo(userInfo);
            
            if (debugLogging)
            {
                Debug.Log($"[PassportUIController] User info loaded with access token");
            }
        }
        catch (System.Exception ex)
        {
            string errorInfo = $"Error loading user info: {ex.Message}";
            SetUserInfo(errorInfo);
            Debug.LogError($"[PassportUIController] Failed to load user info: {ex.Message}");
        }
    }
    
    void ClearUserInfo()
    {
        SetUserInfo("");
    }
    
    void SetUserInfo(string message)
    {
        // Update Legacy Text
        if (userInfoText != null)
        {
            userInfoText.text = message;
        }
        
        // Update TextMeshPro
        if (userInfoTextTMP != null)
        {
            userInfoTextTMP.text = message;
        }
    }
    
    void FixCursorState()
    {
        // Fix common cursor issues after authentication
        try
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (debugLogging)
            {
                Debug.Log("[PassportUIController] Cursor state restored");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PassportUIController] Could not fix cursor state: {ex.Message}");
        }
    }
    
    void EnsureCursorAvailable()
    {
        // ⚠️ AGGRESSIVE CURSOR MANAGEMENT FOR DEMO PURPOSES
        // This method forcibly unlocks the cursor every second to ensure demo UI remains clickable.
        // For production games: Disable 'forceCursorAlwaysAvailable' and implement custom cursor logic.
        try
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                if (debugLogging)
                {
                    Debug.Log("[PassportUIController] Cursor state corrected (was locked or invisible)");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PassportUIController] Could not ensure cursor availability: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Debug Helpers
    
    // Debug panel (only shows when debug logging is enabled)
    void OnGUI()
    {
        if (!debugLogging) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 200, 150));
        GUILayout.Box("Passport Debug Panel");
        
        var manager = PassportManager.Instance;
        if (manager != null)
        {
            GUILayout.Label($"Initialized: {manager.IsInitialized}");
            GUILayout.Label($"Logged In: {manager.IsLoggedIn}");
            
            if (GUILayout.Button("Test Login"))
            {
                OnLoginButtonClicked();
            }
            
            if (GUILayout.Button("Test Logout"))
            {
                OnLogoutButtonClicked();
            }
        }
        else
        {
            GUILayout.Label("PassportManager: NULL");
        }
        
        GUILayout.EndArea();
    }
    
    #endregion
}