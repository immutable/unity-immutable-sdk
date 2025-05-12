using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class LogoutScript : MonoBehaviour
{
#pragma warning disable CS8618
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
            Debug.LogError("Passport instance is null");
        }
    }

    /// <summary>
    /// Logs out of Passport using the selected auth method.
    /// </summary>
    public async void Logout()
    {
        try
        {
            if (SampleAppManager.UsePKCE)
            {
                await Passport.LogoutPKCE();
            }
            else
            {
                await Passport.Logout();
            }
            SampleAppManager.IsConnectedToImx = false;
            SampleAppManager.IsConnectedToZkEvm = false;
            SceneManager.LoadScene("UnauthenticatedScene");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to logout: {ex.Message}");
        }
    }
} 