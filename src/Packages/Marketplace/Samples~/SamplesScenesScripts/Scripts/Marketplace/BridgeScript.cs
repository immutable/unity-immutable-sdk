using System;
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
            queryParams: new BridgeQueryParams
            {
                FromTokenAddress = string.IsNullOrEmpty(FromTokenAddress.text) ? null : FromTokenAddress.text,
                FromChainID = string.IsNullOrEmpty(FromChain.text) ? null : FromChain.text,
                ToTokenAddress = string.IsNullOrEmpty(ToTokenAddress.text) ? null : ToTokenAddress.text,
                ToChainID = string.IsNullOrEmpty(ToChain.text) ? null : ToChain.text
            }
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
