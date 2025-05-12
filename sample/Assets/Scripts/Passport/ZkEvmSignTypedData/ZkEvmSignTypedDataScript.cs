using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ZkEvmSignTypedDataScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField Payload;
    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    public async void SignTypedData()
    {
        ShowOutput("Signing payload...");
        try
        {
            string signature = await Passport.ZkEvmSignTypedDataV4(Payload.text);
            ShowOutput(signature);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to sign typed data: {ex.Message}");
        }
    }

    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 