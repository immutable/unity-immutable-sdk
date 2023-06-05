using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using UnityEditor;

public class AuthenticatedScript : MonoBehaviour
{
    private Passport passport;

    [SerializeField] private Text output;

    [SerializeField] private Button accessTokenButton;
    [SerializeField] private Button idTokenButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;

    void Start()
    {
        passport = Passport.Instance;
        passport.OnReady += OnReady;
    }

    private void OnReady() {
        showOutput("AuthenticatedScript Passport is ready");
    }

    public async void GetAddress() {
        showOutput($"Called GetAddress() {Passport.Instance != null}");
        string? address = await Passport.Instance.GetAddress();
        showOutput(address);
    }

    public async void Logout() {
        showOutput("Called Logout()");
        passport.Logout();
        showOutput("Logged out");
    }

    public void GetAccessToken() {
        showOutput(passport.getAccessToken());
    }

    public void GetIdToken() {
        showOutput(passport.getIdToken());
    }

    private void showOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
