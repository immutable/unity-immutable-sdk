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
        private readonly string _publishableKey;
        private static readonly Dictionary<string, string> BaseUrls = new()
        {
            { "sandbox", "https://checkout-playground.sandbox.immutable.com/checkout/swap" },
            { "production", "https://toolkit.immutable.com/checkout/swap" }
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="Swap"/> class.
        /// </summary>
        /// <param name="environment">The environment to use (<c>sandbox</c> or <c>production</c>).</param>
        /// <param name="publishableKey">The publishable key obtained from <a href="https://hub.immutable.com/">Immutable Hub</a>.  
        /// See <a href="https://docs.immutable.com/api/zkEVM/apikeys">API keys</a> for more details.</param>
        public Swap(string environment, string publishableKey)
        {
            _environment = environment;
            _publishableKey = publishableKey;
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

            var queryParams = new Dictionary<string, string>
            {
                {"publishableKey", _publishableKey}
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