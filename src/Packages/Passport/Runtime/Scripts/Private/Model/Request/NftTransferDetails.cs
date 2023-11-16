using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class NftTransferDetails
    {
        /**
        * Ethereum address of the receiving user
        */
        public string receiver;
        /**
        * The token ID
        */
        public string tokenId;

        /**
        * The token contract address
        */
        public string tokenAddress;

        public NftTransferDetails(
            string receiver,
            string tokenId,
            string tokenAddress
            )
        {
            this.receiver = receiver;
            this.tokenId = tokenId;
            this.tokenAddress = tokenAddress;
        }
    }
}