using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class AuthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text output;

    [SerializeField] private Button accessTokenButton;
    [SerializeField] private Button idTokenButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button signMessageButton;
    [SerializeField] private InputField signInput;

    private Passport passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport Instance is null");
        }
    }

    public async void GetAddress()
    {
        ShowOutput($"Called GetAddress()...");
        try
        {
            string? address = await passport.GetAddress();
            ShowOutput(address ?? "No address");
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to get address: {e.Type}");
        }
        catch (Exception)
        {
            ShowOutput("Unable to get address");
        }
    }

    public async void Logout()
    {
        await passport.Logout();
        SceneManager.LoadScene(sceneName: "UnauthenticatedScene");
    }

    public async void GetAccessToken()
    {
        string? accessToken = await passport.GetAccessToken();
        ShowOutput(accessToken ?? "No access token");
    }

    public async void GetIdToken()
    {
        string? idToken = await passport.GetIdToken();
        ShowOutput(idToken ?? "No ID token");
    }

    private void ShowOutput(string message)
    {
        if (output != null)
        {
            output.text = message;
        }
    }
}
