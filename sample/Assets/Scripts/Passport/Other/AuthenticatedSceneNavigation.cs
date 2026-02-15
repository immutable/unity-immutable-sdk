using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthenticatedSceneNavigation : MonoBehaviour
{
    // zkEVM Features
    public void ShowZkEvmGetBalance()
    {
        SceneManager.LoadScene("ZkEvmGetBalance");
    }

    public void ShowZkEvmSendTransaction()
    {
        SceneManager.LoadScene("ZkEvmSendTransaction");
    }

    public void ShowZkEvmGetTransactionReceipt()
    {
        SceneManager.LoadScene("ZkEvmGetTransactionReceipt");
    }

    public void ShowZkEvmSignTypedData()
    {
        SceneManager.LoadScene("ZkEvmSignTypedData");
    }

    public void ShowZkEvmRequestAccounts()
    {
        SceneManager.LoadScene("ZkEvmRequestAccounts");
    }

    public void ShowSetCallTimeout()
    {
        SceneManager.LoadScene("SetCallTimeout");
    }

    public void ShowLinkWallet()
    {
        SceneManager.LoadScene("Passport/LinkWallet");
    }

    // Add more navigation methods as needed for other features
}