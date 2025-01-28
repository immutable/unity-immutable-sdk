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
        /// <param name="fiatCurrency">The fiat currency to use (default: "USD").</param>
        /// <param name="fiatAmount">The amount of fiat currency to spend when purchasing cryptocurrency (default: "50").</param>
        /// <param name="cryptoCurrency">The cryptocurrency to purchase (default: "IMX").</param>
        /// <param name="cryptoCurrencyList">A comma-separated list of available cryptocurrencies for purchase (default: "imx,eth,usdc").</param>
        /// <returns>The generated on-ramp URL.</returns>
        public static string GenerateOnRampLink(
            Environment environment,
            string email,
            string address,
            string fiatCurrency = "USD",
            string fiatAmount = "50",
            string cryptoCurrency = "IMX",
            string cryptoCurrencyList = "imx,eth,usdc"
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.OnRamp);
            var apiKey = LinkConfig.GetApiKey(environment, Flow.OnRamp);

            var queryParams = new Dictionary<string, string>
            {
                {"apiKey", apiKey},
                {"network", "immutablezkevm"},
                {"defaultPaymentMethod", "credit_debit_card"},
                {"disablePaymentMethods", ""},
                {"productsAvailed", "buy"},
                {"exchangeScreenTitle", "Buy"},
                {"themeColor", "0D0D0D"},
                {"defaultCryptoCurrency", cryptoCurrency},
                {"email", Uri.EscapeDataString(email)},
                {"isAutoFillUserData", "true"},
                {"disableWalletAddressForm", "true"},
                {"defaultFiatAmount", fiatAmount},
                {"defaultFiatCurrency", fiatCurrency},
                {"walletAddress", address},
                {"cryptoCurrencyList", cryptoCurrencyList}
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Generates a link for the swap flow.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>Sandbox</c> or <c>Production</c>).</param>
        /// <param name="publishableKey">The publishable key obtained from <a href="https://hub.immutable.com/">Immutable Hub</a>. See <a href="https://docs.immutable.com/api/zkEVM/apikeys">API keys</a> for more details.</param>
        /// <param name="fromTokenAddress">The address of the token being swapped from (default is null).</param>
        /// <param name="toTokenAddress">The address of the token being swapped to (default is null).</param>
        /// <returns>A swap URL</returns>
        public static string GenerateSwapLink(
            Environment environment,
            string publishableKey,
            string? fromTokenAddress = null,
            string? toTokenAddress = null
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.Swap);

            var queryParams = new Dictionary<string, string>
            {
                {"publishableKey", publishableKey}
            };

            if (!string.IsNullOrEmpty(fromTokenAddress))
            {
                queryParams["fromTokenAddress"] = fromTokenAddress;
            }

            if (!string.IsNullOrEmpty(toTokenAddress))
            {
                queryParams["toTokenAddress"] = toTokenAddress;
            }

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// Generates a link for the bridge flow.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>Sandbox</c> or <c>Production</c>).</param>
        /// <param name="fromTokenAddress">The address of the token being moved from (default is null).</param>
        /// <param name="fromChainID">The ID of the source blockchain (default is null).</param>
        /// <param name="toTokenAddress">The address of the token being moved to (default is null).</param>
        /// <param name="toChainID">The ID of the destination blockchain (default is null).</param>
        /// <returns>A bridge URL.</returns>
        public static string GenerateBridgeLink(
            Environment environment,
            string? fromTokenAddress = null,
            string? fromChainID = null,
            string? toTokenAddress = null,
            string? toChainID = null
        )
        {
            var baseUrl = LinkConfig.GetBaseUrl(environment, Flow.Bridge);

            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(fromTokenAddress))
                queryParams["fromToken"] = fromTokenAddress.ToLower();

            if (!string.IsNullOrEmpty(fromChainID))
                queryParams["fromChain"] = fromChainID;

            if (!string.IsNullOrEmpty(toTokenAddress))
                queryParams["toToken"] = toTokenAddress.ToLower();

            if (!string.IsNullOrEmpty(toChainID))
                queryParams["toChain"] = toChainID;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}