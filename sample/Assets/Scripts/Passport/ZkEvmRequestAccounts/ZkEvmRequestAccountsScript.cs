using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class ZkEvmRequestAccountsScript : MonoBehaviour
{
    [SerializeField] private Text Output;

    public void RequestAccounts()
    {
        RequestAccountsAsync();
    }

    private async UniTaskVoid RequestAccountsAsync()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport not initialized.");
            return;
        }

        ShowOutput("Requesting wallet accounts...");
        try
        {
            List<string> accounts = await SampleAppManager.PassportInstance.ZkEvmRequestAccounts();
            ShowOutput(accounts.Count > 0 ? string.Join(", ", accounts) : "No accounts found.");
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to request wallet accounts: {ex.Message}");
        }
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
            Output.text = message;
        Debug.Log($"[ZkEvmRequestAccountsScript] {message}");
    }
}
