using System;
using System.Collections.Generic;
using System.Linq;

namespace Immutable.Marketplace.OnRamp
{
    /// <summary>
    /// Provides functionality for generating an on-ramp link, allowing players to purchase tokens 
    /// using fiat currency and transfer them directly to the Immutable zkEVM network.
    /// </summary>
    public class OnRamp
    {
        private readonly string _environment;
        private readonly string _email;
        private readonly string _address;
        private static readonly Dictionary<string, string> BaseUrls = new()
        {
            { "sandbox", "https://global-stg.transak.com" },
            { "production", "https://global.transak.com" }
        };

        private static readonly Dictionary<string, string> ApiKeys = new()
        {
            { "sandbox", "d14b44fb-0f84-4db5-affb-e044040d724b" }, // This can be hardcoded as it is a public API key
            { "production", "ad1bca70-d917-4628-bb0f-5609537498bc" }
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="OnRamp"/> class.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>sandbox</c> or <c>production</c>).</param>
        /// <param name="email">The user's email address, pre-filled in the on-ramp flow.</param>
        /// <param name="address">The user's wallet address, where tokens will be sent.</param>
        public OnRamp(string environment, string email, string address)
        {
            _environment = environment;
            _email = email;
            _address = address;
        }

        /// <summary>
        /// Generates a link for the on-ramp flow.
        /// </summary>
        /// <param name="fiatCurrency">The fiat currency to be used (default is "USD").</param>
        /// <param name="defaultFiatAmount">The default amount of fiat currency (default is "50").</param>
        /// <param name="defaultCryptoCurrency">The default cryptocurrency (default is "IMX")..</param>
        /// <param name="defaultCryptoCurrencyList">A comma-separated list of available cryptocurrencies to purchase (default is "imx,eth,usdc").</param>
        /// <returns>An on-ramp URL</returns>
        public string GetLink(
            string fiatCurrency = "USD",
            string defaultFiatAmount = "50",
            string defaultCryptoCurrency = "IMX",
            string defaultCryptoCurrencyList = "imx,eth,usdc"
        )
        {
            var baseUrl = BaseUrls[_environment];
            var apiKey = ApiKeys[_environment];

            var queryParams = new Dictionary<string, string>
        {
            {"apiKey", apiKey},
            {"network", "immutablezkevm"},
            {"defaultPaymentMethod", "credit_debit_card"},
            {"disablePaymentMethods", ""},
            {"productsAvailed", "buy"},
            {"exchangeScreenTitle", "Buy"},
            {"themeColor", "0D0D0D"},
            {"defaultCryptoCurrency", defaultCryptoCurrency},
            {"email", Uri.EscapeDataString(_email)},
            {"isAutoFillUserData", "true"},
            {"disableWalletAddressForm", "true"},
            {"defaultFiatAmount", defaultFiatAmount},
            {"defaultFiatCurrency", fiatCurrency},
            {"walletAddress", _address},
            {"cryptoCurrencyList", defaultCryptoCurrencyList}
        };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}