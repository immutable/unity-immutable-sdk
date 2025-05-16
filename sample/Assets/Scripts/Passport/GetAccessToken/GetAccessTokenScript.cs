using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class GetAccessTokenScript : MonoBehaviour
{
    [SerializeField] private Text Output;

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

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 