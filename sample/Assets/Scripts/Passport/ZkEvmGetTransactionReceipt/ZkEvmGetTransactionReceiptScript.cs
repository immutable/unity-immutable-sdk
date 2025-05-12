using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ZkEvmGetTransactionReceiptScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField TransactionHash;
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

    public async void GetZkEvmTransactionReceipt()
    {
        ShowOutput("Getting transaction receipt...");
        try
        {
            TransactionReceiptResponse response = await Passport.ZkEvmGetTransactionReceipt(TransactionHash.text);
            string status = $"Status: {GetTransactionStatusString(response.status)}";
            ShowOutput(status);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to retrieve transaction receipt: {ex.Message}");
        }
    }

    private string GetTransactionStatusString(string status)
    {
        switch (status)
        {
            case "1":
            case "0x1":
                return "Success";
            case "0":
            case "0x0":
                return "Failed";
            case null:
                return "Still processing";
            default:
                return "Unknown status";
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