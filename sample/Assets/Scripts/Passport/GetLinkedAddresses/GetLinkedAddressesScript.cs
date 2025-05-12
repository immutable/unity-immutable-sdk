using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;

public class GetLinkedAddressesScript : MonoBehaviour
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
    /// Retrieves the user's linked external wallets from the Passport account dashboard.
    /// </summary>
    public async void GetLinkedAddresses()
    {
        try
        {
            List<string> addresses = await Passport.GetLinkedAddresses();
            ShowOutput(addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get linked addresses: {ex.Message}");
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