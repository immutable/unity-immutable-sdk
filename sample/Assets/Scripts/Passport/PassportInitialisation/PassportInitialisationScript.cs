using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;

public class PassportInitialisationScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
#pragma warning restore CS8618

    void Start()
    {
        InitialisePassport();
    }

    private async void InitialisePassport()
    {
        ShowOutput("Initialising Passport...");

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

            // Initialise Passport
            // Use "local" for testing against local BFF backend
            const string environment = "local";
            const string clientId = "2Dx7GLUZeFsMnmp1kvOXJ2SYaWGhEpnF";
            
            var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
            SampleAppManager.PassportInstance = passport;

            // Navigate to the unauthenticated scene after initialising Passport
            //SceneManager.LoadScene("UnauthenticatedScene");
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
