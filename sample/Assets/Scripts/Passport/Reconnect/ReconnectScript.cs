using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class ReconnectScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
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
    }

    /// <summary>
    /// Uses existing credentials to re-login to Passport and reconnect to IMX.
    /// </summary>
    public async void Reconnect()
    {
        ShowOutput("Reconnecting to Passport using saved credentials...");
        try
        {
            bool connected = await Passport.ConnectImx(useCachedSession: true);
            if (connected)
            {
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput("Could not reconnect using saved credentials");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to reconnect: {ex.Message}");
        }
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