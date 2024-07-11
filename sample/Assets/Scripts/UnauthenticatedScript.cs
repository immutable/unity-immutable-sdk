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
    [SerializeField] private Text SelectLoginMethod;
    [SerializeField] private Toggle UseDeviceCodeAuthToggle;
    [SerializeField] private Toggle UsePKCEToggle;
    [SerializeField] private InputField DeviceCodeTimeoutMs;

    private Passport passport;
#pragma warning restore CS8618

    void Start()
    {
        Debug.Log("Starting...");
        if (!SampleAppManager.InitialisedPassport)
        {
            // Set up login method toggles
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            // Allow users to select which login method
            Debug.Log("");
            ShowSelectLoginMethod(true);
            UseDeviceCodeAuthToggle.onValueChanged.AddListener(delegate (bool on)
            {
                SampleAppManager.UsePKCE = !on;
                ShowSelectLoginMethod(false);
                InitialisePassport();
            });
            UsePKCEToggle.onValueChanged.AddListener(delegate (bool on)
            {
                SampleAppManager.UsePKCE = on;
                ShowSelectLoginMethod(false);
                InitialisePassport();
            });
#else
            // Users cannot select which login method as only device code auth is supported
            ShowSelectLoginMethod(false);
            InitialisePassport();
#endif
        }
        else
        {
            // This is called if user logged out from the Authenticated Scene
            ShowSelectLoginMethod(false);
            ReloginButton.gameObject.SetActive(false);
            ReconnectButton.gameObject.SetActive(false);
            LoginButton.gameObject.SetActive(true);
            ConnectButton.gameObject.SetActive(true);
            DeviceCodeTimeoutMs.gameObject.SetActive(!SampleAppManager.UsePKCE);
            passport = Passport.Instance;
        }
    }

    private void ShowSelectLoginMethod(bool show)
    {
        SelectLoginMethod.gameObject.SetActive(show);
        UseDeviceCodeAuthToggle.gameObject.SetActive(show);
        UsePKCEToggle.gameObject.SetActive(show);
    }

    private async void InitialisePassport()
    {
        try
        {
            ShowOutput("Initilising Passport");

            // Initiliase Passport
            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string redirectUri = SampleAppManager.UsePKCE ? "imxsample://callback" : null;
            string logoutRedirectUri = SampleAppManager.UsePKCE ? "imxsample://callback/logout" : null;

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
            DeviceCodeTimeoutMs.gameObject.SetActive(!hasCredsSaved && !SampleAppManager.UsePKCE);

            SampleAppManager.InitialisedPassport = true;
            ShowOutput("Ready");
        }
        catch (Exception ex)
        {
            ShowOutput($"Initialise Passport error: {ex.Message}");
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
            Nullable<long> timeoutMs = GetDeviceCodeTimeoutMs(); ;
            ShowOutput($"Called Login() (timeout: {(timeoutMs != null ? timeoutMs.ToString() + "ms" : "none")})...");

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            if (SampleAppManager.UsePKCE)
            {
                await passport.LoginPKCE();
            }
            else
            {
                await passport.Login(timeoutMs: timeoutMs);
            }
#else
            await passport.Login(timeoutMs: timeoutMs);
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
                await Logout();
            }

            Debug.Log(error);
            ShowOutput(error);
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
                ClearStorageAndCache();
                ShowOutput($"Could not login using saved credentials");
            }
        }
        catch (Exception ex)
        {
            ClearStorageAndCache();
            ShowOutput($"Relogin() error: {ex.Message}");
        }
    }

    public async void Connect()
    {
        try
        {
            Nullable<long> timeoutMs = GetDeviceCodeTimeoutMs();
            ShowOutput($"Called Connect() (timeout: {(timeoutMs != null ? timeoutMs.ToString() + "ms" : "none")})...");

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            if (SampleAppManager.UsePKCE)
            {
                await passport.ConnectImxPKCE();
            }
            else
            {
                await passport.ConnectImx(timeoutMs: timeoutMs);
            }
#else
            await passport.ConnectImx(timeoutMs: timeoutMs);
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
                await Logout();
            }

            Debug.Log(error);
            ShowOutput(error);
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
                ClearStorageAndCache();
                ShowOutput($"Could not connect using saved credentials");
            }
        }
        catch (Exception ex)
        {
            ClearStorageAndCache();
            ShowOutput($"Reconnect() error: {ex.Message}");
        }
    }

    private async UniTask Logout()
    {
        try
        {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            if (SampleAppManager.UsePKCE)
            {
                await passport.LogoutPKCE();
            }
            else
            {
                await passport.Logout();
            }
#else
            await passport.Logout();
#endif
        }
        catch (Exception ex)
        {
            ShowOutput($"Logout() error: {ex.Message}");
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

    private Nullable<long> GetDeviceCodeTimeoutMs()
    {
        return String.IsNullOrEmpty(DeviceCodeTimeoutMs.text) ? null : long.Parse(DeviceCodeTimeoutMs.text);
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
