using System;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    void Start()
    {
        InitialisePassport(logoutRedirectUri: "https://www.immutable.com");
    }
        
    private async void InitialisePassport(string redirectUri = null, string logoutRedirectUri = null)
    {
        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Info;

            // Initialise Passport
            string environment = Immutable.Passport.Model.Environment.SANDBOX;
            string clientId = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK";
            Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);

            // Navigate to the unauthenticated scene after initialising Passport
            await passport.Login();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
        }
    }
}