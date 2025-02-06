using UnityEngine;
using UnityEngine.SceneManagement;

public class MarketplaceScript : MonoBehaviour
{
    public void OnRamp()
    {
        SceneManager.LoadScene("OnRampScene");
    }

    public void Swap()
    {
        SceneManager.LoadScene("SwapScene");
    }

    public void Bridge()
    {
        SceneManager.LoadScene("BridgeScene");
    }

}
