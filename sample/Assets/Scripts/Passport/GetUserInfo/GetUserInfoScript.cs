using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;

public class GetUserInfoScript : MonoBehaviour
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
    /// Retrieves the currently logged-in user's email.
    /// </summary>
    public async void GetEmail()
    {
        try
        {
            string email = await Passport.GetEmail();
            ShowOutput(email);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get email: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's Passport ID.
    /// </summary>
    public async void GetPassportId()
    {
        try
        {
            string passportId = await Passport.GetPassportId();
            ShowOutput(passportId);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get Passport ID: {ex.Message}");
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