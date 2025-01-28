using System;
using System.Collections.Generic;
using System.Linq;

namespace Immutable.Marketplace
{
    public class LinkFactory
    {
        /// <summary>
        /// Generates a URL for the on-ramp flow.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>Sandbox</c> or <c>Production</c>).</param>
        /// <param name="email">The user's email address, pre-filled in the on-ramp flow.</param>
        /// <param name="address">The user's wallet address, where tokens will be sent.</param>
        /// <param name="queryParams">The query parameters for the on-ramp flow. Uses default values if not specified.</param>
        /// <returns>The generated on-ramp URL.</returns>
        public static string GenerateOnRampLink(
            Environment environment,
            string email,
            string address,
            OnRampQueryParams queryParams = default
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.OnRamp);
            var apiKey = LinkConfig.GetApiKey(environment, Flow.OnRamp);

            var queryParamsDictionary = new Dictionary<string, string>
            {
                {"apiKey", apiKey},
                {"network", "immutablezkevm"},
                {"defaultPaymentMethod", "credit_debit_card"},
                {"disablePaymentMethods", ""},
                {"productsAvailed", "buy"},
                {"exchangeScreenTitle", "Buy"},
                {"themeColor", "0D0D0D"},
                {"defaultCryptoCurrency", queryParams.CryptoCurrency},
                {"email", Uri.EscapeDataString(email)},
                {"isAutoFillUserData", "true"},
                {"disableWalletAddressForm", "true"},
                {"defaultFiatAmount", queryParams.FiatAmount},
                {"defaultFiatCurrency", queryParams.FiatCurrency},
                {"walletAddress", address},
                {"cryptoCurrencyList", queryParams.CryptoCurrencyList}
            };

            var queryString = string.Join("&", queryParamsDictionary.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Generates a link for the swap flow.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>Sandbox</c> or <c>Production</c>).</param>
        /// <param name="publishableKey">The publishable key obtained from <a href="https://hub.immutable.com/">Immutable Hub</a>. See <a href="https://docs.immutable.com/api/zkEVM/apikeys">API keys</a> for more details.</param>
        /// <param name="queryParams">The query parameters for the swap flow. Uses default values if not specified.</param>
        /// <returns>A swap URL.</returns>
        public static string GenerateSwapLink(
            Environment environment,
            string publishableKey,
            SwapQueryParams queryParams = default
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.Swap);

            var queryParamsDictionary = new Dictionary<string, string>
            {
                {"publishableKey", publishableKey}
            };

            if (!string.IsNullOrEmpty(queryParams.FromTokenAddress))
            {
                queryParamsDictionary["fromTokenAddress"] = queryParams.FromTokenAddress;
            }

            if (!string.IsNullOrEmpty(queryParams.ToTokenAddress))
            {
                queryParamsDictionary["toTokenAddress"] = queryParams.ToTokenAddress;
            }

            var queryString = string.Join("&", queryParamsDictionary.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Generates a link for the bridge flow.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>Sandbox</c> or <c>Production</c>).</param>
        /// <param name="queryParams">The query parameters for the bridge flow. Uses default values if not specified.</param>
        /// <returns>A bridge URL.</returns>
        public static string GenerateBridgeLink(
            Environment environment,
            BridgeQueryParams queryParams = default
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.Bridge);

            var queryParamsDictionary = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(queryParams.FromTokenAddress))
                queryParamsDictionary["fromToken"] = queryParams.FromTokenAddress.ToLower();

            if (!string.IsNullOrEmpty(queryParams.FromChainID))
                queryParamsDictionary["fromChain"] = queryParams.FromChainID;

            if (!string.IsNullOrEmpty(queryParams.ToTokenAddress))
                queryParamsDictionary["toToken"] = queryParams.ToTokenAddress.ToLower();

            if (!string.IsNullOrEmpty(queryParams.ToChainID))
                queryParamsDictionary["toChain"] = queryParams.ToChainID;

            var queryString = string.Join("&", queryParamsDictionary.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}
