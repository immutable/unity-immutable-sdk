using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;
using Cysharp.Threading.Tasks;

/// <summary>
/// Sample script demonstrating how to initialize Passport using the PassportUI prefab.
/// This is the recommended approach for quick setup - configure Passport settings
/// in the PassportUI Inspector, then call InitializeWithPassport().
///
/// For advanced scenarios without UI, see PassportInitialisationScript.cs instead.
/// </summary>
public class PassportInitialisationWithUIScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
#pragma warning restore CS8618

    private PassportUI passportUI;

    void Start()
    {
        InitialisePassport();
    }

    private async void InitialisePassport()
    {
        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Debug;

            // Don't redact token values from logs
            Passport.RedactTokensInLogs = false;

            // Find PassportUI component
            passportUI = FindObjectOfType<PassportUI>();
            if (passportUI == null)
            {
                Debug.LogError("PassportUI component not found in scene - UI login will not be available");
                return;
            }

            // Subscribe to login events for automatic scene transition
            PassportUI.OnLoginSuccessStatic += OnPassportLoginSuccess;
            PassportUI.OnLoginFailureStatic += OnPassportLoginFailure;

            // Check if Passport is already initialized (e.g., from logout flow)
            if (Passport.Instance != null)
            {
                Debug.Log("Passport already initialized, setting up UI only...");

                // Just initialize the UI with the existing Passport instance
                await passportUI.InitializeWithPassport(Passport.Instance);

                // Store reference for other scripts that need it
                SampleAppManager.PassportInstance = Passport.Instance;

                Debug.Log("PassportUI initialized with existing Passport instance");
            }
            else
            {
                Debug.Log("Initializing Passport using PassportUI prefab configuration...");

                // PassportUI handles both Passport.Init() and UI setup
                // Configuration is done in the PassportUI Inspector (clientId, environment, etc.)
                await passportUI.InitializeWithPassport();

                // Store reference for other scripts that need it
                SampleAppManager.PassportInstance = Passport.Instance;

                Debug.Log("Passport and PassportUI initialized successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }

    private void OnPassportLoginSuccess()
    {
        Debug.Log("Passport login successful! Navigating to authenticated scene...");

        // Navigate to authenticated scene
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void OnPassportLoginFailure(string errorMessage)
    {
        Debug.LogError($"Passport login failed: {errorMessage}");

        // Could show error UI, retry options, etc.
        // For now, just log the error
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions to prevent memory leaks
        if (passportUI != null)
        {
            PassportUI.OnLoginSuccessStatic -= OnPassportLoginSuccess;
            PassportUI.OnLoginFailureStatic -= OnPassportLoginFailure;
        }
    }
}
