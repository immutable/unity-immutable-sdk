#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Immutable.Passport.Model;
using Cysharp.Threading.Tasks;

public class ZkEvmSendTransactionScript : MonoBehaviour
{
    [SerializeField] private Text? output;
    [SerializeField] private Toggle? confirmToggle;
    [SerializeField] private Toggle? getTransactionReceiptToggle;
    [SerializeField] private InputField? toInputField;
    [SerializeField] private InputField? valueInputField;
    [SerializeField] private InputField? dataInputField;

    void Start()
    {
        if (output != null)
        {
            ClickToCopyHelper.EnableClickToCopy(output);
        }

        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }

        // Make sure UI elements are initialised
        if (confirmToggle != null && getTransactionReceiptToggle != null)
        {
            confirmToggle.onValueChanged.AddListener(delegate
            {
                getTransactionReceiptToggle.gameObject.SetActive(!confirmToggle.isOn);
            });
        }
    }

    public async void SendTransaction()
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
            var request = new TransactionRequest
            {
                to = toInputField != null ? toInputField.text : "",
                value = valueInputField != null ? valueInputField.text : "",
                data = dataInputField != null ? dataInputField.text : ""
            };

            if (confirmToggle != null && confirmToggle.isOn)
            {
                var response = await SampleAppManager.PassportInstance.ZkEvmSendTransactionWithConfirmation(request);
                ShowOutput($"Transaction hash: {response?.hash}\nStatus: {GetTransactionStatusString(response?.status)}");
            }
            else
            {
                var transactionHash = await SampleAppManager.PassportInstance.ZkEvmSendTransaction(request);

                if (transactionHash == null)
                {
                    ShowOutput("No transaction hash");
                    return;
                }

                if (getTransactionReceiptToggle != null && getTransactionReceiptToggle.isOn)
                {
                    var status = await PollStatus(transactionHash);
                    ShowOutput($"Transaction hash: {transactionHash}\nStatus: {GetTransactionStatusString(status)}");
                    return;
                }

                ShowOutput($"Transaction hash: {transactionHash}");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to send transaction: {ex.Message}");
        }
    }

    private static async UniTask<string?> PollStatus(string transactionHash)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var response = await SampleAppManager.PassportInstance.ZkEvmGetTransactionReceipt(transactionHash);
                if (response?.status == null)
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
        Debug.Log($"[ZkEvmSendTransactionScript] {message}");
    }
}