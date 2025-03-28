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
    /// MintAsset
    /// </summary>
    [DataContract(Name = "MintAsset")]
    public partial class MintAsset
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MintAsset" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected MintAsset() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MintAsset" /> class.
        /// </summary>
        /// <param name="referenceId">The id of this asset in the system that originates the mint request (required).</param>
        /// <param name="ownerAddress">The address of the receiver (required).</param>
        /// <param name="tokenId">An optional &#x60;uint256&#x60; token id as string. Required for ERC1155 collections..</param>
        /// <param name="amount">Optional mount of tokens to mint. Required for ERC1155 collections. ERC712 collections can omit this field or set it to 1.</param>
        /// <param name="metadata">metadata.</param>
        public MintAsset(string referenceId = default(string), string ownerAddress = default(string), string tokenId = default(string), string amount = default(string), NFTMetadataRequest metadata = default(NFTMetadataRequest))
        {
            // to ensure "referenceId" is required (not null)
            if (referenceId == null)
            {
                throw new ArgumentNullException("referenceId is a required property for MintAsset and cannot be null");
            }
            this.ReferenceId = referenceId;
            // to ensure "ownerAddress" is required (not null)
            if (ownerAddress == null)
            {
                throw new ArgumentNullException("ownerAddress is a required property for MintAsset and cannot be null");
            }
            this.OwnerAddress = ownerAddress;
            this.TokenId = tokenId;
            this.Amount = amount;
            this.Metadata = metadata;
        }

        /// <summary>
        /// The id of this asset in the system that originates the mint request
        /// </summary>
        /// <value>The id of this asset in the system that originates the mint request</value>
        /// <example>67f7d464-b8f0-4f6a-9a3b-8d3cb4a21af0</example>
        [DataMember(Name = "reference_id", IsRequired = true, EmitDefaultValue = true)]
        public string ReferenceId { get; set; }

        /// <summary>
        /// The address of the receiver
        /// </summary>
        /// <value>The address of the receiver</value>
        /// <example>0xc344c05eef8876e517072f879dae8905aa2b956b</example>
        [DataMember(Name = "owner_address", IsRequired = true, EmitDefaultValue = true)]
        public string OwnerAddress { get; set; }

        /// <summary>
        /// An optional &#x60;uint256&#x60; token id as string. Required for ERC1155 collections.
        /// </summary>
        /// <value>An optional &#x60;uint256&#x60; token id as string. Required for ERC1155 collections.</value>
        /// <example>1</example>
        [DataMember(Name = "token_id", EmitDefaultValue = true)]
        public string TokenId { get; set; }

        /// <summary>
        /// Optional mount of tokens to mint. Required for ERC1155 collections. ERC712 collections can omit this field or set it to 1
        /// </summary>
        /// <value>Optional mount of tokens to mint. Required for ERC1155 collections. ERC712 collections can omit this field or set it to 1</value>
        /// <example>1</example>
        [DataMember(Name = "amount", EmitDefaultValue = true)]
        public string Amount { get; set; }

        /// <summary>
        /// Gets or Sets Metadata
        /// </summary>
        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public NFTMetadataRequest Metadata { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class MintAsset {\n");
            sb.Append("  ReferenceId: ").Append(ReferenceId).Append("\n");
            sb.Append("  OwnerAddress: ").Append(OwnerAddress).Append("\n");
            sb.Append("  TokenId: ").Append(TokenId).Append("\n");
            sb.Append("  Amount: ").Append(Amount).Append("\n");
            sb.Append("  Metadata: ").Append(Metadata).Append("\n");
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
