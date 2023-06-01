using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;
using UnityEditor;

public class SampleScript : MonoBehaviour
{
    [SerializeField] private Passport passport;

    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;

    void Start()
    {
        Debug.Log($"Found passport? {passport != null}");
    }

    public async void Connect() {
        showOutput("Called Connect()");
        bool success = await passport.Connect();
        showOutput("Successfully connected to Passport");
    }

    public async void GetAddress() {
        showOutput("Called GetAddress()");
        string? address = await passport.GetAddress();
        showOutput(address);
    }

    public async void Logout() {
        showOutput("Called Logout()");
        passport.Logout();
        showOutput("Logged out");
    }

    private void showOutput(string message) {
        output.text = message;
    }
}
