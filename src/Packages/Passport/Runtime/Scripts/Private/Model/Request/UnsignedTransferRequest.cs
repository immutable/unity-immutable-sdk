using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class UnsignedTransferRequest
    {
        /**
        * Ethereum address of the receiving user
        */
        public string Receiver { get; }

        /**
        * The type of the token being transferred, either ETH, ERC20 or ERC721
        */
        public string Type { get; }

        /**
        * The amount of tokens being transferred
        */
        public string Amount { get; }

        /**
        * The token ID
        */
        public string? TokenId { get; }

        /**
        * The token address
        */
        public string? TokenAddress { get; }

        private UnsignedTransferRequest(
            string type,
            int amount,
            string receiver,
            string? tokenId = null,
            string? tokenAddress = null
            )
        {
            this.Type = type;
            this.Amount = $"{amount}";
            this.TokenId = tokenId;
            this.TokenAddress = tokenAddress;
            this.Receiver = receiver;
        }

        public static UnsignedTransferRequest ETH(string receiver, int amount)
        {
            return new UnsignedTransferRequest("ETH", amount, receiver);
        }

        public static UnsignedTransferRequest ERC20(string receiver, int amount, string tokenAddress)
        {
            return new UnsignedTransferRequest("ERC20", amount, receiver, null, tokenAddress);
        }

        public static UnsignedTransferRequest ERC721(string receiver, string tokenId, string tokenAddress)
        {
            return new UnsignedTransferRequest("ERC721", 1, receiver, tokenId, tokenAddress);
        }
    }
}