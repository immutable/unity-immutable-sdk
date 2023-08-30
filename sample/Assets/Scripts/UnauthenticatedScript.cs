using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class UnauthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Button tryAgainButton;

    private Passport passport;
#pragma warning restore CS8618

    async void Start()
    {
        try
        {
            ShowOutput("Starting...");
            connectButton.gameObject.SetActive(false);
            tryAgainButton.gameObject.SetActive(false);

            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string? redirectUri = null;
#if UNITY_IPHONE || UNITY_ANDROID
            redirectUri = "imxsample://callback";
#endif

            passport = await Passport.Init(
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                clientId, environment, redirectUri, 10000
#else
                clientId, environment, redirectUri
#endif
                );

            // Check if user's logged in before
            bool hasCredsSaved = await passport.HasCredentialsSaved();
            if (hasCredsSaved)
            {
                // Use existing credentials to connect to Passport
                ShowOutput("Connecting to Passport using saved credentials...");

                bool connected = await passport.ConnectSilent();
                if (connected)
                {
                    // Successfully connected to Passport
                    NavigateToAuthenticatedScene();
                }
                else
                {
                    // Could not connect to Passport, enable connect button
                    connectButton.gameObject.SetActive(true);
                }
            }
            else
            {
                // No existing credentials to use to connect
                ShowOutput("Ready");
                // Enable connect button
                connectButton.gameObject.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Start() error: {ex.Message}");
            tryAgainButton.gameObject.SetActive(true);
        }
    }

    public void OnTryAgain()
    {
        Start();
    }

#pragma warning disable IDE0051
    private void OnReady()
    {
        ShowOutput("Passport is ready");
        connectButton.gameObject.SetActive(true);
    }
#pragma warning restore IDE0051

    public async void Connect()
    {
        try
        {
            ShowOutput("Called Connect()...");
            connectButton.gameObject.SetActive(false);

#if UNITY_ANDROID || UNITY_IPHONE
            await passport.ConnectPKCE();
#else
            await passport.Connect();
#endif

            NavigateToAuthenticatedScene();
        }
        catch (Exception ex)
        {
#if UNITY_ANDROID || UNITY_IPHONE
            connectButton.gameObject.SetActive(true);
#endif
            string error;
            if (ex is PassportException passportException && passportException.IsNetworkError())
            {
                error = $"Connect() error: Check your internet connection and try again";
            }
            else
            {
                error = $"Connect() error: {ex.Message}";
                // Restart everything
                await passport.Logout();
            }

            Debug.Log(error);
            ShowOutput(error);
        }
    }

    private void NavigateToAuthenticatedScene()
    {
        SceneManager.LoadScene(sceneName: "AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (output != null)
        {
            output.text = message;
        }
    }
}
