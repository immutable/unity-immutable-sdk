using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;

public class PassportInitializationScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
    [SerializeField] private Button UseDeviceCodeAuthButton;
    [SerializeField] private Button UsePKCEButton;
#pragma warning restore CS8618

    void Start()
    {
        // WebGL does not support Device Code Auth, so we'll use PKCE by default instead.
#if UNITY_WEBGL
        UsePKCE();
#endif
    }

    /// <summary>
    /// Initialises Passport to use Device Code Auth
    /// </summary>
    public void UseDeviceCodeAuth()
    {
        SampleAppManager.UsePKCE = false;
        InitialisePassport(logoutRedirectUri: "https://www.immutable.com");
    }

    /// <summary>
    /// Initialises Passport to use PKCE with the specified redirect URIs.
    /// </summary>
    public void UsePKCE()
    {
        SampleAppManager.UsePKCE = true;
#if UNITY_WEBGL
        string url = Application.absoluteURL;
        Uri uri = new Uri(url);
        string scheme = uri.Scheme;
        string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        string fullPath = uri.AbsolutePath.EndsWith("/") ? uri.AbsolutePath : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

        string redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
        string logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
        
        InitialisePassport(redirectUri: redirectUri, logoutRedirectUri: logoutRedirectUri);
#else
        InitialisePassport(redirectUri: "immutablerunner://callback", logoutRedirectUri: "immutablerunner://logout");
#endif
    }

    /// <summary>
    /// Initialises Passport.
    /// </summary>
    /// <param name="redirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser after 
    /// authorisation has been granted by the user</param>
    /// <param name="logoutRedirectUri">The URL to which auth will redirect the browser
    /// after log out is complete</param>
    private async void InitialisePassport(string? redirectUri = null, string? logoutRedirectUri = null)
    {
        ShowOutput("Initialising Passport...");

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Info;

            // Don't redact token values from logs
            Passport.RedactTokensInLogs = false;

            // Initialise Passport
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
            Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);

            // Navigate to the unauthenticated scene after initialising Passport
            SceneManager.LoadScene("UnauthenticatedScene");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            ShowOutput($"Initialise Passport error: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints the specified <code>message</code> to the output box.
    /// </summary>
    /// <param name="message">The message to print</param>
    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}
