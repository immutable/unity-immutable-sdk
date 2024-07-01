using System.Globalization;
using System.Numerics;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;
using Immutable.Passport.Event;
using Unity.VisualScripting;

public class AuthenticatedScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private Text Output;

    [SerializeField] private Canvas AuthenticatedCanvas;
    [SerializeField] private Button AccessTokenButton;
    [SerializeField] private Button IdTokenButton;
    [SerializeField] private Button LogoutButton;

    // IMX
    [SerializeField] private Button ConnectButton;
    [SerializeField] private Button IsRegisteredOffchainButton;
    [SerializeField] private Button RegisterOffchainButton;
    [SerializeField] private Button GetAddressButton;
    [SerializeField] private Button ShowTransferButton;

    [SerializeField] private Canvas TransferCanvas;

    [SerializeField] private InputField TokenIdInput1;
    [SerializeField] private InputField TokenAddressInput1;
    [SerializeField] private InputField ReceiverInput1;

    [SerializeField] private InputField TokenIdInput2;
    [SerializeField] private InputField TokenAddressInput2;
    [SerializeField] private InputField ReceiverInput2;

    [SerializeField] private InputField TokenIdInput3;
    [SerializeField] private InputField TokenAddressInput3;
    [SerializeField] private InputField ReceiverInput3;

    [SerializeField] private Button TransferButton;
    [SerializeField] private Button CancelTransferButton;

    // ZkEvm
    [SerializeField] private Button ConnectEvmButton;
    [SerializeField] private Button SendTransactionButton;
    [SerializeField] private Button RequestAccountsButton;
    [SerializeField] private Button GetBalanceButton;
    [SerializeField] private Button GetTransactionReceiptButton;

    // ZkEVM Get Balance Transaction
    [SerializeField] private Canvas ZkGetBalanceCanvas;
    [SerializeField] private InputField ZkGetBalanceAccount;

    // ZkEVM Send Transaction
    [SerializeField] private Toggle ZkSendTransactionConfirm;
    [SerializeField] private Canvas ZkSendTransactionCanvas;
    [SerializeField] private InputField ZkSendTransactionTo;
    [SerializeField] private InputField ZkSendTransactionValue;
    [SerializeField] private InputField ZkSendTransactionData;

    // ZkEVM Get Transaction Receipt
    [SerializeField] private Canvas ZkGetTransactionReceiptCanvas;
    [SerializeField] private InputField ZkGetTransactionReceiptHash;

    private Passport passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            passport = Passport.Instance;
            ConnectButton.gameObject.SetActive(!SampleAppManager.IsConnected);
            IsRegisteredOffchainButton.gameObject.SetActive(SampleAppManager.IsConnected);
            RegisterOffchainButton.gameObject.SetActive(SampleAppManager.IsConnected);
            GetAddressButton.gameObject.SetActive(SampleAppManager.IsConnected);
            ShowTransferButton.gameObject.SetActive(SampleAppManager.IsConnected);

            // Listen to Passport Auth events
            passport.OnAuthEvent += OnPassportAuthEvent;
        }
        else
        {
            ShowOutput("Passport Instance is null");
        }
    }

    private void OnPassportAuthEvent(PassportAuthEvent authEvent)
    {
        Debug.Log($"OnPassportAuthEvent {authEvent.ToString()}");
    }

    public async void Connect()
    {
        try
        {
            // Use existing credentials to connect to Passport
            ShowOutput("Connecting into Passport using saved credentials...");
            ConnectButton.gameObject.SetActive(false);
            bool connected = await passport.ConnectImx(useCachedSession: true);
            if (connected)
            {
                IsRegisteredOffchainButton.gameObject.SetActive(true);
                RegisterOffchainButton.gameObject.SetActive(true);
                GetAddressButton.gameObject.SetActive(true);
                ShowTransferButton.gameObject.SetActive(true);
                ShowOutput($"Connected");
            }
            else
            {
                ShowOutput($"Could not connect using saved credentials");
                ConnectButton.gameObject.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Connect() error: {ex.Message}");
            ConnectButton.gameObject.SetActive(true);
        }
    }

    public async void IsRegisteredOffchain()
    {
        ShowOutput($"Called IsRegisteredOffchain()...");
        try
        {
            bool isRegistered = await passport.IsRegisteredOffchain();
            ShowOutput(isRegistered ? "Registered" : "Not registered");
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to check if user is registered off chain: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to check if user is registered off chain");
        }
    }

    public async void RegisterOffchain()
    {
        ShowOutput($"Called RegisterOffchain()...");
        try
        {
            RegisterUserResponse response = await passport.RegisterOffchain();
            if (response != null)
            {
                ShowOutput($"Registered {response.tx_hash}");
            }
            else
            {
                ShowOutput($"Not registered");
            }
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to register off chain: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to register off chain");
        }
    }

    public async void GetAddress()
    {
        ShowOutput($"Called GetAddress()...");
        try
        {
            string address = await passport.GetAddress();
            ShowOutput(address ?? "No address");
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to get address: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to get address");
        }
    }

    public async void Logout()
    {
        try
        {
            ShowOutput("Logging out...");
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
            await passport.LogoutPKCE();
#else
            await passport.Logout();
#endif
            SampleAppManager.IsConnected = false;
            passport.OnAuthEvent -= OnPassportAuthEvent;
            SceneManager.LoadScene(sceneName: "UnauthenticatedScene");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to log out: {ex.Message}");
        }
    }

    public async void GetAccessToken()
    {
        try
        {
            string accessToken = await passport.GetAccessToken();
            ShowOutput(accessToken);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get ID token: {ex.Message}");
        }
    }

    public async void GetIdToken()
    {
        try
        {
            string idToken = await passport.GetIdToken();
            ShowOutput(idToken);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get ID token: {ex.Message}");
        }
    }

    public async void GetEmail()
    {
        try
        {
            string email = await passport.GetEmail();
            ShowOutput(email);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get email: {ex.Message}");
        }
    }

    public async void GetLinkedAddresses()
    {
        try
        {
            List<string> addresses = await passport.GetLinkedAddresses();
            ShowOutput(addresses.Count > 0 ? String.Join(", ", addresses) : "No linked addresses");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get linked addresses: {ex.Message}");
        }
    }

    public async void GetPassportId()
    {
        try
        {
            string passportId = await passport.GetPassportId();
            ShowOutput(passportId);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get Passport ID: {ex.Message}");
        }
    }

    public void ShowTransfer()
    {
        AuthenticatedCanvas.gameObject.SetActive(false);
        TransferCanvas.gameObject.SetActive(true);
    }

    public void CancelTransfer()
    {
        AuthenticatedCanvas.gameObject.SetActive(true);
        TransferCanvas.gameObject.SetActive(false);
        ClearInputs();
    }

    public async void Transfer()
    {
        if (TokenIdInput1.text != "" && TokenAddressInput1.text != "" && ReceiverInput1.text != "")
        {
            ShowOutput("Transferring...");
            TransferButton.gameObject.SetActive(false);
            CancelTransferButton.gameObject.SetActive(false);

            try
            {
                List<NftTransferDetails> details = getTransferDetails();

                if (details.Count > 1)
                {
                    CreateBatchTransferResponse response = await passport.ImxBatchNftTransfer(details.ToArray());
                    ShowOutput($"Transferred {response.transfer_ids.Length} items successfully");
                }
                else
                {
                    UnsignedTransferRequest request = UnsignedTransferRequest.ERC721(
                        details[0].receiver,
                        details[0].tokenId,
                        details[0].tokenAddress
                    );
                    CreateTransferResponseV1 response = await passport.ImxTransfer(request);
                    ShowOutput($"Transferred successfully. Transfer id: {response.transfer_id}");
                }

                ClearInputs();
            }
            catch (Exception e)
            {
                ShowOutput($"Unable to transfer: {e.Message}");
            }

            TransferButton.gameObject.SetActive(true);
            CancelTransferButton.gameObject.SetActive(true);
        }
    }

    private List<NftTransferDetails> getTransferDetails()
    {
        List<NftTransferDetails> details = new List<NftTransferDetails>();

        details.Add(
            new NftTransferDetails(
                ReceiverInput1.text,
                TokenIdInput1.text,
                TokenAddressInput1.text
            )
        );

        if (TokenIdInput2.text != "" && TokenAddressInput2.text != "" && ReceiverInput2.text != "")
        {
            details.Add(
                new NftTransferDetails(
                    ReceiverInput2.text,
                    TokenIdInput2.text,
                    TokenAddressInput2.text
                )
            );
        }

        if (TokenIdInput3.text != "" && TokenAddressInput3.text != "" && ReceiverInput3.text != "")
        {
            details.Add(
                new NftTransferDetails(
                    ReceiverInput3.text,
                    TokenIdInput3.text,
                    TokenAddressInput3.text
                )
            );
        }

        return details;
    }

    // ZKEvm
    public async void ConnectEvm()
    {
        try
        {
            await passport.ConnectEvm();
            ShowOutput("Connected to EVM");
            ConnectEvmButton.gameObject.SetActive(false);
            SendTransactionButton.gameObject.SetActive(true);
            RequestAccountsButton.gameObject.SetActive(true);
            GetBalanceButton.gameObject.SetActive(true);
            GetTransactionReceiptButton.gameObject.SetActive(true);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to connect to EVM: {ex.Message}");
        }
    }

    public async void SendZkTransaction()
    {
        try
        {
            ShowOutput($"Called sendTransaction()...");
            TransactionRequest request = new TransactionRequest()
            {
                to = ZkSendTransactionTo.text,
                value = ZkSendTransactionValue.text,
                data = ZkSendTransactionData.text

            };
            if (ZkSendTransactionConfirm.isOn)
            {
                TransactionReceiptResponse response = await passport.ZkEvmSendTransactionWithConfirmation(request);
                ShowOutput($"Transaction hash: {response.transactionHash}\nStatus: {GetTransactionStatusString(response.status)}");
            }
            else
            {
                string response = await passport.ZkEvmSendTransaction(request);
                ShowOutput($"Transaction hash: {response}");
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to request accounts: {ex.Message}");
        }
    }

    public void ShowZkSendTransaction()
    {
        AuthenticatedCanvas.gameObject.SetActive(false);
        ZkSendTransactionCanvas.gameObject.SetActive(true);
        ZkSendTransactionTo.text = "";
        ZkSendTransactionValue.text = "";
        ZkSendTransactionData.text = "";
    }

    public void CancelZkSendTransaction()
    {
        AuthenticatedCanvas.gameObject.SetActive(true);
        ZkSendTransactionCanvas.gameObject.SetActive(false);
    }

    public async void RequestAccounts()
    {
        try
        {
            ShowOutput($"Called RequestAccounts()...");
            List<string> accounts = await passport.ZkEvmRequestAccounts();
            ShowOutput(String.Join(", ", accounts));
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to request accounts: {ex.Message}");
        }
    }

    public async void GetBalance()
    {
        try
        {
            ShowOutput($"Called GetBalance()...");
            string balance = await passport.ZkEvmGetBalance(ZkGetBalanceAccount.text);
            var balanceBI = BigInteger.Parse(balance.Replace("0x", "0"), NumberStyles.HexNumber);
            ShowOutput($"Hex: {balance}\nDec: {balanceBI.ToString()}");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get balance: {ex.Message}");
        }
    }

    public void ShowZkGetBalance()
    {
        AuthenticatedCanvas.gameObject.SetActive(false);
        ZkGetBalanceCanvas.gameObject.SetActive(true);
        ZkGetBalanceAccount.text = "";
    }

    public void CancelZkGetBalance()
    {
        AuthenticatedCanvas.gameObject.SetActive(true);
        ZkGetBalanceCanvas.gameObject.SetActive(false);
    }

    public async void GetZkTransactionReceiptStatus()
    {
        try
        {
            ShowOutput($"Getting zkEVM transaction receipt status...");

            TransactionReceiptResponse response = await passport.ZkEvmGetTransactionReceipt(ZkGetTransactionReceiptHash.text);
            string status = $"Transaction receipt status: {GetTransactionStatusString(response.status)}";
            ShowOutput(status);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get transaction receipt: {ex.Message}");
        }
    }

    private string GetTransactionStatusString(string status)
    {
        switch (status)
        {
            case "1":
            case "0x1":
                return "Success";
            case "0":
            case "0x0":
                return "Failed";
            case null:
                return "Still processing";
            default:
                return "";
        }
    }

    public void ShowZkTransactionReceiptStatus()
    {
        AuthenticatedCanvas.gameObject.SetActive(false);
        ZkGetTransactionReceiptCanvas.gameObject.SetActive(true);
        ZkGetTransactionReceiptHash.text = "";
    }

    public void CancelZkTransactionReceiptStatus()
    {
        AuthenticatedCanvas.gameObject.SetActive(true);
        ZkGetTransactionReceiptCanvas.gameObject.SetActive(false);
    }

    public void ClearStorageAndCache()
    {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        passport.ClearStorage();
        passport.ClearCache(true);
        ShowOutput("Cleared storage and cache");
#else
        ShowOutput("Support on Android and iOS devices only");
#endif
    }

    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }

    private void ClearInputs()
    {
        TokenIdInput1.text = "";
        TokenAddressInput1.text = "";
        ReceiverInput1.text = "";

        TokenIdInput2.text = "";
        TokenAddressInput2.text = "";
        ReceiverInput2.text = "";

        TokenIdInput3.text = "";
        TokenAddressInput3.text = "";
        ReceiverInput3.text = "";
    }
}
