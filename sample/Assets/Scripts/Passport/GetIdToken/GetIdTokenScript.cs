using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;

public class GetIdTokenScript : MonoBehaviour
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
            ShowOutput("Passport instance is null");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's ID token.
    /// </summary>
    public async void GetIdToken()
    {
        try
        {
            string idToken = await Passport.GetIdToken();
            ShowOutput(idToken);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get ID token: {ex.Message}");
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