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
    private enum Environment
    {
        Production,
        Sandbox,
        Development
    }

#pragma warning disable CS8618
    [SerializeField] private Text Output;

    [SerializeField] private Dropdown EnvironmentDropdown;
    [SerializeField] private InputField AccessTokenInput;
    [SerializeField] private InputField TypeInput;
    [SerializeField] private InputField WalletAddressInput;
    [SerializeField] private InputField SignatureInput;
    [SerializeField] private InputField NonceInput;
#pragma warning restore CS8618

    /// <summary>
    /// Link an external EOA wallet by providing an EIP-712 signature.
    /// </summary>
    public async void LinkWallet()
    {
        ShowOutput("Linking wallet...");

        try
        {
            var environments = (Environment[])Enum.GetValues(typeof(Environment));
            var environment = environments[EnvironmentDropdown.value];

            var config = new Configuration
            {
                BasePath = environment switch
                {
                    Environment.Production => "https://api.immutable.com",
                    Environment.Sandbox => "https://api.sandbox.immutable.com",
                    Environment.Development => "https://api.dev.immutable.com",
                    _ => ""
                },
                // Use Immutable Unity SDK Passport package to get the access token
                AccessToken = AccessTokenInput.text
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