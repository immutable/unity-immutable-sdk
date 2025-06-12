using System.Globalization;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class ZkEvmGetBalanceScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField AddressInput;

    public void GetBalance()
    {
        GetBalanceAsync();
    }

    private async UniTaskVoid GetBalanceAsync()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Getting account balance...");
        try
        {
            string balanceHex = await SampleAppManager.PassportInstance.ZkEvmGetBalance(AddressInput.text);
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
        if (Output != null)
        {
            Output.text = message;
        }
    }
}