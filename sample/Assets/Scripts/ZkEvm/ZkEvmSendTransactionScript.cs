using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ZkEvmSendTransactionScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;

    [SerializeField] private Toggle ConfirmToggle;
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
        ShowOutput("Called sendTransaction()...");

        try
        {
            // Create transaction request
            TransactionRequest request = new TransactionRequest
            {
                to = ToInputField.text,
                value = ValueInputField.text,
                data = DataInputField.text
            };

            // Send transaction with or without confirmation
            if (ConfirmToggle.isOn)
            {
                TransactionReceiptResponse response = await Passport.ZkEvmSendTransactionWithConfirmation(request);
                ShowOutput($"Transaction hash: {response.transactionHash}\nStatus: {GetTransactionStatusString(response.status)}");
            }
            else
            {
                string response = await Passport.ZkEvmSendTransaction(request);
                ShowOutput($"Transaction hash: {response}");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to request accounts: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts transaction status code to a human-readable string.
    /// </summary>
    /// <param name="status">The transaction status code.</param>
    /// <returns>A string representing the status.</returns>
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
