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
    [SerializeField] private Text output;

    [SerializeField] private Button accessTokenButton;
    [SerializeField] private Button idTokenButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button signMessageButton;
    [SerializeField] private InputField signInput;

    public async void GetAddress() {
        ShowOutput($"Called GetAddress()...");
        try {
            string? address = await Passport.Instance.GetAddress();
            ShowOutput(address);
        } catch (Exception e) {
            ShowOutput("Unable to get address");
        }
    }

    public async void Logout() {
        Passport.Instance.Logout();
        SceneManager.LoadScene(sceneName:"UnauthenticatedScene");
    }

    public void GetAccessToken() {
        ShowOutput(Passport.Instance.GetAccessToken());
    }

    public void GetIdToken() {
        ShowOutput(Passport.Instance.GetIdToken());
    }

    public async void SignMessage() {
        ShowOutput("Called SignMessage()...");
        try {
            string? result = await Passport.Instance.SignMessage(signInput.text);
            ShowOutput(result);
        } catch (Exception e) {
            ShowOutput("Unable to sign message");
        }
    }

    private void ShowOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
