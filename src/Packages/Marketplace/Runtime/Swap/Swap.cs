using System;
using System.Collections.Generic;
using System.Linq;

namespace Immutable.Marketplace.Swap
{
    /// <summary>
    /// Provides functionality for generating a link to the swap flow, 
    /// simplifying the process of swapping tokens on the Immutable zkEVM network.
    /// </summary>
    public class Swap
    {
        private readonly string _environment;
        private static readonly Dictionary<string, string> BaseUrls = new()
        {
            { "sandbox", "https://checkout-playground.sandbox.immutable.com/checkout/swap" },
            { "production", "https://toolkit.immutable.com/checkout/swap" }
        };

        private static readonly Dictionary<string, string> ApiKeys = new()
        {
            { "sandbox", "pk_imapik-test-7-hfC5T$W$eEDE8Mc5mp" }, // This can be hardcoded as it is a public API key
            { "production", "pk_imapik-WGd9orNd8mLdtTCTb3CP" }
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="Swap"/> class.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>sandbox</c> or <c>production</c>).</param>
        public Swap(string environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Generates a link for the swap flow.
        /// </summary>
        /// <param name="fromTokenAddress">The address of the token being swapped from (default is null).</param>
        /// <param name="toTokenAddress">The address of the token being swapped to (default is null).</param>
        /// <returns>A swap URL</returns>
        public string GetLink(string? fromTokenAddress = null, string? toTokenAddress = null)
        {
            var baseUrl = BaseUrls[_environment];
            var apiKey = ApiKeys[_environment];

            var queryParams = new Dictionary<string, string>
            {
                {"publishableKey", apiKey}
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
    }
}