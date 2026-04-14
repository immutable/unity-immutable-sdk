#nullable enable

using System.Globalization;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ZkEvmGetBalanceScript : MonoBehaviour
{
    [SerializeField] private Text? output;
    [SerializeField] private InputField? addressInput;

    public async void GetBalance()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Getting account balance...");
        try
        {
            var address = addressInput?.text;
            if (address == null)
            {
                ShowOutput("No address");
                return;
            }

            var balanceHex = await SampleAppManager.PassportInstance.ZkEvmGetBalance(address);
            var balanceDec = BigInteger.Parse(balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
            if (balanceDec < 0)
            {
                balanceDec = BigInteger.Parse("0" + balanceHex.Replace("0x", ""), NumberStyles.HexNumber);
            }
            ShowOutput($"Balance:\nHex: {balanceHex}\nDec: {balanceDec}");
        }
        catch (System.Exception ex)
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
        if (output != null)
            output.text = message;
        Debug.Log($"[ZkEvmGetBalanceScript] {message}");
    }
}