using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class GetIdTokenScript : MonoBehaviour
{
    [SerializeField] private Text Output;

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

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 