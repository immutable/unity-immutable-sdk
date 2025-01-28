using System;
using AltWebSocketSharp;
using Immutable.Marketplace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Environment = Immutable.Marketplace.Environment;

public class BridgeScript : MonoBehaviour
{
    [SerializeField] private Dropdown EnvironmentDropdown;

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
        var environments = (Environment[])Enum.GetValues(typeof(Environment));
        var environment = environments[EnvironmentDropdown.value];

        var link = LinkFactory.GenerateBridgeLink(
            environment: environment,
            fromTokenAddress: FromTokenAddress.text.IsNullOrEmpty() ? null : FromTokenAddress.text,
            fromChainID: FromChain.text.IsNullOrEmpty() ? null : FromChain.text,
            toTokenAddress: ToTokenAddress.text.IsNullOrEmpty() ? null : ToTokenAddress.text,
            toChainID: ToChain.text.IsNullOrEmpty() ? null : ToChain.text
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
