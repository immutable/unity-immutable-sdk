#nullable enable

using UnityEngine;
using UnityEngine.UI;

public class ZkEvmRequestAccountsScript : MonoBehaviour
{
    [SerializeField] private Text? output;

    public async void RequestAccounts()
    {
        if (SampleAppManager.PassportInstance == null)
        {
            ShowOutput("Passport not initialised.");
            return;
        }

        ShowOutput("Requesting wallet accounts...");
        try
        {
            var accounts = await SampleAppManager.PassportInstance.ZkEvmRequestAccounts();
            ShowOutput(accounts.Count > 0 ? string.Join(", ", accounts) : "No accounts found.");
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to request wallet accounts: {ex.Message}");
        }
    }

    private void ShowOutput(string message)
    {
        if (output != null)
            output.text = message;
        Debug.Log($"[ZkEvmRequestAccountsScript] {message}");
    }
}
