using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class SelectAuthMethodScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
    [SerializeField] private Toggle UseDeviceCodeAuthToggle;
    [SerializeField] private Toggle UsePKCEToggle;
#pragma warning restore CS8618

    void Start()
    {
        SetupPadding();

#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
        // For Android, iOS, macOS and Mac Unity Editor, allow users to select auth method
        // as both Device Code Auth and PKCE are available

        // Set up Device Code Auth toggle
        UseDeviceCodeAuthToggle.onValueChanged.AddListener(delegate (bool on)
        {
            SampleAppManager.UsePKCE = !on;
            InitialisePassport();
        });

        // Set up PKCE toggle
        UsePKCEToggle.onValueChanged.AddListener(delegate (bool on)
        {
            SampleAppManager.UsePKCE = on;
            // Initiliase Passport with redirects
            InitialisePassport(redirectUri: "imxsample://callback", logoutRedirectUri: "imxsample://callback/logout");
        });
#else
        // Otherwise only Device Code Auth is only available, so initialise Passport straight away
        InitialisePassport();
#endif
    }

    private async void InitialisePassport(string redirectUri = null, string logoutRedirectUri = null)
    {
        try
        {
            ShowOutput("Initilising Passport");

            // Initiliase Passport
            string clientId = "ZJL7JvetcDFBNDlgRs5oJoxuAUUl6uQj";
            string environment = Immutable.Passport.Model.Environment.SANDBOX;

            Passport passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);

            // Navigate to unauthenticated scene after initialising Passport
            SceneManager.LoadScene(sceneName: "UnauthenticatedScene");
        }
        catch (Exception ex)
        {
            ShowOutput($"Initialise Passport error: {ex.Message}");
        }
    }

    private void ShowOutput(string message)
    {
        Debug.Log($"Output: {message}");
        if (Output != null)
        {
            Output.text = message;
        }
    }

    private void SetupPadding()
    {
#if UNITY_IPHONE && !UNITY_EDITOR
        // Iphones normally have notches, so adding top padding so it doesn't block the UI
        TopPadding.gameObject.SetActive(true);
#else
        TopPadding.gameObject.SetActive(false);
#endif
    }
}
