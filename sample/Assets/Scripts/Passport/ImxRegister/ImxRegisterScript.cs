using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        ClickToCopyHelper.EnableClickToCopy(Output);

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
            ShowOutput($"Unable to register off-chain: {e.Message} ({e.Type})");
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