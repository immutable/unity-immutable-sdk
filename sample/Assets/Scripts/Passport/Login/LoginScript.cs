using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class LoginScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private Button DefaultLoginButton;
    [SerializeField] private Button GoogleLoginButton;
    [SerializeField] private Button Auth0NativeLoginButton; // Renamed from AndroidAccountPickerButton
    [SerializeField] private Button AppleLoginButton;
    [SerializeField] private Button FacebookLoginButton;

    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport Instance is null");
        }

        // Set up button listeners using DirectLoginOptions
        if (DefaultLoginButton != null) DefaultLoginButton.onClick.AddListener(() => Login(new DirectLoginOptions()));
        if (GoogleLoginButton != null) GoogleLoginButton.onClick.AddListener(() => Login(new DirectLoginOptions(DirectLoginMethod.Google)));
        if (Auth0NativeLoginButton != null) Auth0NativeLoginButton.onClick.AddListener(LoginWithAuth0Native);
        if (AppleLoginButton != null) AppleLoginButton.onClick.AddListener(() => Login(new DirectLoginOptions(DirectLoginMethod.Apple)));
        if (FacebookLoginButton != null) FacebookLoginButton.onClick.AddListener(() => Login(new DirectLoginOptions(DirectLoginMethod.Facebook)));
    }

    /// <summary>
    /// Logs into Passport using the default auth method.
    /// </summary>
    public async void Login()
    {
        await LoginAsync(new DirectLoginOptions());
    }

    /// <summary>
    /// Logs into Passport using the specified direct login options.
    /// </summary>
    /// <param name="directLoginOptions">The direct login options</param>
    public async void Login(DirectLoginOptions directLoginOptions)
    {
        await LoginAsync(directLoginOptions);
    }

    /// <summary>
    /// Internal async method that performs the actual login logic.
    /// </summary>
    /// <param name="directLoginOptions">The direct login options</param>
    private async System.Threading.Tasks.Task LoginAsync(DirectLoginOptions directLoginOptions)
    {
        try
        {
            string directLoginMethod = directLoginOptions.directLoginMethod.ToString().ToLower();

            ShowOutput($"Logging in with {directLoginMethod} method...");

            bool success = await Passport.Login(useCachedSession: false, directLoginOptions: directLoginOptions);

            if (success)
            {
                ShowOutput($"Successfully logged in with {directLoginMethod}");
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                ShowOutput($"Failed to log in with {directLoginMethod}");
            }
        }
        catch (OperationCanceledException ex)
        {
            ShowOutput($"Login cancelled: {ex.Message}");
        }
        catch (Exception ex)
        {
            ShowOutput($"Login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Login with Auth0 native authentication
    /// Uses Android Credential Manager + Auth0 SDK (native One Tap, no browser)
    /// Requires Android 9+ (API 28+)
    /// </summary>
    private async void LoginWithAuth0Native()
    {
        try
        {
            ShowOutput("Signing in with Auth0 native authentication...");

            // Call Auth0 native login (shows native Google picker, authenticates with Auth0)
            bool success = await Passport.LoginWithAuth0Native();

            if (success)
            {
                ShowOutput("Auth0 native sign-in successful!");
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                ShowOutput("Auth0 native sign-in failed");
            }
        }
        catch (OperationCanceledException)
        {
            ShowOutput("Sign-in cancelled by user");
        }
        catch (Exception ex)
        {
            ShowOutput($"Sign-in error: {ex.Message}");
        }
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }

        Debug.Log($"[LoginScript] {message}");
    }
}