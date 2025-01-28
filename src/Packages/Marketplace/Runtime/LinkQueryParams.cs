namespace Immutable.Marketplace
{
    /// <summary>
    /// Represents the query parameters for generating an on-ramp URL.
    /// </summary>
    public struct OnRampQueryParams
    {
        /// <summary>
        /// The cryptocurrency to purchase (default: "IMX").
        /// </summary>
        public string DefaultCryptoCurrency { get; set; }

        /// <summary>
        /// The amount of fiat currency to spend when purchasing cryptocurrency (default: "50").
        /// </summary>
        public string DefaultFiatAmount { get; set; }

        /// <summary>
        /// The fiat currency to use (default: "USD").
        /// </summary>
        public string DefaultFiatCurrency { get; set; }

        /// <summary>
        /// A comma-separated list of available cryptocurrencies for purchase (default: "imx,eth,usdc").
        /// </summary>
        public string CryptoCurrencyList { get; set; }
    }

    /// <summary>
    /// Represents the query parameters for generating a swap URL.
    /// </summary>
    public struct SwapQueryParams
    {
        /// <summary>
        /// The address of the token being swapped from (default is null).
        /// </summary>
        public string? FromTokenAddress { get; set; }

        /// <summary>
        /// The address of the token being swapped to (default is null).
        /// </summary>
        public string? ToTokenAddress { get; set; }
    }

    /// <summary>
    /// Represents the query parameters for generating a bridge URL.
    /// </summary>
    public struct BridgeQueryParams
    {
        /// <summary>
        /// The address of the token being moved from (default is null).
        /// </summary>
        public string? FromTokenAddress { get; set; }

        /// <summary>
        /// The ID of the source blockchain (default is null).
        /// </summary>
        public string? FromChainID { get; set; }

        /// <summary>
        /// The address of the token being moved to (default is null).
        /// </summary>
        public string? ToTokenAddress { get; set; }

        /// <summary>
        /// The ID of the destination blockchain (default is null).
        /// </summary>
        public string? ToChainID { get; set; }
    }
}
