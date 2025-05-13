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
        ShowOutput("Connecting to Passport using saved credentials...");
        try
        {
            bool isConnected = await Passport.Instance.ConnectImx(useCachedSession: true);
            if (isConnected)
            {
                ShowOutput("Connected to IMX");
            }
            else
            {
                ShowOutput("Could not connect using saved credentials");
            }
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Error connecting: {ex.Message}");
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