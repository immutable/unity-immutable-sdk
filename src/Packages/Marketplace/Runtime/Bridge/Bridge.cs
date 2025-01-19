using System;
using System.Collections.Generic;
using System.Linq;

namespace Immutable.Marketplace.Bridge
{
    /// <summary>
    /// Provides functionality for generating a link to the bridge flow, 
    /// simplifying the process of moving tokens from and to the Immutable zkEVM network.
    /// </summary>
    public class Bridge
    {
        private readonly string _environment;
        private static readonly Dictionary<string, string> BaseUrls = new()
        {
            { "sandbox", "https://checkout-playground.sandbox.immutable.com/checkout/squid" },
            { "production", "https://toolkit.immutable.com/checkout/squid" }
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="Bridge"/> class.
        /// </summary>
        /// <param name="environment">Specifies the environment (<c>sandbox</c> or <c>production</c>).</param>
        public Bridge(string environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Generates a link for the bridge flow.
        /// </summary>
        /// <param name="fromTokenAddress">The address of the token being moved from (default is null).</param>
        /// <param name="fromChain">The ID of the source blockchain (default is null).</param>
        /// <param name="toTokenAddress">The address of the token being moved to (default is null).</param>
        /// <param name="toChain">The ID of the destination blockchain (default is null).</param>
        /// <returns>A bridge URL.</returns>
        public string GetLink(string? fromTokenAddress = null, string? fromChain = null, string? toTokenAddress = null, string? toChain = null)
        {
            var baseUrl = BaseUrls[_environment];

            var queryParams = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(fromTokenAddress))
                queryParams["fromToken"] = fromTokenAddress;

            if (!string.IsNullOrEmpty(fromChain))
                queryParams["fromChain"] = fromChain;

            if (!string.IsNullOrEmpty(toTokenAddress))
                queryParams["toTokenAddress"] = toTokenAddress;

            if (!string.IsNullOrEmpty(toChain))
                queryParams["toChain"] = toChain;

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}").ToArray());
            return $"{baseUrl}?{queryString}";
        }
    }
}