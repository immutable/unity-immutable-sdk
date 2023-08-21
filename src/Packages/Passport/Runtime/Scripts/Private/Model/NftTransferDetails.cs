using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class NftTransferDetails
    {
        /**
        * Ethereum address of the receiving user
        */
        public string Receiver { get; }

        /**
        * The token ID
        */
        public string TokenId { get; }

        /**
        * The token contract address
        */
        public string TokenAddress { get; }

        public NftTransferDetails(
            string receiver,
            string tokenId,
            string tokenAddress
            )
        {
            this.Receiver = receiver;
            this.TokenId = tokenId;
            this.TokenAddress = tokenAddress;
        }
    }
}