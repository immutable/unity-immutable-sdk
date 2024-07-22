using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class SelectAuthMethodScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
    [SerializeField] private Toggle UseDeviceCodeAuthToggle;
    [SerializeField] private Toggle UsePKCEToggle;
#pragma warning restore CS8618

    void Start()
    {
        SetupPadding();

        // Determine if PKCE is supported based on the platform
        SampleAppManager.SupportsPKCE = IsPKCESupported();

        // Set up auth based on PKCE support
        if (SampleAppManager.SupportsPKCE)
        {
            ConfigureAuthOptions();
        }
        else
        {
            InitialisePassport();
        }
    }

    /// <summary>
    /// Checks if the current platform supports PKCE authentication.
    /// </summary>
    private bool IsPKCESupported()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Configures auth options by setting up listeners for the Device Code Auth and PKCE toggles 
    /// to handle changes in the authentication method.
    /// </summary>
    private void ConfigureAuthOptions()
    {
        // Set up Device Code Auth toggle
        UseDeviceCodeAuthToggle.onValueChanged.AddListener(delegate (bool on)
        {
            SampleAppManager.UsePKCE = !on;
            InitialisePassport();
        });

        // Set up PKCE toggle
        UsePKCEToggle.onValueChanged.AddListener(delegate (bool on)
        {
            SampleAppManager.UsePKCE = on;
            InitialisePassport(redirectUri: "imxsample://callback", logoutRedirectUri: "imxsample://callback/logout");
        });
    }

    /// <summary>
    /// Initialises Passport.
    /// </summary>
    /// <param name="redirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser after 
    /// authorisation has been granted by the user</param>
    /// <param name="logoutRedirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser
    /// after log out is complete</param>
    private async void InitialisePassport(string redirectUri = null, string logoutRedirectUri = null)
    {
        ShowOutput("Initialising Passport...");

        try
        {
            // Initialise Passport
            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;

            Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);

            // Navigate to the unauthenticated scene after initialising Passport
            SceneManager.LoadScene("UnauthenticatedScene");
        }
        catch (Exception ex)
        {
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

    /// <summary>
    /// Adds top padding to the scene when running on an iPhone to accommodate notches that may obstruct the UI.
    /// </summary>
    private void SetupPadding()
    {
#if UNITY_IPHONE && !UNITY_EDITOR
    TopPadding.gameObject.SetActive(true);
#else
        TopPadding.gameObject.SetActive(false);
#endif
    }
}
