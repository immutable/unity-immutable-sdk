using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;
using Cysharp.Threading.Tasks;

public class ZkEvmSendTransactionScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private Toggle ConfirmToggle;
    [SerializeField] private Toggle GetTransactionReceiptToggle;
    [SerializeField] private InputField ToInputField;
    [SerializeField] private InputField ValueInputField;
    [SerializeField] private InputField DataInputField;

    void Start()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ConfirmToggle.onValueChanged.AddListener(delegate
        {
            GetTransactionReceiptToggle.gameObject.SetActive(!ConfirmToggle.isOn);
        });
    }

    public void SendTransaction()
    {
        SendTransactionAsync().Forget();
    }

    private async UniTaskVoid SendTransactionAsync()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        // Ensure EVM provider is connected
        try
        {
            ShowOutput("Connecting to zkEVM provider...");
            await SampleAppManager.PassportInstance.ConnectEvm();
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to connect to zkEVM provider: {ex.Message}");
            return;
        }
        ShowOutput("Sending transaction...");
        try
        {
            TransactionRequest request = new TransactionRequest
            {
                to = ToInputField.text,
                value = ValueInputField.text,
                data = DataInputField.text
            };
            if (ConfirmToggle.isOn)
            {
                TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmSendTransactionWithConfirmation(request);
                ShowOutput($"Transaction hash: {response.transactionHash}\nStatus: {GetTransactionStatusString(response.status)}");
            }
            else
            {
                string transactionHash = await SampleAppManager.PassportInstance.ZkEvmSendTransaction(request);
                if (GetTransactionReceiptToggle.isOn)
                {
                    string? status = await PollStatus(transactionHash);
                    ShowOutput($"Transaction hash: {transactionHash}\nStatus: {GetTransactionStatusString(status)}");
                }
                else
                {
                    ShowOutput($"Transaction hash: {transactionHash}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to send transaction: {ex.Message}");
        }
    }

    static async UniTask<string?> PollStatus(string transactionHash)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                TransactionReceiptResponse response = await SampleAppManager.PassportInstance.ZkEvmGetTransactionReceipt(transactionHash);
                if (response.status == null)
                {
                    await UniTask.Delay(delayTimeSpan: TimeSpan.FromSeconds(1), cancellationToken: cancellationTokenSource.Token);
                }
                else
                {
                    return response.status;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        return null;
    }

    private string GetTransactionStatusString(string? status)
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