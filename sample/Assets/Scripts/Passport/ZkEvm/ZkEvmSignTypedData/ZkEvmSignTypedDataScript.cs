#nullable enable

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class ZkEvmSignTypedDataScript : MonoBehaviour
{
    [SerializeField] private Text? output;
    [SerializeField] private InputField? payloadInputField;

    public async void SignTypedData()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Signing payload...");
        try
        {
            var payload = payloadInputField?.text;
            if (payload == null)
            {
                ShowOutput("No payload");
                return;
            }

            await Passport.Instance.ConnectEvm();
            var signature = await Passport.Instance.ZkEvmSignTypedDataV4(payload);
            ShowOutput(signature ?? "No signature");
        }
        catch (System.Exception ex)
        {
            ShowOutput($"Failed to sign typed data: {ex.Message}");
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
        Debug.Log($"[ZkEvmSignTypedDataScript] {message}");
    }
}