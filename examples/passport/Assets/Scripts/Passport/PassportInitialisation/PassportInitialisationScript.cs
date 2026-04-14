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

        var redirectUri = "immutablerunner://callback";
        var logoutRedirectUri = "immutablerunner://logout";

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Debug;

            // Don't redact token values from logs
            Passport.RedactTokensInLogs = false;

            // Initialise Passport
            const string environment = Immutable.Passport.Model.Environment.SANDBOX;
            const string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
            // const string clientId = "IllW5pJ54DShXtaSXzaAlghm40uQjptd";
            var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
            SampleAppManager.PassportInstance = passport;

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
