using AltWebSocketSharp;
using Immutable.Marketplace.OnRamp;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OnRampScript : MonoBehaviour
{
    [SerializeField] private Dropdown Environment;
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
        var environment = Environment.options[Environment.value].text;
        var email = EmailInput.text;
        var walletAddress = AddressInput.text;

        var onRamp = new OnRamp(environment, email, walletAddress);

        var link = onRamp.GetLink(
            fiatCurrency: FiatCurrencyInput.text.IsNullOrEmpty() ? "USD" : FiatCurrencyInput.text,
            fiatAmount: FiatAmountInput.text.IsNullOrEmpty() ? "50" : FiatAmountInput.text,
            cryptoCurrency: CryptoCurrency.text.IsNullOrEmpty() ? "IMX" : CryptoCurrency.text,
            cryptoCurrencyList: CryptoCurrencyList.text.IsNullOrEmpty() ? "imx,eth,usdc" : CryptoCurrencyList.text
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
