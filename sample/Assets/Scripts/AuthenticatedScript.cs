using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Model;

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

    private Passport Passport;
#pragma warning restore CS8618

    void Start()
    {
        if (Passport.Instance != null)
        {
            // Get Passport instance
            Passport = Passport.Instance;
            CheckIfConnectedToImx();
            CheckIfConnectedToZkEvm();
        }
        else
        {
            ShowOutput("Passport instance is null");
        }
    }

    /// <summary>
    /// Checks if the user is connected to IMX and updates the UI to show appropriate buttons.
    /// </summary>
    private void CheckIfConnectedToImx()
    {
        bool isConnected = SampleAppManager.IsConnectedToImx;
        ConnectButton.gameObject.SetActive(!isConnected);
        IsRegisteredOffchainButton.gameObject.SetActive(isConnected);
        RegisterOffchainButton.gameObject.SetActive(isConnected);
        GetAddressButton.gameObject.SetActive(isConnected);
        ShowTransferButton.gameObject.SetActive(isConnected);
    }

    /// <summary>
    /// Checks if the user is connected to zkEVM and updates the UI to show appropriate buttons.
    /// </summary>
    private void CheckIfConnectedToZkEvm()
    {
        bool isConnected = SampleAppManager.IsConnectedToZkEvm;
        ConnectEvmButton.gameObject.SetActive(!isConnected);
        SendTransactionButton.gameObject.SetActive(isConnected);
        RequestAccountsButton.gameObject.SetActive(isConnected);
        GetBalanceButton.gameObject.SetActive(isConnected);
        GetTransactionReceiptButton.gameObject.SetActive(isConnected);
    }

    #region Passport functions

    /// <summary>
    /// Retrieves the currently logged-in user's access token.
    /// </summary>
    public async void GetAccessToken()
    {
        try
        {
            string accessToken = await Passport.GetAccessToken();
            ShowOutput(accessToken);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get access token: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's ID token.
    /// </summary>
    public async void GetIdToken()
    {
        try
        {
            string idToken = await Passport.GetIdToken();
            ShowOutput(idToken);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get ID token: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's email.
    /// </summary>
    public async void GetEmail()
    {
        try
        {
            string email = await Passport.GetEmail();
            ShowOutput(email);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get email: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the currently logged-in user's Passport ID.
    /// </summary>
    public async void GetPassportId()
    {
        try
        {
            string passportId = await Passport.GetPassportId();
            ShowOutput(passportId);
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get Passport ID: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the user's linked external wallets from the Passport account dashboard.
    /// </summary>
    public async void GetLinkedAddresses()
    {
        try
        {
            List<string> addresses = await Passport.GetLinkedAddresses();
            ShowOutput(addresses.Count > 0 ? string.Join(", ", addresses) : "No linked addresses");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to get linked addresses: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs out of Passport using the selected auth method. 
    /// Defaults to Device Code Auth when running as a Windows Standalone application or in the Unity Editor on Windows.
    /// </summary>
    public async void Logout()
    {
        ShowOutput("Logging out...");

        try
        {
            // Logout using the appropriate logout method
            if (SampleAppManager.SupportsPKCE && SampleAppManager.UsePKCE)
            {
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
                await Passport.LogoutPKCE();
#endif
            }
            else
            {
                await Passport.Logout();
            }

            // Reset connection status and navigate to the unauthenticated scene
            SampleAppManager.IsConnectedToImx = false;
            SampleAppManager.IsConnectedToZkEvm = false;
            SceneManager.LoadScene(sceneName: "UnauthenticatedScene");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to logout: {ex.Message}");
        }
    }

    #endregion

    public async void Connect()
    {
        try
        {
            // Use existing credentials to connect to Passport
            ShowOutput("Connecting into Passport using saved credentials...");
            ConnectButton.gameObject.SetActive(false);
            bool connected = await Passport.ConnectImx(useCachedSession: true);
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
            bool isRegistered = await Passport.IsRegisteredOffchain();
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
            RegisterUserResponse response = await Passport.RegisterOffchain();
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
            string address = await Passport.GetAddress();
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
                    CreateBatchTransferResponse response = await Passport.ImxBatchNftTransfer(details.ToArray());
                    ShowOutput($"Transferred {response.transfer_ids.Length} items successfully");
                }
                else
                {
                    UnsignedTransferRequest request = UnsignedTransferRequest.ERC721(
                        details[0].receiver,
                        details[0].tokenId,
                        details[0].tokenAddress
                    );
                    CreateTransferResponseV1 response = await Passport.ImxTransfer(request);
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

    #region zkEVM

    /// <summary>
    /// Instantiates the zkEVM provider to enable interaction with zkEVM.
    /// </summary>
    public async void ConnectEvm()
    {
        try
        {
            // Instantiates the zkEVM provider
            await Passport.ConnectEvm();

            // Update connection status
            SampleAppManager.IsConnectedToZkEvm = true;

            // Update UI elements to show zkEVM-related buttons
            ConnectEvmButton.gameObject.SetActive(false);
            SendTransactionButton.gameObject.SetActive(true);
            RequestAccountsButton.gameObject.SetActive(true);
            GetBalanceButton.gameObject.SetActive(true);
            GetTransactionReceiptButton.gameObject.SetActive(true);

            ShowOutput("Connected to EVM");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to instantiate zkEVM provider: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialises the logged-in user's Passport wallet and retrieves their wallet address.
    /// </summary>
    public async void RequestAccounts()
    {
        ShowOutput("Requesting wallet accounts...");

        try
        {
            // Initialise the wallet and get the wallet addresses from the zkEVM provider
            List<string> accounts = await Passport.ZkEvmRequestAccounts();

            // Display the retrieved wallet addresses
            ShowOutput(accounts.Count > 0 ? string.Join(", ", accounts) : "No accounts found.");
        }
        catch (Exception ex)
        {
            ShowOutput($"Failed to request wallet accounts: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates to zkEVM Send Transaction scene.
    /// </summary>
    public void ShowZkEvmSendTransaction()
    {
        SceneManager.LoadScene("ZkEvmSendTransaction");
    }

    /// <summary>
    /// Navigates to zkEVM Get Balance scene.
    /// </summary>
    public void ShowZkEvmGetBalance()
    {
        SceneManager.LoadScene("ZkEvmGetBalance");
    }

    /// <summary>
    /// Navigates to zkEVM Get Transaction Receipt scene.
    /// </summary>
    public void ShowZkEvmGetTransactionReceipt()
    {
        SceneManager.LoadScene("ZkEvmGetTransactionReceipt");
    }

    #endregion

    public void ClearStorageAndCache()
    {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        Passport.ClearStorage();
        Passport.ClearCache(true);
        ShowOutput("Cleared storage and cache");
#else
        ShowOutput("Support on Android and iOS devices only");
#endif
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
