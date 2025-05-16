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

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 