using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ReconnectScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    /// <summary>
    /// Uses existing credentials to re-login to Passport and reconnect to IMX.
    /// </summary>
    public void Reconnect()
    {
        ReconnectAsync();
    }

    private async UniTaskVoid ReconnectAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport Instance is null");
            return;
        }
        ShowOutput("Reconnecting to Passport using saved credentials...");
        try
        {
            bool connected = await Passport.Instance.ConnectImx(useCachedSession: true);
            if (connected)
            {
                // Set IMX and zkEVM state and update UI as if user clicked Connect to IMX/EVM
                SampleAppManager.IsConnectedToImx = true;
                SampleAppManager.IsConnectedToZkEvm = true;
                SampleAppManager.PassportInstance = Passport.Instance;
                var sceneManager = GameObject.FindObjectOfType<AuthenticatedSceneManager>();
                if (sceneManager != null)
                {
                    sceneManager.UpdateImxButtonStates();
                    sceneManager.UpdateZkEvmButtonStates();
                }
                NavigateToAuthenticatedScene();
            }
            else
            {
                ShowOutput("Could not reconnect using saved credentials");
            }
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to reconnect: {ex.Message}");
        }
    }

    private void NavigateToAuthenticatedScene()
    {
        AuthenticatedSceneManager.NavigateToAuthenticatedScene();
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}