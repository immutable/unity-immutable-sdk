using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;
using Immutable.Passport.Event;

public class UnauthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private Button LoginButton;
    [SerializeField] private Button ConnectButton;
    [SerializeField] private Button ReloginButton;
    [SerializeField] private Button ReconnectButton;

    private Passport passport;
#pragma warning restore CS8618

    async void Start()
    {
        try
        {
            ShowOutput("Starting...");
            LoginButton.gameObject.SetActive(false);
            ConnectButton.gameObject.SetActive(false);
            ReloginButton.gameObject.SetActive(false);
            ReconnectButton.gameObject.SetActive(false);

            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string redirectUri = null;
            string logoutRedirectUri = null;

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            redirectUri = "imxsample://callback";
            logoutRedirectUri = "imxsample://callback/logout";
#endif

            passport = await Passport.Init(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                clientId, environment, redirectUri, logoutRedirectUri, 10000
#else
                clientId, environment, redirectUri, logoutRedirectUri
#endif
                );

            // Listen to Passport Auth events
            passport.OnAuthEvent += OnPassportAuthEvent;

            // Check if user's logged in before
            bool hasCredsSaved = await passport.HasCredentialsSaved();
            Debug.Log(hasCredsSaved ? "Has credentials saved" : "Does not have credentials saved");
            ReloginButton.gameObject.SetActive(hasCredsSaved);
            ReconnectButton.gameObject.SetActive(hasCredsSaved);
            LoginButton.gameObject.SetActive(!hasCredsSaved);
            ConnectButton.gameObject.SetActive(!hasCredsSaved);

            ShowOutput("Ready");
        }
        catch (Exception ex)
        {
            ShowOutput($"Start() error: {ex.Message}");
        }
    }

    private void OnPassportAuthEvent(PassportAuthEvent authEvent)
    {
        Debug.Log($"OnPassportAuthEvent {authEvent.ToString()}");
    }

    public async void Login()
    {
        try
        {
            ShowOutput("Called Login()...");
            LoginButton.gameObject.SetActive(false);

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            await passport.LoginPKCE();
#else
            await passport.Login();
#endif

            SampleAppManager.IsConnected = false;
            NavigateToAuthenticatedScene();
        }
        catch (Exception ex)
        {
            string error;
            if (ex is PassportException passportException && passportException.IsNetworkError())
            {
                error = $"Login() error: Check your internet connection and try again";
            }
            else if (ex is OperationCanceledException)
            {
                error = "Login() cancelled";
            }
            else
            {
                error = $"Login() error: {ex.Message}";
                // Restart everything
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await passport.Logout();
#else
                await passport.LogoutPKCE();
#endif
            }

            Debug.Log(error);
            ShowOutput(error);
            LoginButton.gameObject.SetActive(true);
        }
    }

    public async void Relogin()
    {
        try
        {
            // Use existing credentials to log in to Passport
            ShowOutput("Logging into Passport using saved credentials...");
            ReloginButton.gameObject.SetActive(false);
            bool loggedIn = await passport.Login(useCachedSession: true);
            if (loggedIn)
            {
                SampleAppManager.IsConnected = false;
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput($"Could not login using saved credentials");
                ClearStorageAndCache();

            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Relogin() error: {ex.Message}");
            ClearStorageAndCache();
        }
    }

    public async void Connect()
    {
        try
        {
            ShowOutput("Called Connect()...");
            ConnectButton.gameObject.SetActive(false);

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            await passport.ConnectImxPKCE();
#else
            await passport.ConnectImx();
#endif

            SampleAppManager.IsConnected = true;
            NavigateToAuthenticatedScene();
        }
        catch (Exception ex)
        {
            string error;
            if (ex is PassportException passportException && passportException.IsNetworkError())
            {
                error = $"Connect() error: Check your internet connection and try again";
            }
            else if (ex is OperationCanceledException)
            {
                error = "Connect() cancelled";
            }
            else
            {
                error = $"Connect() error: {ex.Message}";
                // Restart everything
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await passport.Logout();
#else
                await passport.LogoutPKCE();
#endif
            }

            Debug.Log(error);
            ShowOutput(error);
            ConnectButton.gameObject.SetActive(true);
        }
    }

    public async void Reconnect()
    {
        try
        {
            // Use existing credentials to connect to Passport
            ShowOutput("Reconnecting into Passport using saved credentials...");
            ReconnectButton.gameObject.SetActive(false);
            bool connected = await passport.ConnectImx(useCachedSession: true);
            if (connected)
            {
                SampleAppManager.IsConnected = true;
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput($"Could not connect using saved credentials");
                ClearStorageAndCache();

            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Reconnect() error: {ex.Message}");
            ClearStorageAndCache();
        }
    }

    public void ClearStorageAndCache()
    {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        passport.ClearStorage();
        passport.ClearCache(true);
        ShowOutput("Cleared storage and cache");
#else
        ShowOutput("Support on Android and iOS devices only");
#endif
    }

    private void NavigateToAuthenticatedScene()
    {
        passport.OnAuthEvent -= OnPassportAuthEvent;
        SceneManager.LoadScene(sceneName: "AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        Debug.Log($"Output: {message}");
        if (Output != null)
        {
            Output.text = message;
        }
    }
}
