using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class GetLinkedAddressesScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    /// <summary>
    /// Retrieves the user's linked external wallets from the Passport account dashboard.
    /// </summary>
    public void GetLinkedAddresses()
    {
        GetLinkedAddressesAsync().Forget();
    }

    private async UniTaskVoid GetLinkedAddressesAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            List<string> addresses = await Passport.Instance.GetLinkedAddresses();
            ShowOutput(addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses");
        }
        catch (System.Exception ex)
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