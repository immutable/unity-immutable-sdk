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
    public async void SignTypedData()
    {
        ShowOutput("Signing payload...");

        try
        {
            // Sign the given payload
            string signature = await Passport.ZkEvmSignTypedDataV4(Payload.text);
            ShowOutput(signature);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to retrieve transaction receipt: {ex.Message}");
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
