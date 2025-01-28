using System;
using System.Collections.Generic;
using AltWebSocketSharp;
using Immutable.Marketplace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Environment = Immutable.Marketplace.Environment;

public class OnRampScript : MonoBehaviour
{
    [SerializeField] private Dropdown EnvironmentDropdown;
    [SerializeField] private InputField EmailInput;
    [SerializeField] private InputField AddressInput;

    [SerializeField] private InputField FiatCurrencyInput;
    [SerializeField] private InputField FiatAmountInput;
    [SerializeField] private InputField CryptoCurrency;
    [SerializeField] private InputField CryptoCurrencyList;

    [SerializeField] private Button OpenButton;

    private void Start()
    {
        OpenButton.interactable = false;

        // Enable the button when email and wallet address fields are populated
        EmailInput.onValueChanged.AddListener(_ => ValidateInputFields());
        AddressInput.onValueChanged.AddListener(_ => ValidateInputFields());
    }

    /// <summary>
    /// Validates input fields and enables the open button if both email and address are entered.
    /// </summary>
    private void ValidateInputFields()
    {
        OpenButton.interactable = !EmailInput.text.IsNullOrEmpty() && !AddressInput.text.IsNullOrEmpty();
    }

    /// <summary>
    /// Opens the on-ramp widget with specified inputs, defaulting to pre-set values when fields are empty.
    /// </summary>
    public void OpenWidget()
    {
        var environments = (Environment[])Enum.GetValues(typeof(Environment));
        var environment = environments[EnvironmentDropdown.value];
        var email = EmailInput.text;
        var walletAddress = AddressInput.text;

        var link = LinkFactory.GenerateOnRampLink(
            environment: environment,
            email: email,
            address: walletAddress,
            queryParams: new OnRampQueryParams
            {
                DefaultFiatCurrency = FiatCurrencyInput.text.IsNullOrEmpty() ? "USD" : FiatCurrencyInput.text,
                DefaultFiatAmount = FiatAmountInput.text.IsNullOrEmpty() ? "50" : FiatAmountInput.text,
                DefaultCryptoCurrency = CryptoCurrency.text.IsNullOrEmpty() ? "IMX" : CryptoCurrency.text,
                CryptoCurrencyList = CryptoCurrencyList.text.IsNullOrEmpty() ? "imx,eth,usdc" : CryptoCurrencyList.text
            },
            extraQueryParams: new Dictionary<string, string> {
                {"themeColor", "000000"}
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