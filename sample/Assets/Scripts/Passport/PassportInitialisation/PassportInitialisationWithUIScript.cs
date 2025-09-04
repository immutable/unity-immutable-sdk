using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;

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
        string redirectUri;
        string logoutRedirectUri;

#if UNITY_WEBGL
            var url = Application.absoluteURL;
            var uri = new Uri(url);
            var scheme = uri.Scheme;
            var hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var fullPath = uri.AbsolutePath.EndsWith("/")
                ? uri.AbsolutePath
                : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

            redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
            logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
#else
        redirectUri = "immutablerunner://callback";
        logoutRedirectUri = "immutablerunner://logout";
#endif

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Debug;

            // Don't redact token values from logs
            Passport.RedactTokensInLogs = false;

            // Initialise Passport with UI support enabled
            const string environment = Immutable.Passport.Model.Environment.SANDBOX;
            // const string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
            const string clientId = "IllW5pJ54DShXtaSXzaAlghm40uQjptd";
            var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
            SampleAppManager.PassportInstance = passport;

            // Find and initialize PassportUI at runtime
            passportUI = FindObjectOfType<PassportUI>();
            if (passportUI != null)
            {
                passportUI.Init(passport);
                Debug.Log("PassportUI found and initialized successfully");

                // Subscribe to login success event for automatic scene transition
                PassportUI.OnLoginSuccessStatic += OnPassportLoginSuccess;
                PassportUI.OnLoginFailureStatic += OnPassportLoginFailure;
            }
            else
            {
                Debug.LogWarning("PassportUI component not found in scene - UI login will not be available");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }

    private void OnPassportLoginSuccess()
    {
        Debug.Log("🎉 Passport login successful! Navigating to authenticated scene...");

        // Navigate to authenticated scene
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void OnPassportLoginFailure(string errorMessage)
    {
        Debug.LogError($"❌ Passport login failed: {errorMessage}");

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
