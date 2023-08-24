using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

public class AuthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text output;

    [SerializeField] private Canvas authenticatedCanvas;
    [SerializeField] private Button accessTokenButton;
    [SerializeField] private Button idTokenButton;
    [SerializeField] private Button getAddressButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button signMessageButton;
    [SerializeField] private Button showTransferButton;
    [SerializeField] private InputField signInput;

    [SerializeField] private Canvas transferCanvas;
    [SerializeField] private InputField tokenIdInput;
    [SerializeField] private InputField tokenAddressInput;
    [SerializeField] private InputField receiverInput;
    [SerializeField] private Button transferButton;
    [SerializeField] private Button cancelTransferButton;

    private Passport passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            passport = Passport.Instance;
        }
        else
        {
            ShowOutput("Passport Instance is null");
        }
    }

    public async void GetAddress()
    {
        ShowOutput($"Called GetAddress()...");
        try
        {
            string? address = await passport.GetAddress();
            ShowOutput(address ?? "No address");
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to get address: {e.Type}");
        }
        catch (Exception)
        {
            ShowOutput("Unable to get address");
        }
    }

    public async void Logout()
    {
        await passport.Logout();
        SceneManager.LoadScene(sceneName: "UnauthenticatedScene");
    }

    public async void GetAccessToken()
    {
        string? accessToken = await passport.GetAccessToken();
        ShowOutput(accessToken ?? "No access token");
    }

    public async void GetIdToken()
    {
        string? idToken = await passport.GetIdToken();
        ShowOutput(idToken ?? "No ID token");
    }

    public async void GetEmail()
    {
        string? email = await passport.GetEmail();
        ShowOutput(email ?? "No email");
    }

    public async void ShowTransfer()
    {
        authenticatedCanvas.gameObject.SetActive(false);
        transferCanvas.gameObject.SetActive(true);
    }

    public async void CancelTransfer()
    {
        authenticatedCanvas.gameObject.SetActive(true);
        transferCanvas.gameObject.SetActive(false);
        tokenIdInput.text = "";
        tokenAddressInput.text = "";
        receiverInput.text = "";
    }

    public async void Transfer()
    {
        if (tokenIdInput.text != "" && tokenAddressInput.text != "" && receiverInput.text != "")
        {
            transferButton.gameObject.SetActive(false);
            cancelTransferButton.gameObject.SetActive(false);

            try
            {
                UnsignedTransferRequest request = UnsignedTransferRequest.ERC721(
                    receiverInput.text,
                    tokenIdInput.text,
                    tokenAddressInput.text
                    );
                CreateTransferResponseV1 response = await passport.ImxTransfer(request);
                ShowOutput($"Transferred successfully. Transfer id: {response.TransferId}");
                tokenIdInput.text = "";
                tokenAddressInput.text = "";
                receiverInput.text = "";
            }
            catch (Exception e)
            {
                ShowOutput($"Unable to transfer: {e.Message}");
            }

            transferButton.gameObject.SetActive(true);
            cancelTransferButton.gameObject.SetActive(true);
        }
    }

    private void ShowOutput(string message)
    {
        if (output != null)
        {
            output.text = message;
        }
    }
}
