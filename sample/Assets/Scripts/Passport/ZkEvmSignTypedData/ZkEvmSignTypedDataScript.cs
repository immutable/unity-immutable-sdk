using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;
using Immutable.Passport.Model;

public class ZkEvmSignTypedDataScript : MonoBehaviour
{
    [SerializeField] private Text Output;
    [SerializeField] private InputField Payload;

    public void SignTypedData()
    {
        SignTypedDataAsync();
    }

    private async UniTaskVoid SignTypedDataAsync()
    {
        if (Passport.Instance == null)
        {
            ShowOutput("Passport instance is null");
            return;
        }
        ShowOutput("Signing payload...");
        try
        {
            await Passport.Instance.ConnectEvm();
            string signature = await Passport.Instance.ZkEvmSignTypedDataV4(Payload.text);
            ShowOutput(signature);
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
        if (Output != null)
        {
            Output.text = message;
        }
    }
} 