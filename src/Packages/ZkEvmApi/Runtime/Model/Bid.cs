/*
 * Immutable zkEVM API
 *
 * Immutable Multi Rollup API
 *
 * The version of the OpenAPI document: 1.0.0
 * Contact: support@immutable.com
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OpenAPIDateConverter = Immutable.Api.ZkEvm.Client.OpenAPIDateConverter;

namespace Immutable.Api.ZkEvm.Model
{
    /// <summary>
    /// Bid
    /// </summary>
    [DataContract(Name = "Bid")]
    public partial class Bid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bid" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Bid() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Bid" /> class.
        /// </summary>
        /// <param name="bidId">Global Order identifier (required).</param>
        /// <param name="priceDetails">priceDetails (required).</param>
        /// <param name="tokenId">Token ID. Null for collection bids that can be fulfilled by any asset in the collection (required).</param>
        /// <param name="contractAddress">ETH Address of collection that the asset belongs to (required).</param>
        /// <param name="creator">ETH Address of listing creator (required).</param>
        /// <param name="amount">Amount of token included in the listing (required).</param>
        public Bid(string bidId = default(string), MarketPriceDetails priceDetails = default(MarketPriceDetails), string tokenId = default(string), string contractAddress = default(string), string creator = default(string), string amount = default(string))
        {
            // to ensure "bidId" is required (not null)
            if (bidId == null)
            {
                throw new ArgumentNullException("bidId is a required property for Bid and cannot be null");
            }
            this.BidId = bidId;
            // to ensure "priceDetails" is required (not null)
            if (priceDetails == null)
            {
                throw new ArgumentNullException("priceDetails is a required property for Bid and cannot be null");
            }
            this.PriceDetails = priceDetails;
            // to ensure "tokenId" is required (not null)
            if (tokenId == null)
            {
                throw new ArgumentNullException("tokenId is a required property for Bid and cannot be null");
            }
            this.TokenId = tokenId;
            // to ensure "contractAddress" is required (not null)
            if (contractAddress == null)
            {
                throw new ArgumentNullException("contractAddress is a required property for Bid and cannot be null");
            }
            this.ContractAddress = contractAddress;
            // to ensure "creator" is required (not null)
            if (creator == null)
            {
                throw new ArgumentNullException("creator is a required property for Bid and cannot be null");
            }
            this.Creator = creator;
            // to ensure "amount" is required (not null)
            if (amount == null)
            {
                throw new ArgumentNullException("amount is a required property for Bid and cannot be null");
            }
            this.Amount = amount;
        }

        /// <summary>
        /// Global Order identifier
        /// </summary>
        /// <value>Global Order identifier</value>
        /// <example>018792C9-4AD7-8EC4-4038-9E05C598534A</example>
        [DataMember(Name = "bid_id", IsRequired = true, EmitDefaultValue = true)]
        public string BidId { get; set; }

        /// <summary>
        /// Gets or Sets PriceDetails
        /// </summary>
        [DataMember(Name = "price_details", IsRequired = true, EmitDefaultValue = true)]
        public MarketPriceDetails PriceDetails { get; set; }

        /// <summary>
        /// Token ID. Null for collection bids that can be fulfilled by any asset in the collection
        /// </summary>
        /// <value>Token ID. Null for collection bids that can be fulfilled by any asset in the collection</value>
        /// <example>1</example>
        [DataMember(Name = "token_id", IsRequired = true, EmitDefaultValue = true)]
        public string TokenId { get; set; }

        /// <summary>
        /// ETH Address of collection that the asset belongs to
        /// </summary>
        /// <value>ETH Address of collection that the asset belongs to</value>
        /// <example>0xe9b00a87700f660e46b6f5deaa1232836bcc07d3</example>
        [DataMember(Name = "contract_address", IsRequired = true, EmitDefaultValue = true)]
        public string ContractAddress { get; set; }

        /// <summary>
        /// ETH Address of listing creator
        /// </summary>
        /// <value>ETH Address of listing creator</value>
        /// <example>0xe9b00a87700f660e46b6f5deaa1232836bcc07d3</example>
        [DataMember(Name = "creator", IsRequired = true, EmitDefaultValue = true)]
        public string Creator { get; set; }

        /// <summary>
        /// Amount of token included in the listing
        /// </summary>
        /// <value>Amount of token included in the listing</value>
        /// <example>1</example>
        [DataMember(Name = "amount", IsRequired = true, EmitDefaultValue = true)]
        public string Amount { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Bid {\n");
            sb.Append("  BidId: ").Append(BidId).Append("\n");
            sb.Append("  PriceDetails: ").Append(PriceDetails).Append("\n");
            sb.Append("  TokenId: ").Append(TokenId).Append("\n");
            sb.Append("  ContractAddress: ").Append(ContractAddress).Append("\n");
            sb.Append("  Creator: ").Append(Creator).Append("\n");
            sb.Append("  Amount: ").Append(Amount).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

    }

}