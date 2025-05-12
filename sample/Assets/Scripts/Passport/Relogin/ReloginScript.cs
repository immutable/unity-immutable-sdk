using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class ReloginScript : MonoBehaviour
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
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput("Could not re-login using saved credentials");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to re-login: {ex.Message}");
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