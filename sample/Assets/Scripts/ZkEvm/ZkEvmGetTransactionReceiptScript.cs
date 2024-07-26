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
            // Get Passport instance
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    /// <summary>
    /// Retrieves the transaction receipt for a given transaction hash using the Ethereum JSON-RPC <c>eth_getTransactionReceipt</c> method.
    /// </summary>
    public async void GetZkEvmTransactionReceipt()
    {
        ShowOutput("Getting transaction receipt...");

        try
        {
            // Retrieve the transaction receipt using the provided hash
            TransactionReceiptResponse response = await Passport.ZkEvmGetTransactionReceipt(TransactionHash.text);

            // Display the transaction status
            string status = $"Status: {GetTransactionStatusString(response.status)}";
            ShowOutput(status);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to retrieve transaction receipt: {ex.Message}");
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
