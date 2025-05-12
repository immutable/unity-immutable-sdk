using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class SetCallTimeoutScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField TimeoutInput;
    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    public void SetTimeout()
    {
        int timeout = Int32.Parse(TimeoutInput.text);
        Passport.SetCallTimeout(timeout);
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
    }
} 