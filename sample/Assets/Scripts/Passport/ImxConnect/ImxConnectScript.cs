using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;

public class ImxConnectScript : MonoBehaviour
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
    /// Initialises the user's wallet and sets up the Immutable X provider using saved credentials if the user is already logged in.
    /// </summary>
    public async void ConnectImx()
    {
        ShowOutput("Connecting to Passport using saved credentials...");
        try
        {
            bool isConnected = await Passport.ConnectImx(useCachedSession: true);
            if (isConnected)
            {
                ShowOutput("Connected to IMX");
            }
            else
            {
                ShowOutput("Could not connect using saved credentials");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Error connecting: {ex.Message}");
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