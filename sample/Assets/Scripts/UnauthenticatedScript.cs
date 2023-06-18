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

    [SerializeField] private Text output;

    [SerializeField] private Button connectButton;
    [SerializeField] private Text userCodeText;
    [SerializeField] private Button proceedLoginButton;

    async void Start()
    {
        try 
        {
            ShowOutput("Starting...");
            connectButton.gameObject.SetActive(false);
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);
            
            await Passport.Init();
            connectButton.gameObject.SetActive(true);
        } 
        catch (Exception ex) 
        {
            ShowOutput($"Start() error: {ex.Message}");
        }
    }

    private void OnReady() 
    {
        ShowOutput("Passport is ready");
        connectButton.gameObject.SetActive(true);
    }

    public async void Connect() 
    {
        try 
        {
            ShowOutput("Called Connect()...");
            userCodeText.gameObject.SetActive(false);
            proceedLoginButton.gameObject.SetActive(false);

            string? code = await Passport.Instance.Connect();

            if (code != null) 
            {
                // Code confirmation required
                ShowOutput($"Code to verify: {code}");
                userCodeText.gameObject.SetActive(true);
                userCodeText.text = code;
                proceedLoginButton.gameObject.SetActive(true);
            }
            else 
            {
                // No need to confirm code, log user straight in
                NavigateToAuthenticatedScene();
            }
        } 
        catch (Exception ex) 
        {
            string error = $"Connect() error: {ex.Message}";
            Debug.Log(error);
            ShowOutput(error);
        }
    }

    public async void ConfirmCode() 
    {
        try 
        {
            ShowOutput("Called ConfirmCode()...");
            await Passport.Instance.ConfirmCode();
            ShowOutput("Confirmed code");
            NavigateToAuthenticatedScene();
        } 
        catch (Exception ex) 
        {
            ShowOutput($"ConfirmCode() error: {ex.Message}");
        }
    }

    private void NavigateToAuthenticatedScene() 
    {
        SceneManager.LoadScene(sceneName:"AuthenticatedScene");
    }

    private void ShowOutput(string message) 
    {
        if (output != null) 
        {
            output.text = message;
        }
    }
}
