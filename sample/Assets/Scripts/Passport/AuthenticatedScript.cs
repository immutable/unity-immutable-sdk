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
    [SerializeField] private Button NftTransferButton;

    // ZkEvm
    [SerializeField] private Button ConnectEvmButton;
    [SerializeField] private Button SendTransactionButton;
    [SerializeField] private Button RequestAccountsButton;
    [SerializeField] private Button GetBalanceButton;
    [SerializeField] private Button GetTransactionReceiptButton;
    [SerializeField] private Button SignTypedDataButton;

    // Other
    [SerializeField] private Button LaunchBrowserButton;

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

#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))
        LaunchBrowserButton.gameObject.SetActive(true);
#else
        LaunchBrowserButton.gameObject.SetActive(false);
#endif
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
        NftTransferButton.gameObject.SetActive(isConnected);
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
        SignTypedDataButton.gameObject.SetActive(isConnected);
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
    /// Navigates to Link Wallet scene.
    /// </summary>
    public void ShowLinkWallet()
    {
        SceneManager.LoadScene("LinkWallet");
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
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX || UNITY_WEBGL
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

    #region IMX

    /// <summary>
    /// Initialises the user's wallet and sets up the Immutable X provider using saved credentials if the user is already logged in.
    /// </summary>
    public async void Connect()
    {
        ShowOutput("Connecting to Passport using saved credentials...");
        ConnectButton.gameObject.SetActive(false);

        try
        {
            // Attempt to connect to Immutable X using saved credentials
            bool isConnected = await Passport.ConnectImx(useCachedSession: true);

            // Update connection status
            SampleAppManager.IsConnectedToImx = isConnected;

            if (isConnected)
            {
                // Enable UI elements related to Immutable X upon successful connection
                IsRegisteredOffchainButton.gameObject.SetActive(true);
                RegisterOffchainButton.gameObject.SetActive(true);
                GetAddressButton.gameObject.SetActive(true);
                NftTransferButton.gameObject.SetActive(true);

                ShowOutput("Connected to IMX");
            }
            else
            {
                ShowOutput("Could not connect using saved credentials");
                ConnectButton.gameObject.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            ShowOutput($"Error connecting: {ex.Message}");
            ConnectButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Checks if the user is registered off-chain with Immutable X.
    /// </summary>
    public async void IsRegisteredOffchain()
    {
        ShowOutput("Checking if user is registered off-chain...");

        try
        {
            bool isRegistered = await Passport.IsRegisteredOffchain();
            ShowOutput(isRegistered ? "Registered" : "Not registered");
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to check off-chain registration: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to check off-chain registration");
        }
    }

    /// <summary>
    /// Registers the user with Immutable X if they are not already registered.
    /// </summary>
    public async void RegisterOffchain()
    {
        ShowOutput("Registering off-chain...");

        try
        {
            RegisterUserResponse response = await Passport.RegisterOffchain();

            if (response != null)
            {
                ShowOutput($"Successfully registered");
            }
            else
            {
                ShowOutput("Registration failed");
            }
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to register off-chain: {e.Message} ({e.Type})");
        }
        catch (Exception e)
        {
            ShowOutput($"Unable to register off-chain {e.Message}");
        }
    }

    /// <summary>
    /// Gets the wallet address of the currently logged-in user.
    /// </summary>
    public async void GetAddress()
    {
        ShowOutput("Retrieving wallet address...");

        try
        {
            string address = await Passport.GetAddress();
            ShowOutput(string.IsNullOrEmpty(address) ? "No address found" : address);
        }
        catch (PassportException e)
        {
            ShowOutput($"Unable to retrieve address: {e.Message} ({e.Type})");
        }
        catch (Exception)
        {
            ShowOutput("Unable to retrieve address");
        }
    }

    /// <summary>
    /// Navigates to IMX NFT Transfer scene.
    /// </summary>
    public void ShowImxNftTransfer()
    {
        SceneManager.LoadScene("ImxNftTransfer");
    }

    #endregion

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
            SignTypedDataButton.gameObject.SetActive(true);

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

    /// <summary>
    /// Navigates to zkEVM Sign Typed Data scene.
    /// </summary>
    public void ShowZkEvmSignTypedData()
    {
        SceneManager.LoadScene("ZkEvmSignTypedData");
    }

    #endregion

    #region Other

    /// <summary>
    /// Clears the underlying WebView storage and cache, including any saved credentials.
    /// </summary>
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
    /// Navigates to Set Call Timeout scene.
    /// </summary>
    public void ShowSetCallTimeout()
    {
        SceneManager.LoadScene("SetCallTimeout");
    }

    /// <summary>
    /// Navigates to Launch Browser scene.
    /// </summary>
    public void LaunchBrowser()
    {
        SceneManager.LoadScene("LaunchBrowser");
    }

    #endregion

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
