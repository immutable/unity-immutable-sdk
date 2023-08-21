using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class UnsignedTransferRequest
    {
        public string Receiver { get; }

        public string Type { get; }

        public string Amount { get; }

        public string? TokenId { get; }

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