using System;
using AltWebSocketSharp;
using Immutable.Marketplace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Environment = Immutable.Marketplace.Environment;

public class SwapScript : MonoBehaviour
{
    [SerializeField] private Dropdown EnvironmentDropdown;
    [SerializeField] private InputField KeyInput;

    [SerializeField] private InputField FromTokenAddress;
    [SerializeField] private InputField ToTokenAddress;

    [SerializeField] private Button OpenButton;

    private void Start()
    {
        OpenButton.interactable = false;

        // Enable the button when publishable key field is populated
        KeyInput.onValueChanged.AddListener(_ => ValidateInputFields());
    }

    /// <summary>
    /// Validates input field and enables the open button if publishable key is entered.
    /// </summary>
    private void ValidateInputFields()
    {
        OpenButton.interactable = !KeyInput.text.IsNullOrEmpty();
    }

    /// <summary>
    /// Opens the swap widget with specified inputs, defaulting to pre-set values when fields are empty.
    /// </summary>
    public void OpenWidget()
    {
        var environments = (Environment[])Enum.GetValues(typeof(Environment));
        var environment = environments[EnvironmentDropdown.value];
        var publishableKey = KeyInput.text;

        var link = LinkFactory.GenerateSwapLink(
            environment: environment,
            publishableKey: publishableKey,
            fromTokenAddress: FromTokenAddress.text.IsNullOrEmpty() ? null : FromTokenAddress.text,
            toTokenAddress: ToTokenAddress.text.IsNullOrEmpty() ? null : ToTokenAddress.text
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
