using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ImxConnectScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    void Start()
    {
        ClickToCopyHelper.EnableClickToCopy(Output);
    }

    /// <summary>
    /// Initialises the user's wallet and sets up the Immutable X provider using saved credentials if the user is already logged in.
    /// </summary>
    public void ConnectImx()
    {
        ConnectImxAsync();
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

            SampleAppManager.IsConnectedToImx = true;
            ShowOutput("Connected to IMX"); // Show success early

            // Try to find UnauthenticatedSceneManager first
            var unauthSceneManager = FindObjectOfType<UnauthenticatedSceneManager>();
            if (unauthSceneManager != null)
            {
                unauthSceneManager.OnImxConnected?.Invoke();
                return;
            }

            var authSceneManager = FindObjectOfType<AuthenticatedSceneManager>();
            if (authSceneManager != null)
            {
                authSceneManager.UpdateImxButtonStates();
                authSceneManager.OnImxConnected?.Invoke();
                return;
            }
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