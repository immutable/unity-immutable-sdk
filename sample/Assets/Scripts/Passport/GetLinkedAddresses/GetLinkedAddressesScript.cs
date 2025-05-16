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
        Debug.Log("E2E TEST DEBUG: GetLinkedAddresses() called (public method)");
        GetLinkedAddressesAsync();
    }

    private async UniTaskVoid GetLinkedAddressesAsync()
    {
        Debug.Log("E2E TEST DEBUG: GetLinkedAddressesAsync() started");
        if (Passport.Instance == null)
        {
            Debug.Log("E2E TEST DEBUG: Passport instance is null in GetLinkedAddressesAsync");
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            Debug.Log("E2E TEST DEBUG: About to await Passport.Instance.GetLinkedAddresses()");
            List<string> addresses = await Passport.Instance.GetLinkedAddresses();
            string outputMessage = addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses";
            Debug.Log($"E2E TEST DEBUG: Got addresses, output will be: {outputMessage}");
            ShowOutput(outputMessage);
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