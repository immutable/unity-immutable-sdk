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

        // Set up button listeners if buttons are assigned
        if (DefaultLoginButton != null) DefaultLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.None));
        if (GoogleLoginButton != null) GoogleLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Google));
        if (AppleLoginButton != null) AppleLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Apple));
        if (FacebookLoginButton != null) FacebookLoginButton.onClick.AddListener(() => Login(DirectLoginMethod.Facebook));
    }

    /// <summary>
    /// Logs into Passport using the default auth method.
    /// </summary>
    public async void Login()
    {
        await LoginAsync(DirectLoginMethod.None);
    }

    /// <summary>
    /// Logs into Passport using the specified direct login method.
    /// </summary>
    /// <param name="directLoginMethod">The direct login method to use (Google, Apple, Facebook, or None for default)</param>
    public async void Login(DirectLoginMethod directLoginMethod)
    {
        await LoginAsync(directLoginMethod);
    }

    /// <summary>
    /// Internal async method that performs the actual login logic.
    /// </summary>
    /// <param name="directLoginMethod">The direct login method to use</param>
    private async System.Threading.Tasks.Task LoginAsync(DirectLoginMethod directLoginMethod)
    {
        try
        {
            string methodName = directLoginMethod == DirectLoginMethod.None ? "default" : directLoginMethod.ToString();
            ShowOutput($"Logging in with {methodName} method...");

            bool success = await Passport.Login(useCachedSession: false, directLoginMethod: directLoginMethod);

            if (success)
            {
                ShowOutput($"Successfully logged in with {methodName}");
                SceneManager.LoadScene("AuthenticatedScene");
            }
            else
            {
                ShowOutput($"Failed to log in with {methodName}");
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

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }

        Debug.Log($"[LoginScript] {message}");
    }
}