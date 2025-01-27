using AltWebSocketSharp;
using Immutable.Marketplace.Bridge;
using Immutable.Marketplace.OnRamp;
using Immutable.Marketplace.Swap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BridgeScript : MonoBehaviour
{
    [SerializeField] private Dropdown Environment;

    [SerializeField] private InputField FromTokenAddress;
    [SerializeField] private InputField FromChain;
    [SerializeField] private InputField ToTokenAddress;
    [SerializeField] private InputField ToChain;
    
    [SerializeField] private Button OpenButton;

    /// <summary>
    /// Opens the bridge widget with specified inputs, defaulting to pre-set values when fields are empty.
    /// </summary>
    public void OpenWidget()
    {
        var environment = Environment.options[Environment.value].text;

        var bridge = new Bridge(environment);

        var link = bridge.GetLink(
            fromTokenAddress: FromTokenAddress.text.IsNullOrEmpty() ? null : FromTokenAddress.text,
            fromChain: FromChain.text.IsNullOrEmpty() ? null : FromChain.text,
            toTokenAddress: ToTokenAddress.text.IsNullOrEmpty() ? null : ToTokenAddress.text,
            toChain: ToChain.text.IsNullOrEmpty() ? null : ToChain.text
        );

        Application.OpenURL(link);
    }

    /// <summary>
    /// Returns to the marketplace scene.
    /// </summary>
    public void Cancel()
    {
        SceneManager.LoadScene("MarketplaceScene");
    }

}
