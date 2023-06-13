using System;
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
    [SerializeField] private Button signMessageButton;
    [SerializeField] private InputField signInput;

    void Start()
    {
        passport = Passport.Instance;
    }

    public async void GetAddress() {
        showOutput($"Called GetAddress()...");
        try {
            string? address = await Passport.Instance.GetAddress();
            showOutput(address);
        } catch (Exception e) {
            showOutput("Unable to get address");
        }
    }

    public async void Logout() {
        showOutput("Called Logout()...");
        passport.Logout();
        showOutput("Logged out");
    }

    public void GetAccessToken() {
        showOutput(passport.GetAccessToken());
    }

    public void GetIdToken() {
        showOutput(passport.GetIdToken());
    }

    public async void SignMessage() {
        showOutput("Called SignMessage()...");
        try {
            string? result = await passport.SignMessage(signInput.text);
            showOutput(result);
        } catch (Exception e) {
            showOutput("Unable to sign message");
        }
    }

    private void showOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
