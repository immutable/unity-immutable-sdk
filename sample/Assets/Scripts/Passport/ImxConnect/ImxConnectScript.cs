using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ImxConnectScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    /// <summary>
    /// Initialises the user's wallet and sets up the Immutable X provider using saved credentials if the user is already logged in.
    /// </summary>
    public void ConnectImx()
    {
        ConnectImxAsync().Forget();
    }

    private async UniTaskVoid ConnectImxAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        // Set the static property for global access
        SampleAppManager.PassportInstance = Passport.Instance;
        ShowOutput("Connecting to Passport using saved credentials...");
        try
        {
            await Passport.Instance.ConnectImx();
            
            // Add these lines to update connection state and refresh UI
            SampleAppManager.IsConnectedToImx = true;
            var sceneManager = FindObjectOfType<AuthenticatedSceneManager>();
            if (sceneManager != null)
            {
                sceneManager.UpdateImxButtonStates();
                Debug.Log("Updated IMX button states after connection");
            }
            else
            {
                Debug.LogWarning("Could not find AuthenticatedSceneManager to update button states");
            }
            
            ShowOutput("Connected to IMX");
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to connect to IMX: {ex.Message}");
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