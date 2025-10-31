using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

/// <summary>
/// Test script for Apple Sign-in with Passport
/// Tests the full LoginWithApple() flow on iOS
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
#pragma warning restore CS8618

    void Start()
    {
        Log("Apple Sign-in Test Scene started");
        
        // Get Passport instance
        if (Passport.Instance != null)
        {
            passport = Passport.Instance;
            Log("SUCCESS: Passport instance found");
        }
        else
        {
            Log("ERROR: Passport instance is null - Passport must be initialized before using Apple Sign In");
            UpdateStatus("ERROR: Passport not initialized");
        }

        // Set up button listener
        if (appleSignInButton != null)
        {
            appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
            Log("SUCCESS: Apple Sign In button listener added");
        }
        else
        {
            Log("ERROR: Apple Sign In button not assigned");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        // Platform detection info
        Log($"Platform: {Application.platform}");
        Log($"iOS Device: {Application.platform == RuntimePlatform.IPhonePlayer}");
        Log($"Editor: {Application.isEditor}");
        
#if UNITY_IOS && !UNITY_EDITOR
        Log("SUCCESS: Running on iOS device - Apple Sign In available");
        UpdateStatus("Ready to test Apple Sign In");
#else
        Log("WARNING: Not on iOS device - Apple Sign In only available on iOS");
        UpdateStatus("iOS device required for Apple Sign In");
#endif
    }

    private async void OnAppleSignInClicked()
    {
        Log("Apple Sign In button clicked!");
        UpdateStatus("Starting Apple Sign In...");

#if UNITY_IOS && !UNITY_EDITOR
        // iOS Device: Use Passport.LoginWithApple()
        if (passport == null)
        {
            Log("ERROR: Passport is null, cannot login");
            UpdateStatus("ERROR: Passport not initialized");
            return;
        }

        try
        {
            Log("Starting Apple Sign-in with Passport...");
            Log("Backend URL determined by Passport environment configuration");
            
            bool success = await passport.LoginWithApple();

            if (success)
            {
                Log("SUCCESS: Apple Sign-in completed successfully!");
                Log("SUCCESS: User is now authenticated with Passport!");
                UpdateStatus("Login Successful!");
                
                // Get user details
                try
                {
                    var address = await passport.GetAddress();
                    var email = await passport.GetEmail();
                    Log($"Email: {email}");
                    Log($"Wallet Address: {address}");
                }
                catch (Exception ex)
                {
                    Log($"WARNING: Could not fetch user details: {ex.Message}");
                }
                
                // Wait a moment then navigate to authenticated scene
                await System.Threading.Tasks.Task.Delay(3000);
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                Log("ERROR: Apple Sign-in failed");
                UpdateStatus("Login Failed");
            }
        }
        catch (OperationCanceledException)
        {
            Log("WARNING: User cancelled Apple Sign-in");
            UpdateStatus("Login Cancelled");
        }
        catch (PassportException ex)
        {
            Log($"ERROR: Passport exception: {ex.Message}");
            Log($"   Error Type: {ex.Type}");
            UpdateStatus($"ERROR: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Unexpected exception: {ex.Message}");
            Log($"   Stack trace: {ex.StackTrace}");
            UpdateStatus($"ERROR: {ex.Message}");
        }
#else
        // Not on iOS device - Fall back to regular Passport.Login()
        Log("WARNING: Not on iOS device - using Passport.Login() fallback");
        
        if (passport == null)
        {
            Log("ERROR: Passport is null, cannot login");
            UpdateStatus("ERROR: Passport not initialized");
            return;
        }

        try
        {
            Log("Creating DirectLoginOptions for Apple...");
            var directLoginOptions = new DirectLoginOptions(DirectLoginMethod.Apple);
            
            Log("Calling Passport.Login()...");
            bool success = await passport.Login(useCachedSession: false, directLoginOptions: directLoginOptions);

            if (success)
            {
                Log("SUCCESS: Login successful!");
                UpdateStatus("Login Successful!");
                
                // Wait a moment then load authenticated scene
                await System.Threading.Tasks.Task.Delay(2000);
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                Log("ERROR: Login failed");
                UpdateStatus("Login Failed");
            }
        }
        catch (OperationCanceledException ex)
        {
            Log($"WARNING: Login cancelled: {ex.Message}");
            UpdateStatus("Login Cancelled");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Login exception: {ex.Message}");
            UpdateStatus($"ERROR: {ex.Message}");
        }
#endif
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
    }
}

