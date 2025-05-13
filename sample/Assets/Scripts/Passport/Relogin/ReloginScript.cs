using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ReloginScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    /// <summary>
    /// Uses the existing credentials to re-login to Passport.
    /// </summary>
    public void Relogin()
    {
        ReloginAsync().Forget();
    }

    private async UniTaskVoid ReloginAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport Instance is null");
            return;
        }
        ShowOutput("Re-logging into Passport using saved credentials...");
        try
        {
            bool loggedIn = await Passport.Instance.Login(useCachedSession: true);
            if (loggedIn)
            {
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput("Could not re-login using saved credentials");
            }
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to re-login: {ex.Message}");
        }
    }

    private void NavigateToAuthenticatedScene()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 