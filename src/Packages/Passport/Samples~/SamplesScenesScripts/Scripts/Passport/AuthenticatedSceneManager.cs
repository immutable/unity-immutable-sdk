using UnityEngine;
using UnityEngine.UI;
using System;

public class AuthenticatedSceneManager : MonoBehaviour
{
    [Header("zkEVM Buttons")]
    [SerializeField] private Button ConnectEvmButton;
    [SerializeField] private Button SendTransactionButton;
    [SerializeField] private Button RequestAccountsButton;
    [SerializeField] private Button GetBalanceButton;
    [SerializeField] private Button GetTransactionReceiptButton;
    [SerializeField] private Button SignTypedDataButton;

    void Start()
    {
        UpdateZkEvmButtonStates();
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("UnauthenticatedScene");
    }
}