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
    string redirectUri = null;
    string logoutRedirectUri = "https://www.immutable.com";
    string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
#pragma warning restore CS8618
    void Start()
    {
#if UNITY_WEBGL
        string url = Application.absoluteURL;
        Uri uri = new Uri(url);
        string scheme = uri.Scheme;
        string hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
        string fullPath = uri.AbsolutePath.EndsWith("/") ? uri.AbsolutePath : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

        redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
        logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
        clientId = "UnB98ngnXIZIEJWGJOjVe1BpCx5ix7qc";
#endif
    
        // Determine if PKCE is supported based on the platform
        SampleAppManager.SupportsPKCE = IsPKCESupported();

        // If PKCE is not supported, initialise Passport to use Device Code Auth
        if (!SampleAppManager.SupportsPKCE)
        {
            InitialisePassport();
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
        InitialisePassport();
    }

    /// <summary>
    /// Initialises Passport to use PKCE with the specified redirect URIs.
    /// </summary>
    public void UsePKCE()
    {
        SampleAppManager.UsePKCE = true;
        InitialisePassport();
    }

    /// <summary>
    /// Initialises Passport.
    /// </summary>
    private async void InitialisePassport()
    {
        ShowOutput("Initialising Passport...");

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Info;

            // Initialise Passport
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
