using System;
using Immutable.Api.ZkEvm.Api;
using Immutable.Api.ZkEvm.Client;
using Immutable.Api.ZkEvm.Model;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;

public class LinkWalletScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;
    [SerializeField] private InputField TypeInput;
    [SerializeField] private InputField WalletAddressInput;
    [SerializeField] private InputField SignatureInput;
    [SerializeField] private InputField NonceInput;
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

    /// <summary>
    /// Link an external EOA wallet by providing an EIP-712 signature.
    /// </summary>
    public async void LinkWallet()
    {
        ShowOutput("Linking wallet...");
        try
        {
            await Passport.ConnectEvm();
            await Passport.ZkEvmRequestAccounts();
            var config = new Configuration
            {
                BasePath = Passport.environment switch
                {
                    Immutable.Passport.Model.Environment.SANDBOX => "https://api.sandbox.immutable.com",
                    Immutable.Passport.Model.Environment.PRODUCTION => "https://api.immutable.com",
                    Immutable.Passport.Model.Environment.DEVELOPMENT => "https://api.dev.immutable.com",
                    _ => ""
                },
                AccessToken = await Passport.GetAccessToken()
            };
            var apiInstance = new PassportProfileApi(config);
            var linkWalletV2Request = new LinkWalletV2Request(
                type: TypeInput.text,
                walletAddress: WalletAddressInput.text,
                signature: SignatureInput.text,
                nonce: NonceInput.text);
            await apiInstance.LinkWalletV2Async(linkWalletV2Request);
            ShowOutput($"Linked external wallet: {WalletAddressInput.text}");
        }
        catch (ApiException e)
        {
            Debug.Log($"Exception when calling PassportProfileApi.LinkWalletV2: {e.Message}");
            Debug.Log($"Status Code: {e.ErrorCode}");
            ShowOutput($"Failed to link wallet: {e.Message}");
        }
        catch (Exception e)
        {
            ShowOutput($"Failed to link wallet: {e.Message}");
        }
    }

    /// <summary>
    /// Navigates back to the authenticated scene.
    /// </summary>
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