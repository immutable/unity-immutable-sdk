using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using UnityEditor;

public class UnauthenticatedScript : MonoBehaviour
{
    [SerializeField] private Passport passport;

    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;

    void Start()
    {
        passport.OnReady += OnReady;

        Debug.Log($"UnauthenticatedScript Found passport? {Passport.Instance != null}");
    }

    private void OnReady() {
        showOutput("UnauthenticatedScript Passport is ready");
    }

    public async void Connect() {
        showOutput("Called Connect()");
        bool success = await passport.Connect();
        showOutput("Successfully connected to Passport");
        SceneManager.LoadScene(sceneName:"AuthenticatedScene");
    }

    private void showOutput(string message) {
        if (output != null) {
            output.text = message;
        }
    }
}
