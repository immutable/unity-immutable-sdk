using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;

public class SelectAuthMethodScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
    [SerializeField] private Button UseDeviceCodeAuthButton;
    [SerializeField] private Button UsePKCEButton;
    string REDIRECT_URI = null;
    string LOGOUT_REIDIRECT_URI = "https://www.immutable.com";
#pragma warning restore CS8618
    void Start()
    {
#if UNITY_WEBGL
    string url = Application.absoluteURL;
    Uri uri = new Uri(url);
    string scheme = uri.Scheme;
    string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
    string fullPath = uri.AbsolutePath.EndsWith("/") ? uri.AbsolutePath : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

    REDIRECT_URI = $"{scheme}://{hostWithPort}{fullPath}callback.html";
    LOGOUT_REIDIRECT_URI = $"{scheme}://{hostWithPort}{fullPath}logout.html";
#endif
    
        // Determine if PKCE is supported based on the platform
        SampleAppManager.SupportsPKCE = IsPKCESupported();

        // If PKCE is not supported, initialise Passport to use Device Code Auth
        if (!SampleAppManager.SupportsPKCE)
        {
            InitialisePassport(redirectUri: REDIRECT_URI, logoutRedirectUri: LOGOUT_REIDIRECT_URI);
        }
    }

    /// <summary>
    /// Checks if the current platform supports PKCE authentication.
    /// </summary>
    private bool IsPKCESupported()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Initialises Passport to use Device Code Auth
    /// </summary>
    public void UseDeviceCodeAuth()
    {
        SampleAppManager.UsePKCE = false;
        InitialisePassport(redirectUri: REDIRECT_URI, logoutRedirectUri: LOGOUT_REIDIRECT_URI);
    }

    /// <summary>
    /// Initialises Passport to use PKCE with the specified redirect URIs.
    /// </summary>
    public void UsePKCE()
    {
        SampleAppManager.UsePKCE = true;
        InitialisePassport(redirectUri: REDIRECT_URI, logoutRedirectUri: LOGOUT_REIDIRECT_URI);
    }

    /// <summary>
    /// Initialises Passport.
    /// </summary>
    /// <param name="redirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser after 
    /// authorisation has been granted by the user</param>
    /// <param name="logoutRedirectUri">The URL to which auth will redirect the browser
    /// after log out is complete</param>
    private async void InitialisePassport(string redirectUri = null, string logoutRedirectUri = null)
    {
        ShowOutput("Initialising Passport...");

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Info;

            // Initialise Passport
            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;

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
