using System.Globalization;
using System.Numerics;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class ZkEvmGetBalanceScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField AddressInput;

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
    /// Gets the balance of the account for the specified address.
    /// The balance is obtained in hexadecimal format and then converted to decimal for display.
    /// </summary>
    public async void GetBalance()
    {
        ShowOutput("Getting account balance...");

        try
        {
            // Retrieve the balance in hexadecimal format
            string balanceHex = await Passport.ZkEvmGetBalance(AddressInput.text);

            // Convert the hexadecimal balance to a BigInteger for decimal representation
            var balanceDec = BigInteger.Parse(balanceHex.Replace("0x", ""), NumberStyles.HexNumber);

            // Display both hexadecimal and decimal representations of the balance
            ShowOutput($"Balance:\nHex: {balanceHex}\nDec: {balanceDec}");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get balance: {ex.Message}");
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
