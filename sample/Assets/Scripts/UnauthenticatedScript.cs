using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Auth;
using System.Threading;

public class UnauthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Text userCodeText;
    [SerializeField] private Button proceedLoginButton;
    [SerializeField] private Button cancelLoginButton;
    [SerializeField] private Button logoutButton;

    private Passport passport;
    private CancellationTokenSource? loginTokenSource = null;
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
            cancelLoginButton.gameObject.SetActive(false);

            passport = await Passport.Init("ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj");
            connectButton.gameObject.SetActive(true);
            bool hasCredsSaved = await passport.HasCredentialsSaved();
            if (hasCredsSaved)
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
            string error = $"Connect() error: {ex.Message}";
            Debug.Log(error);
            ShowOutput(error);
            // Restart everything
            await passport.Logout();
        }
    }

    public async void ConfirmCode()
    {
        try
        {
            loginTokenSource = new CancellationTokenSource();
            cancelLoginButton.gameObject.SetActive(true);
            ShowOutput("Called ConfirmCode()...");
            await passport.ConfirmCode(token: loginTokenSource.Token);
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
        loginTokenSource?.Cancel();
        connectButton.gameObject.SetActive(true);
        userCodeText.gameObject.SetActive(false);
        proceedLoginButton.gameObject.SetActive(false);
        logoutButton.gameObject.SetActive(false);
        cancelLoginButton.gameObject.SetActive(false);
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
