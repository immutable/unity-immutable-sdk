using System;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ImxGetAddressScript : MonoBehaviour
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
    /// Gets the wallet address of the currently logged-in user.
    /// </summary>
    public async void GetAddress()
    {
        ShowOutput("Retrieving wallet address...");
        try
        {
            string address = await Passport.GetAddress();
            ShowOutput(string.IsNullOrEmpty(address) ? "No address found" : address);
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to retrieve address: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to retrieve address");
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