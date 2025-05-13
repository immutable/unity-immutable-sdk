using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ZkEvmGetTransactionReceiptScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField TransactionHash;

    public void GetZkEvmTransactionReceipt()
    {
        GetZkEvmTransactionReceiptAsync().Forget();
    }

    private async UniTaskVoid GetZkEvmTransactionReceiptAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Getting transaction receipt...");
        try
        {
            await Passport.Instance.ConnectEvm();
            TransactionReceiptResponse response = await Passport.Instance.ZkEvmGetTransactionReceipt(TransactionHash.text);
            string status = $"Status: {GetTransactionStatusString(response.status)}";
            ShowOutput(status);
        }
        catch (System.Exception ex)
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