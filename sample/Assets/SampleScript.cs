using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Immutable.Passport;
using UnityEditor;

public class SampleScript : MonoBehaviour
{
    [SerializeField] private Passport passport;

    [SerializeField] private Button connectButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;

    void Start()
    {
        Debug.Log($"Found passport? {passport != null}");
    }

    public async void Connect() {
        bool success = await passport.Connect();
        showDialog("Successfully connected to Passport");
    }

    public async void GetAddress() {
        string? address = await passport.GetAddress();
        showDialog($"Address {address}");
    }

    public async void Logout() {
        passport.Logout();
        showDialog("Logged out");
    }

    private void showDialog(string message) {
        EditorUtility.DisplayDialog("Output", message, "OK");
    }
}
