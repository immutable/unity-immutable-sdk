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
            Passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    public async void GetBalance()
    {
        ShowOutput("Getting account balance...");
        try
        {
            string balanceHex = await Passport.ZkEvmGetBalance(AddressInput.text);
            var balanceDec = BigInteger.Parse(balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
            if (balanceDec < 0)
            {
                balanceDec = BigInteger.Parse("0" + balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
            }
            ShowOutput($"Balance:\nHex: {balanceHex}\nDec: {balanceDec}");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get balance: {ex.Message}");
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