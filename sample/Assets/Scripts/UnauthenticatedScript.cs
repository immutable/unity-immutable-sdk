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
    [SerializeField] private Text userCodeText;
    [SerializeField] private Button proceedLoginButton;
    [SerializeField] private Button logoutButton;

    private Passport passport;
#pragma warning restore CS8618

    async void Start()
    {
        try
        {
            ShowOutput("Starting...");
            connectButton.gameObject.SetActive(false);
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);
            logoutButton.gameObject.SetActive(false);

            passport = await Passport.Init("ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj");

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
        }
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
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);

            ConnectResponse? response = await passport.Connect();

            if (response != null)
            {
                // Code confirmation required
                ShowOutput($"Code to verify: {response.code}");
                userCodeText.gameObject.SetActive(true);
                userCodeText.text = response.code;
                proceedLoginButton.gameObject.SetActive(true);
            }
            else
            {
                // No need to confirm code, log user straight in
                NavigateToAuthenticatedScene();
            }
        }
        catch (Exception ex)
        {
            PassportException passportException = ex as PassportException;
            string error;
            if (passportException != null && passportException.IsNetworkError())
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

    public async void ConfirmCode()
    {
        try
        {
            ShowOutput("Called ConfirmCode()...");
            await passport.ConfirmCode();
            ShowOutput("Confirmed code");
            NavigateToAuthenticatedScene();
        }
        catch (Exception ex)
        {
            ShowOutput($"ConfirmCode() error: {ex.Message}");
        }
    }

    public void CancelLogin()
    {
        ShowOutput("Login cancelled...");
        connectButton.gameObject.SetActive(true);
        userCodeText.gameObject.SetActive(false);
        proceedLoginButton.gameObject.SetActive(false);
        logoutButton.gameObject.SetActive(false);
    }

    public async void Logout()
    {
        await passport.Logout();
        logoutButton.gameObject.SetActive(false);
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
