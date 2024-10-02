using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immutable.Marketplace.OnRamp
{
    public class OnRamp
    {
        private readonly string _environment;
        private readonly string _email;
        private readonly string _address;
        private static readonly Dictionary<string, string> TransakBaseUrls = new Dictionary<string, string>
        {
            { "sandbox", "https://global-stg.transak.com" },
            { "production", "https://global.transak.com/" }
        };

        private static readonly Dictionary<string, string> TransakApiKeys = new Dictionary<string, string>
        {
            { "sandbox", "d14b44fb-0f84-4db5-affb-e044040d724b" }, // This can be hardcoded as it is a public API key
            { "production", "ad1bca70-d917-4628-bb0f-5609537498bc" }
        };

        public OnRamp(string environment, string email, string address)
        {
            _environment = environment;
            _email = email;
            _address = address;
        }

        public async UniTask<string> GetLink(
            string fiatCurrency = "USD",
            string defaultFiatAmount = "50",
            string defaultCryptoCurrency = "IMX",
            string networks = "immutablezkevm",
            bool disableWalletAddressForm = true
        )
        {
            string baseUrl = TransakBaseUrls[_environment];
            string apiKey = TransakApiKeys[_environment];

            var queryParams = new Dictionary<string, string>
        {
            {"apiKey", apiKey},
            {"network", networks},
            {"defaultPaymentMethod", "credit_debit_card"},
            {"disablePaymentMethods", ""},
            {"productsAvailed", "buy"},
            {"exchangeScreenTitle", "Buy"},
            {"themeColor", "0D0D0D"},
            {"defaultCryptoCurrency", defaultCryptoCurrency},
            {"email", Uri.EscapeDataString(_email)},
            {"isAutoFillUserData", "true"},
            {"disableWalletAddressForm", disableWalletAddressForm.ToString().ToLower()},
            {"defaultFiatAmount", defaultFiatAmount},
            {"defaultFiatCurrency", fiatCurrency},
            {"walletAddress", _address},
            {"cryptoCurrencyList", "imx,eth,usdc"}
        };

            string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}