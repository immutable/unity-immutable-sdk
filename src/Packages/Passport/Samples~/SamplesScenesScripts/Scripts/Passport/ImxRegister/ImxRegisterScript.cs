using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ImxRegisterScript : MonoBehaviour
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
    /// Registers the user with Immutable X if they are not already registered.
    /// </summary>
    public async void RegisterOffchain()
    {
        ShowOutput("Registering off-chain...");
        try
        {
            RegisterUserResponse response = await Passport.RegisterOffchain();
            if (response != null)
            {
                ShowOutput($"Successfully registered");
            }
            else
            {
                ShowOutput("Registration failed");
            }
        }
        catch (PassportException e)
        {
            // Handle 409 - account already registered
            if (e.Type == PassportErrorType.USER_REGISTRATION_ERROR &&
                (e.Message.Contains("409") || e.Message.Contains("already registered")))
            {
                ShowOutput("Passport account already registered");
            }
            else
            {
                ShowOutput($"Unable to register off-chain: {e.Message} ({e.Type})");
            }
        }
        catch (Exception e)
        {
            ShowOutput($"Unable to register off-chain {e.Message}");
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
