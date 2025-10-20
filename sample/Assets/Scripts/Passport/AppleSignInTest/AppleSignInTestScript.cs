using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;
#if UNITY_IOS && !UNITY_EDITOR
using Immutable.Passport.AppleSignIn;
#endif

/// <summary>
/// Test script for Apple Sign In functionality
/// Slice 2: Tests native plugin stub implementation
/// </summary>
public class AppleSignInTestScript : MonoBehaviour
{
#pragma warning disable CS8618
    [Header("UI Elements")]
    [SerializeField] private Button appleSignInButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text logText;
    [SerializeField] private Button backButton;
    
    private Passport passport;
    private string logOutput = "";
    private bool _waitingForNativeCallback = false;
#pragma warning restore CS8618

    void Start()
    {
        Log("AppleSignInTestScript started (Slice 2 - Native Plugin)");
        
        // Get Passport instance
        if (Passport.Instance != null)
        {
            passport = Passport.Instance;
            Log("✅ Passport instance found");
        }
        else
        {
            Log("⚠️ Passport instance is null (not needed for Slice 2 native test)");
        }

        // Initialize native plugin if on iOS device
#if UNITY_IOS && !UNITY_EDITOR
        Log("📱 Running on iOS device - initializing native plugin...");
        try
        {
            AppleSignInNative.Initialize();
            
            // Subscribe to native callbacks
            AppleSignInNative.OnSuccess += OnNativeSuccess;
            AppleSignInNative.OnError += OnNativeError;
            AppleSignInNative.OnCancel += OnNativeCancel;
            
            Log("✅ Native plugin initialized and callbacks registered");
            
            // Check availability
            bool available = AppleSignInNative.IsAvailable();
            Log($"Native Apple Sign In available: {available}");
            
            UpdateStatus("Ready to test Native Apple Sign In");
        }
        catch (Exception ex)
        {
            Log($"❌ Failed to initialize native plugin: {ex.Message}");
            UpdateStatus("ERROR: Native plugin failed to initialize");
        }
#else
        Log("⚠️ Not on iOS device - will fall back to Passport.Login()");
        UpdateStatus("Ready to test Apple Sign In (Passport flow)");
#endif

        // Set up button listeners
        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
            Log("✅ Apple Sign In button listener added");
        }
        else
        {
            Log("❌ Apple Sign In button not assigned");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        // Platform detection info
        Log($"Platform: {Application.platform}");
        Log($"iOS Device: {Application.platform == RuntimePlatform.IPhonePlayer}");
        Log($"Editor: {Application.isEditor}");
        
#if UNITY_IOS
        Log("✅ UNITY_IOS flag is defined");
#else
        Log("⚠️ UNITY_IOS flag is NOT defined");
#endif
    }

    private async void OnAppleSignInClicked()
    {
        Log("🍎 Apple Sign In button clicked!");
        UpdateStatus("Starting Apple Sign In...");

#if UNITY_IOS && !UNITY_EDITOR
        // iOS Device: Use native plugin
        Log("📱 Using native iOS plugin...");
        
        try
        {
            _waitingForNativeCallback = true;
            AppleSignInNative.Start();
            Log("✅ Native Apple Sign In flow started");
            Log("⏳ Waiting for native callback...");
            UpdateStatus("Waiting for Apple Sign In...");
        }
        catch (Exception ex)
        {
            Log($"❌ Native plugin exception: {ex.Message}");
            UpdateStatus($"ERROR: {ex.Message}");
            _waitingForNativeCallback = false;
        }
#else
        // Editor or other platforms: Fall back to Passport.Login()
        Log("⚠️ Not on iOS device - using Passport.Login() fallback");
        
        if (passport == null)
        {
            Log("❌ Passport is null, cannot login");
            UpdateStatus("ERROR: Passport not initialized");
            return;
        }

        try
        {
            Log("Creating DirectLoginOptions for Apple...");
            var directLoginOptions = new DirectLoginOptions(DirectLoginMethod.Apple);
            
            Log($"DirectLoginMethod: {directLoginOptions.directLoginMethod}");
            Log("Calling Passport.Login()...");
            
            bool success = await passport.Login(useCachedSession: false, directLoginOptions: directLoginOptions);

            if (success)
            {
                Log("✅ Login successful!");
                UpdateStatus("Login Successful!");
                
                // Wait a moment then load authenticated scene
                await System.Threading.Tasks.Task.Delay(2000);
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                Log("❌ Login failed");
                UpdateStatus("Login Failed");
            }
        }
        catch (OperationCanceledException ex)
        {
            Log($"⚠️ Login cancelled: {ex.Message}");
            UpdateStatus("Login Cancelled");
        }
        catch (Exception ex)
        {
            Log($"❌ Login exception: {ex.Message}");
            Log($"Stack trace: {ex.StackTrace}");
            UpdateStatus($"ERROR: {ex.Message}");
        }
#endif
    }

