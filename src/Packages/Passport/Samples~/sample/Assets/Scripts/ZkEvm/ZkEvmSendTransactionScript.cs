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
#pragma warning disable CS8618
    [SerializeField] private Text Output;

    [SerializeField] private Toggle ConfirmToggle;
    [SerializeField] private Toggle GetTrasactionReceiptToggle;
    [SerializeField] private InputField ToInputField;
    [SerializeField] private InputField ValueInputField;
    [SerializeField] private InputField DataInputField;

    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            // Get Passport instance
            Passport = Passport.Instance;

            // Show get transaction receipt option if send transaction with confirmation toggle is off
            ConfirmToggle.onValueChanged.AddListener(delegate
            {
                GetTrasactionReceiptToggle.gameObject.SetActive(!ConfirmToggle.isOn);
            });
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    /// <summary>
    /// Constructs a transaction using the provided To, Value, and Data fields.
    /// Sends the transaction to the network and signs it with the currently authenticated Passport account.
    /// If confirmation is requested, the function waits for the transaction to be included in a block,
    /// and displays the transaction status upon completion.
    /// </summary>
    public async void SendTransaction()
    {
        ShowOutput("Sending transaction...");

        try
        {
            // Create transaction request
            TransactionRequest request = new TransactionRequest
            {
                to = ToInputField.text,
                value = ValueInputField.text,
                data = DataInputField.text
            };

            // Check if confirmation is requested
            if (ConfirmToggle.isOn)
            {
                // Send transaction with confirmation and display transaction status upon completion
                TransactionReceiptResponse response = await Passport.ZkEvmSendTransactionWithConfirmation(request);
                ShowOutput($"Transaction hash: {response.transactionHash}\nStatus: {GetTransactionStatusString(response.status)}");
            }
            else
            {
                // Send transaction without confirmation
                string transactionHash = await Passport.ZkEvmSendTransaction(request);

                // Check if receipt is requested
                if (GetTrasactionReceiptToggle.isOn)
                {
                    // Poll for the receipt and display transaction status
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

    /// <summary>
    /// Polls the status of the given transaction hash until either a status is retrieved or a timeout occurs.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction to poll.</param>
    /// <returns>The status of the transaction, or null if a timeout occurs.</returns>
    static async UniTask<string?> PollStatus(string transactionHash)
    {
        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                TransactionReceiptResponse response = await Passport.Instance.ZkEvmGetTransactionReceipt(transactionHash);
                if (response.status == null)
                {
                    // The transaction is still being processed, poll for status again
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
            // Task was canceled due to timeout
        }

        return null; // Timeout or could not get transaction receipt
    }

    /// <summary>
    /// Converts transaction status code to a human-readable string.
    /// </summary>
    /// <param name="status">The transaction status code.</param>
    /// <returns>A string representing the status.</returns>
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

    /// <summary>
    /// Navigates back to the authenticated scene.
    /// </summary>
    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }

    /// <summary>
    /// Prints the specified <code>message</code> to the output box.
    /// </summary>
    /// <param name="message">The message to print</param>
    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}
