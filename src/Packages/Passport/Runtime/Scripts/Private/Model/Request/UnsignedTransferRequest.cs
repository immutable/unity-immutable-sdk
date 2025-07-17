using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class UnsignedTransferRequest
    {
        /**
        * Ethereum address of the receiving user
        */
        public string receiver;

        /**
        * The type of the token being transferred, either ETH, ERC20 or ERC721
        */
        public string type;

        /**
        * The amount of tokens being transferred. For ETH the amount is in unit Wei.
        */
        public string amount;

        /**
        * The token ID
        */
        public string? tokenId;

        /**
        * The token address
        */
        public string? tokenAddress;

        public UnsignedTransferRequest(
            string type,
            string amount,
            string receiver,
            string? tokenId = null,
            string? tokenAddress = null
            )
        {
            this.type = type;
            this.amount = amount;
            this.tokenId = tokenId;
            this.tokenAddress = tokenAddress;
            this.receiver = receiver;
        }


        /**
        * Receiver's ETH address and amount in unit Wei
        */
        public static UnsignedTransferRequest ETH(string receiver, string amount)
        {
            return new UnsignedTransferRequest("ETH", amount, receiver);
        }

        public static UnsignedTransferRequest ERC20(string receiver, int amount, string tokenAddress)
        {
            return new UnsignedTransferRequest("ERC20", $"{amount}", receiver, null, tokenAddress);
        }

        public static UnsignedTransferRequest ERC721(string receiver, string tokenId, string tokenAddress)
        {
            return new UnsignedTransferRequest("ERC721", "1", receiver, tokenId, tokenAddress);
        }
    }
}