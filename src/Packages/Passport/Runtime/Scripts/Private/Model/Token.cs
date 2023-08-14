using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Token
    {
        public string Type { get; }

        public string Quantity { get; }

        public string? TokenId { get; }

        public string? TokenAddress { get; }

        private Token(string type, int quantity, string? tokenId = null, string? tokenAddress = null)
        {
            this.Type = type;
            this.Quantity = $"{quantity}";
            this.TokenId = tokenId;
            this.TokenAddress = tokenAddress;
        }

        public static Token ETH(int quantity)
        {
            return new Token("ETH", quantity);
        }

        public static Token ERC20(int quantity, string tokenAddress)
        {
            return new Token("ERC20", quantity, null, tokenAddress);
        }

        public static Token ERC721(string tokenId, string tokenAddress)
        {
            return new Token("ERC721", 1, tokenId, tokenAddress);
        }
    }
}