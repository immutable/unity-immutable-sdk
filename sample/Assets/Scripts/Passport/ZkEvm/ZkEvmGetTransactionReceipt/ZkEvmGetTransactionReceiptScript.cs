#nullable enable

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Immutable.Passport;

public class ZkEvmGetTransactionReceiptScript : MonoBehaviour
{
    [SerializeField] private Text? output;
    [SerializeField] private InputField? transactionHashInputField;

    void Start()
    {
        if (output != null)
        {
            ClickToCopyHelper.EnableClickToCopy(output);
        }
    }

    public async void GetZkEvmTransactionReceipt()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Getting transaction receipt...");
        try
        {
            var transactionHash = transactionHashInputField?.text;
            if (transactionHash == null)
            {
                ShowOutput("No transaction hash");
                return;
            }

            await Passport.Instance.ConnectEvm();
            var response = await Passport.Instance.ZkEvmGetTransactionReceipt(transactionHash);
            var status = $"Status: {GetTransactionStatusString(response?.status)}";
            ShowOutput(status);
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to retrieve transaction receipt: {ex.Message}");
        }
    }

    private static string GetTransactionStatusString(string? status)
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
        if (output != null)
            output.text = message;
        Debug.Log($"[ZkEvmGetTransactionReceiptScript] {message}");
    }
}