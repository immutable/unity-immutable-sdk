using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class LoginScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField DeviceCodeTimeoutMs;
    private Passport Passport;
#pragma warning restore CS8618

    async void Start()
    {
        if (Passport.Instance != null)
        {
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport Instance is null");
        }
    }

    /// <summary>
    /// Logs into Passport using the selected auth method.
    /// </summary>
    public async void Login()
    {
        var timeoutMs = GetDeviceCodeTimeoutMs();
        string formattedTimeout = timeoutMs != null ? $"{timeoutMs} ms" : "none";
        ShowOutput($"Logging in (timeout: {formattedTimeout})...");
        try
        {
            if (SampleAppManager.UsePKCE)
            {
                await Passport.LoginPKCE();
            }
            else
            {
                await Passport.Login(timeoutMs: timeoutMs);
            }
            NavigateToAuthenticatedScene();
        }
        catch (OperationCanceledException)
        {
            ShowOutput("Failed to login: cancelled");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to login: {ex.Message}");
        }
    }

    private long? GetDeviceCodeTimeoutMs()
    {
        return string.IsNullOrEmpty(DeviceCodeTimeoutMs.text) ? null : long.Parse(DeviceCodeTimeoutMs.text);
    }

    private void NavigateToAuthenticatedScene()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}