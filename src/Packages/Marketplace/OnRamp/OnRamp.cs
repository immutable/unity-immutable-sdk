using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Immutable.Marketplace.OnRamp
{
    public class OnRamp
    {
        private readonly Passport.Passport _passport;
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

        public OnRamp(Passport.Passport passport)
        {
            _passport = passport;
        }

        public async UniTask<string> GetLink(
    string fiatCurrency = "USD",
    string defaultFiatAmount = "50",
    string defaultCryptoCurrency = "IMX",
    string networks = "immutablezkevm",
    bool disableWalletAddressForm = true)
        {
            await _passport.ConnectImx();
            string environment = _passport.GetPassportImpl().environment;

            string email = await _passport.GetEmail();
            string walletAddress = await _passport.GetAddress();
            string baseUrl = TransakBaseUrls[environment];
            string apiKey = TransakApiKeys[environment];

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
            {"email", Uri.EscapeDataString(email)},
            {"isAutoFillUserData", "true"},
            {"disableWalletAddressForm", disableWalletAddressForm.ToString().ToLower()},
            {"defaultFiatAmount", defaultFiatAmount},
            {"defaultFiatCurrency", fiatCurrency},
            {"walletAddress", walletAddress},
            {"cryptoCurrencyList", "imx,eth,usdc"}
        };

            string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}