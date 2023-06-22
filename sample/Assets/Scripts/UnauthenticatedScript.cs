using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

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

            passport = await Passport.Init();
            connectButton.gameObject.SetActive(true);
            if (passport.HasCredentialsSaved())
            {
                logoutButton.gameObject.SetActive(true);
            }
            ShowOutput("Ready");
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

            string? code = await passport.Connect();

            if (code != null)
            {
                // Code confirmation required
                ShowOutput($"Code to verify: {code}");
                userCodeText.gameObject.SetActive(true);
                userCodeText.text = code;
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
            string error = $"Connect() error: {ex.Message}";
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

    public void Logout()
    {
        passport.Logout();
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
