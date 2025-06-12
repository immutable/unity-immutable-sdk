using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class GetUserInfoScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    /// <summary>
    /// Retrieves the currently logged-in user's email.
    /// </summary>
    public void GetEmail()
    {
        GetEmailAsync();
    }

    private async UniTaskVoid GetEmailAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            string email = await Passport.Instance.GetEmail();
            ShowOutput(email);
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to get email: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's Passport ID.
    /// </summary>
    public void GetPassportId()
    {
        GetPassportIdAsync();
    }

    private async UniTaskVoid GetPassportIdAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            string passportId = await Passport.Instance.GetPassportId();
            ShowOutput(passportId);
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to get Passport ID: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's access token.
    /// </summary>
    public void GetAccessToken()
    {
        GetAccessTokenAsync();
    }

    private async UniTaskVoid GetAccessTokenAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            string accessToken = await Passport.Instance.GetAccessToken();
            ShowOutput(accessToken);
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to get access token: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's ID token.
    /// </summary>
    public void GetIdToken()
    {
        GetIdTokenAsync();
    }

    private async UniTaskVoid GetIdTokenAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        try
        {
            string idToken = await Passport.Instance.GetIdToken();
            ShowOutput(idToken);
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to get ID token: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the user's linked external wallets from the Passport account dashboard.
    /// </summary>
    public void GetLinkedAddresses()
    {
        GetLinkedAddressesAsync();
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
            string outputMessage = addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses";
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