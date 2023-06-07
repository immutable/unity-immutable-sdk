using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Auth;
using UnityEditor;

public class UnauthenticatedScript : MonoBehaviour
{
    [SerializeField] private Passport passport;

    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Text userCodeText;
    [SerializeField] private Button proceedLoginButton;

    void Start()
    {
        passport.OnReady += OnReady;

        userCodeText.gameObject.SetActive(false);
        proceedLoginButton.gameObject.SetActive(false);
    }

    private void OnReady() {
        showOutput("UnauthenticatedScript Passport is ready");
    }

    public async void Connect() {
        try {
            showOutput("Called Connect()...");
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);

            string code = await passport.Connect();
            showOutput($"Code to verify: {code}");

            userCodeText.gameObject.SetActive(true);
            userCodeText.text = code;
            proceedLoginButton.gameObject.SetActive(true);
        } catch (Exception ex) {
            string error = $"Connect() error: {ex.Message}";
            Debug.Log(error);
            showOutput(error);
        }
    }

    public async void ConfirmCode() {
        try {
            showOutput("Called ConfirmCode()...");
            await passport.ConfirmCode();
            showOutput("Confirmed code");
            SceneManager.LoadScene(sceneName:"AuthenticatedScene");
        } catch (Exception ex) {
            showOutput($"ConfirmCode() error: {ex.Message}");
        }
    }

    private void showOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
