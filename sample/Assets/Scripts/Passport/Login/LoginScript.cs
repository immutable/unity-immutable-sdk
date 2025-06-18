using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class LoginScript : MonoBehaviour
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
    /// Logs into Passport using the selected auth method.
    /// </summary>
    public async void Login()
    {
        try
        {
            await Passport.Login();
            SceneManager.LoadScene("AuthenticatedScene");
        }
        catch (OperationCanceledException ex)
        {
            ShowOutput($"Failed to login: cancelled {ex.Message}\\n{ex.StackTrace}");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to login: {ex.Message}");
        }
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}