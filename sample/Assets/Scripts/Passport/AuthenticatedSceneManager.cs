using UnityEngine;
using UnityEngine.UI;
using System;

public class AuthenticatedSceneManager : MonoBehaviour
{
    [Header("IMX Buttons")]
    [SerializeField] private Button ConnectButton;
    [SerializeField] private Button IsRegisteredOffchainButton;
    [SerializeField] private Button RegisterOffchainButton;
    [SerializeField] private Button GetAddressButton;
    [SerializeField] private Button NftTransferButton;

    [Header("zkEVM Buttons")]
    [SerializeField] private Button ConnectEvmButton;
    [SerializeField] private Button SendTransactionButton;
    [SerializeField] private Button RequestAccountsButton;
    [SerializeField] private Button GetBalanceButton;
    [SerializeField] private Button GetTransactionReceiptButton;
    [SerializeField] private Button SignTypedDataButton;

    public Action OnImxConnected;

    void Awake()
    {
        OnImxConnected = () => { UpdateImxButtonStates(); };
    }

    void Start()
    {
        UpdateImxButtonStates();
        UpdateZkEvmButtonStates();
    }

    public void UpdateImxButtonStates()
    {
        bool isConnected = SampleAppManager.IsConnectedToImx;
        if (ConnectButton != null) ConnectButton.gameObject.SetActive(!isConnected);
        if (IsRegisteredOffchainButton != null) IsRegisteredOffchainButton.gameObject.SetActive(isConnected);
        if (RegisterOffchainButton != null) RegisterOffchainButton.gameObject.SetActive(isConnected);
        if (GetAddressButton != null) GetAddressButton.gameObject.SetActive(isConnected);
        if (NftTransferButton != null) NftTransferButton.gameObject.SetActive(isConnected);
    }

    public void UpdateZkEvmButtonStates()
    {
        bool isConnected = SampleAppManager.IsConnectedToZkEvm;
        if (ConnectEvmButton != null) ConnectEvmButton.gameObject.SetActive(!isConnected);
        if (SendTransactionButton != null) SendTransactionButton.gameObject.SetActive(isConnected);
        if (RequestAccountsButton != null) RequestAccountsButton.gameObject.SetActive(isConnected);
        if (GetBalanceButton != null) GetBalanceButton.gameObject.SetActive(isConnected);
        if (GetTransactionReceiptButton != null) GetTransactionReceiptButton.gameObject.SetActive(isConnected);
        if (SignTypedDataButton != null) SignTypedDataButton.gameObject.SetActive(isConnected);
    }

    public static void NavigateToAuthenticatedScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("AuthenticatedScene");
    }

    public static void NavigateToUnauthenticatedScene()
    {
        // Navigate to the main initialization scene with PassportUI instead
        UnityEngine.SceneManagement.SceneManager.LoadScene("InitialisationWithUI");
    }
}