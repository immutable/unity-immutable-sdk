using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class SetCallTimeoutScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField TimeoutInput;

    public void SetTimeout()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        if (TimeoutInput == null)
        {
            Debug.LogError("[SetCallTimeoutScript] TimeoutInput is not assigned in the Inspector.");
            ShowOutput("Timeout input field is not assigned.");
            return;
        }
        if (!int.TryParse(TimeoutInput.text, out int timeout))
        {
            ShowOutput("Invalid timeout value");
            return;
        }
        Passport.Instance.SetCallTimeout(timeout);
        ShowOutput($"Set call timeout to: {timeout}ms");
    }

    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
        else
        {
            Debug.LogWarning($"[SetCallTimeoutScript] Output Text is not assigned. Message: {message}");
        }
    }
} 