using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class UnauthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
    // Login buttons
    [SerializeField] private GameObject LoginButtons;
    // Re-login buttons
    [SerializeField] private GameObject ReloginButtons;
    [SerializeField] private InputField DeviceCodeTimeoutMs;

    private Passport Passport;
#pragma warning restore CS8618

    async void Start()
    {
        SetupPadding();

        // Get Passport instance
        Passport = Passport.Instance;

        // Check if the user has logged in before
        await CheckHasCredentialsSaved();
    }

    /// <summary>
    /// Checks if the user has logged in previously and updates the UI to display the appropriate buttons and fields.
    /// </summary>
    private async UniTask CheckHasCredentialsSaved()
    {
        bool hasCredsSaved = await Passport.HasCredentialsSaved();

        // Show re-login buttons if user has credentials saved
        ReloginButtons.SetActive(hasCredsSaved);

        // Show login buttons if user does not have any credentials saved
        LoginButtons.SetActive(!hasCredsSaved);

        // Only show timeout field if Device Code Auth is selected as the auth method and no credentials are saved
        DeviceCodeTimeoutMs.gameObject.SetActive(!hasCredsSaved && !SampleAppManager.UsePKCE);
    }

    /// <summary>
    /// Logs into Passport using the selected auth method. 
    /// Defaults to Device Code Auth when running as a Windows Standalone application or in the Unity Editor on Windows.
    /// </summary>
    public async void Login()
    {
        // Get timeout
        var timeoutMs = GetDeviceCodeTimeoutMs();
        string formattedTimeout = timeoutMs != null ? $"{timeoutMs} ms" : "none";

        ShowOutput($"Logging in (timeout: {formattedTimeout})...");

        try
        {
            // Login using the appropriate login method
            if (SampleAppManager.SupportsPKCE && SampleAppManager.UsePKCE)
            {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await Passport.LoginPKCE();
#endif
            }
            else
            {
                await Passport.Login(timeoutMs: timeoutMs);
            }

            // Navigate to the authenticated scene upon successful login
            NavigateToAuthenticatedScene(connectedToImx: false);
        }
        catch (OperationCanceledException)
        {
            ShowOutput("Failed to login: cancelled");
        }
        catch (Exception ex)
        {
            await Logout();
            ShowOutput($"Failed to login: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs into Passport using the selected auth method. 
    /// Defaults to Device Code Auth when running as a Windows Standalone application or in the Unity Editor on Windows.
    /// 
    /// This function also connects to IMX, which initialises the user's wallet and sets up the IMX provider.
    /// </summary>
    public async void Connect()
    {
        // Get timeout
        var timeoutMs = GetDeviceCodeTimeoutMs();
        string formattedTimeout = timeoutMs != null ? $"{timeoutMs} ms" : "none";

        ShowOutput($"Connecting (timeout: {formattedTimeout})...");

        try
        {
            // Login and connect to IMX using the appropriate connect method
            if (SampleAppManager.SupportsPKCE && SampleAppManager.UsePKCE)
            {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await Passport.ConnectImxPKCE();
#endif
            }
            else
            {
                await Passport.ConnectImx(timeoutMs: timeoutMs);
            }

            // Navigate to the authenticated scene upon successful login and connection to IMX
            NavigateToAuthenticatedScene(connectedToImx: true);
        }
        catch (OperationCanceledException)
        {
            ShowOutput("Failed to connect: cancelled");
        }
        catch (Exception ex)
        {
            await Logout();
            ShowOutput($"Failed to connect: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses the existing credentials to re-login to Passport.
    /// </summary>
    public async void Relogin()
    {
        ShowOutput("Re-logging into Passport using saved credentials...");

        try
        {
            bool loggedIn = await Passport.Login(useCachedSession: true);

            if (loggedIn)
            {
                // Navigate to the authenticated scene upon successful login
                NavigateToAuthenticatedScene(connectedToImx: false);
            }
            else
            {
                // Failed to re-login, so remove existing credentials and restart
                ClearStorageCacheAndRestart();
                ShowOutput("Could not re-login using saved credentials");
            }
        }
        catch (Exception ex)
        {
            // Failed to re-login, so remove existing credentials and restart
            ClearStorageCacheAndRestart();
            ShowOutput($"Failed to re-login: {ex.Message}");
        }
    }

    /// <summary>
    /// Uses existing credentials to re-login to Passport and reconnect to IMX. 
    /// The SDK initialises the user's wallet and sets up the IMX provider during reconnection.
    /// </summary>
    public async void Reconnect()
    {
        ShowOutput("Reconnecting to Passport using saved credentials...");

        try
        {
            bool connected = await Passport.ConnectImx(useCachedSession: true);

            if (connected)
            {
                // Navigate to the authenticated scene upon successful login and connection to IMX
                NavigateToAuthenticatedScene(connectedToImx: true);
            }
            else
            {
                // Failed to reconnect, so remove existing credentials and restart
                ClearStorageCacheAndRestart();
                ShowOutput("Could not reconnect using saved credentials");
            }
        }
        catch (Exception ex)
        {
            // Failed to reconnect, so remove existing credentials and restart
            ClearStorageCacheAndRestart();
            ShowOutput($"Failed to reconnect: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs out of Passport using the selected auth method. 
    /// Defaults to Device Code Auth when running as a Windows Standalone application or in the Unity Editor on Windows.
    /// </summary>
    private async UniTask Logout()
    {
        try
        {
            // Logout using the appropriate logout method
            if (SampleAppManager.SupportsPKCE && SampleAppManager.UsePKCE)
            {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await Passport.LogoutPKCE();
#endif
            }
            else
            {
                await Passport.Logout();
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to logout: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the underlying WebView storage and cache, including any saved credentials.
    /// </summary>
    public void ClearStorageAndCache()
    {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
    Passport.ClearStorage();
    Passport.ClearCache(true);

    // Show login buttons as saved credentials are removed
    ShowLoginButtons();
    ShowOutput("Cleared storage and cache");
#else
        ShowOutput("Available for Android and iOS devices only");
#endif
    }

    /// <summary>
    /// Clears the WebView storage and cache, then updates the UI to show login-related buttons and fields.
    /// </summary>
    private void ClearStorageCacheAndRestart()
    {
        ClearStorageAndCache();
        ShowLoginButtons();
    }

    /// <summary>
    /// Updates the UI to show login buttons and fields, hiding relogin buttons.
    /// </summary>
    private void ShowLoginButtons()
    {
        ReloginButtons.SetActive(false);
        LoginButtons.SetActive(true);
        DeviceCodeTimeoutMs.gameObject.SetActive(!SampleAppManager.UsePKCE);
    }

    /// <summary>
    /// Gets the Device Code Auth timeout the user entered in 
    /// </summary>
    /// <returns></returns>
    private long? GetDeviceCodeTimeoutMs()
    {
        return string.IsNullOrEmpty(DeviceCodeTimeoutMs.text) ? null : long.Parse(DeviceCodeTimeoutMs.text);
    }

    /// <summary>
    /// Records whether the user has only logged in or also connected to IMX.
    /// This ensures the sample app displays the correct buttons in the authenticated scene.
    /// Navigates the user to the authenticated scene.
    /// </summary>
    /// <param name="connectedToImx">Indicates if the user is connected to IMX</param>
    private void NavigateToAuthenticatedScene(bool connectedToImx)
    {
        SampleAppManager.IsConnectedToImx = connectedToImx;
        SceneManager.LoadScene("AuthenticatedScene");
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
