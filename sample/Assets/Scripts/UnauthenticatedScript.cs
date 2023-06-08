using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Auth;
using UnityEditor;
using VoltstroStudios.UnityWebBrowser.Core;
using System.IO;

public class UnauthenticatedScript : MonoBehaviour
{
    [SerializeField] private Passport passport;

    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Text userCodeText;
    [SerializeField] private Button proceedLoginButton;

    void Start()
    {
        connectButton.gameObject.SetActive(false);
        userCodeText.gameObject.SetActive(false);
        proceedLoginButton.gameObject.SetActive(false);

        passport.OnReady += OnReady;

        ShowOutput("Starting...");
    }

    private void OnReady() {
        ShowOutput("Passport is ready");
        connectButton.gameObject.SetActive(true);
    }

    public async void Connect() {
        try {
            ShowOutput("Called Connect()...");
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);

            string? code = await passport.Connect();

            if (code != null) {
                // Code confirmation required
                ShowOutput($"Code to verify: {code}");
                userCodeText.gameObject.SetActive(true);
                userCodeText.text = code;
                proceedLoginButton.gameObject.SetActive(true);
            } else {
                // No need to confirm code, log user straight in
                NavigateToAuthenticatedScene();
            }
        } catch (Exception ex) {
            string error = $"Connect() error: {ex.Message}";
            Debug.Log(error);
            ShowOutput(error);
        }
    }

    public async void ConfirmCode() {
        try {
            ShowOutput("Called ConfirmCode()...");
            await passport.ConfirmCode();
            ShowOutput("Confirmed code");
            NavigateToAuthenticatedScene();
        } catch (Exception ex) {
            ShowOutput($"ConfirmCode() error: {ex.Message}");
        }
    }

    private void NavigateToAuthenticatedScene() {
        SceneManager.LoadScene(sceneName:"AuthenticatedScene");
    }

    private void ShowOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