    // Native callback handlers (called from AppleSignInNative events)
    private void OnNativeSuccess(string identityToken, string authorizationCode, string userID, string email, string fullName)
    {
        Log("🎉 Native callback - SUCCESS!");
        Log($"✅ User ID: {userID}");
        Log($"✅ Email: {email}");
        Log($"✅ Full Name: {fullName}");
        Log($"✅ Identity Token: {identityToken.Substring(0, Math.Min(50, identityToken.Length))}...");
        Log($"✅ Authorization Code: {authorizationCode.Substring(0, Math.Min(50, authorizationCode.Length))}...");
        
        // Write full tokens to file for inspection
        WriteTokensToFile(identityToken, authorizationCode, userID, email, fullName);
        
        UpdateStatus("Native Apple Sign In Successful! ✅");
        _waitingForNativeCallback = false;
        
        Log("📝 For Slice 3, this data will be exchanged with backend for Passport session");
        Log($"📄 Full tokens written to: {System.IO.Path.Combine(Application.persistentDataPath, "apple_tokens.txt")}");
    }
    
    /// <summary>
    /// Writes full Apple Sign In tokens to a file (untruncated)
    /// </summary>
    private void WriteTokensToFile(string identityToken, string authorizationCode, string userID, string email, string fullName)
    {
        try
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "apple_tokens.txt");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string content = $@"=== Apple Sign In Tokens ===
Timestamp: {timestamp}

User ID: {userID}
Email: {email}
Full Name: {fullName}

=== Identity Token (JWT) ===
{identityToken}

=== Authorization Code ===
{authorizationCode}

=== File Location ===
{filePath}
";
            
            System.IO.File.WriteAllText(filePath, content);
            Debug.Log($"[AppleSignInTest] ✅ Full tokens written to file: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AppleSignInTest] ❌ Failed to write tokens to file: {ex.Message}");
        }
    }

    private void OnNativeError(string errorCode, string errorMessage)
    {
        Log($"❌ Native callback - ERROR!");
        Log($"   Error Code: {errorCode}");
        Log($"   Error Message: {errorMessage}");
        
        UpdateStatus($"Native Error: {errorMessage}");
        _waitingForNativeCallback = false;
    }

    private void OnNativeCancel()
    {
        Log("⚠️ Native callback - CANCELLED");
        Log("   User cancelled Apple Sign In");
        
        UpdateStatus("Apple Sign In Cancelled");
        _waitingForNativeCallback = false;
    }

    private void OnBackClicked()
    {
        Log("Back button clicked, returning to UnauthenticatedScene");
        SceneManager.LoadScene("UnauthenticatedScene");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Log($"[STATUS] {message}");
    }

    private void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string logMessage = $"[{timestamp}] {message}";
        
        Debug.Log($"[AppleSignInTest] {message}");
        
        logOutput += logMessage + "\n";
        
        if (logText != null)
        {
            logText.text = logOutput;
            
            // Auto-scroll to bottom (if scrollview exists)
            Canvas.ForceUpdateCanvases();
        }
    }

    void OnDestroy()
    {
        // Clean up button listeners
        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.RemoveListener(OnAppleSignInClicked);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }

        // Clean up native callbacks
#if UNITY_IOS && !UNITY_EDITOR
        AppleSignInNative.OnSuccess -= OnNativeSuccess;
        AppleSignInNative.OnError -= OnNativeError;
        AppleSignInNative.OnCancel -= OnNativeCancel;
        Log("✅ Native callbacks unregistered");
#endif
    }
}

