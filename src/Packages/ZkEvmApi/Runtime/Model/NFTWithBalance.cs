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
    /// NFTWithBalance
    /// </summary>
    [DataContract(Name = "NFTWithBalance")]
    public partial class NFTWithBalance
    {

        /// <summary>
        /// Gets or Sets ContractType
        /// </summary>
        [DataMember(Name = "contract_type", IsRequired = true, EmitDefaultValue = true)]
        public NFTContractType ContractType { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="NFTWithBalance" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected NFTWithBalance() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="NFTWithBalance" /> class.
        /// </summary>
        /// <param name="chain">chain (required).</param>
        /// <param name="tokenId">An &#x60;uint256&#x60; token id as string (required).</param>
        /// <param name="contractAddress">The contract address of the NFT (required).</param>
        /// <param name="contractType">contractType (required).</param>
        /// <param name="indexedAt">When the NFT was first indexed (required).</param>
        /// <param name="updatedAt">When the NFT owner was last updated (required).</param>
        /// <param name="metadataSyncedAt">When NFT metadata was last synced (required).</param>
        /// <param name="metadataId">The id of the metadata of this NFT.</param>
        /// <param name="name">The name of the NFT (required).</param>
        /// <param name="description">The description of the NFT (required).</param>
        /// <param name="image">The image url of the NFT (required).</param>
        /// <param name="externalLink">The external website link of NFT (required).</param>
        /// <param name="animationUrl">The animation url of the NFT (required).</param>
        /// <param name="youtubeUrl">The youtube URL of NFT (required).</param>
        /// <param name="attributes">List of Metadata attributes (required).</param>
        /// <param name="balance">The amount of this NFT this account owns (required).</param>
        public NFTWithBalance(Chain chain = default(Chain), string tokenId = default(string), string contractAddress = default(string), NFTContractType contractType = default(NFTContractType), DateTime indexedAt = default(DateTime), DateTime updatedAt = default(DateTime), DateTime? metadataSyncedAt = default(DateTime?), Guid? metadataId = default(Guid?), string name = default(string), string description = default(string), string image = default(string), string externalLink = default(string), string animationUrl = default(string), string youtubeUrl = default(string), List<NFTMetadataAttribute> attributes = default(List<NFTMetadataAttribute>), string balance = default(string))
        {
            // to ensure "chain" is required (not null)
            if (chain == null)
            {
                throw new ArgumentNullException("chain is a required property for NFTWithBalance and cannot be null");
            }
            this.Chain = chain;
            // to ensure "tokenId" is required (not null)
            if (tokenId == null)
            {
                throw new ArgumentNullException("tokenId is a required property for NFTWithBalance and cannot be null");
            }
            this.TokenId = tokenId;
            // to ensure "contractAddress" is required (not null)
            if (contractAddress == null)
            {
                throw new ArgumentNullException("contractAddress is a required property for NFTWithBalance and cannot be null");
            }
            this.ContractAddress = contractAddress;
            this.ContractType = contractType;
            this.IndexedAt = indexedAt;
            this.UpdatedAt = updatedAt;
            // to ensure "metadataSyncedAt" is required (not null)
            if (metadataSyncedAt == null)
            {
                throw new ArgumentNullException("metadataSyncedAt is a required property for NFTWithBalance and cannot be null");
            }
            this.MetadataSyncedAt = metadataSyncedAt;
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for NFTWithBalance and cannot be null");
            }
            this.Name = name;
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new ArgumentNullException("description is a required property for NFTWithBalance and cannot be null");
            }
            this.Description = description;
            // to ensure "image" is required (not null)
            if (image == null)
            {
                throw new ArgumentNullException("image is a required property for NFTWithBalance and cannot be null");
            }
            this.Image = image;
            // to ensure "externalLink" is required (not null)
            if (externalLink == null)
            {
                throw new ArgumentNullException("externalLink is a required property for NFTWithBalance and cannot be null");
            }
            this.ExternalLink = externalLink;
            // to ensure "animationUrl" is required (not null)
            if (animationUrl == null)
            {
                throw new ArgumentNullException("animationUrl is a required property for NFTWithBalance and cannot be null");
            }
            this.AnimationUrl = animationUrl;
            // to ensure "youtubeUrl" is required (not null)
            if (youtubeUrl == null)
            {
                throw new ArgumentNullException("youtubeUrl is a required property for NFTWithBalance and cannot be null");
            }
            this.YoutubeUrl = youtubeUrl;
            // to ensure "attributes" is required (not null)
            if (attributes == null)
            {
                throw new ArgumentNullException("attributes is a required property for NFTWithBalance and cannot be null");
            }
            this.Attributes = attributes;
            // to ensure "balance" is required (not null)
            if (balance == null)
            {
                throw new ArgumentNullException("balance is a required property for NFTWithBalance and cannot be null");
            }
            this.Balance = balance;
            this.MetadataId = metadataId;
        }

        /// <summary>
        /// Gets or Sets Chain
        /// </summary>
        [DataMember(Name = "chain", IsRequired = true, EmitDefaultValue = true)]
        public Chain Chain { get; set; }

        /// <summary>
        /// An &#x60;uint256&#x60; token id as string
        /// </summary>
        /// <value>An &#x60;uint256&#x60; token id as string</value>
        /// <example>1</example>
        [DataMember(Name = "token_id", IsRequired = true, EmitDefaultValue = true)]
        public string TokenId { get; set; }

        /// <summary>
        /// The contract address of the NFT
        /// </summary>
        /// <value>The contract address of the NFT</value>
        /// <example>0x8a90cab2b38dba80c64b7734e58ee1db38b8992e</example>
        [DataMember(Name = "contract_address", IsRequired = true, EmitDefaultValue = true)]
        public string ContractAddress { get; set; }

        /// <summary>
        /// When the NFT was first indexed
        /// </summary>
        /// <value>When the NFT was first indexed</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "indexed_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime IndexedAt { get; set; }

        /// <summary>
        /// When the NFT owner was last updated
        /// </summary>
        /// <value>When the NFT owner was last updated</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "updated_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// When NFT metadata was last synced
        /// </summary>
        /// <value>When NFT metadata was last synced</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "metadata_synced_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime? MetadataSyncedAt { get; set; }

        /// <summary>
        /// The id of the metadata of this NFT
        /// </summary>
        /// <value>The id of the metadata of this NFT</value>
        /// <example>ae83bc80-4dd5-11ee-be56-0242ac120002</example>
        [DataMember(Name = "metadata_id", EmitDefaultValue = true)]
        public Guid? MetadataId { get; set; }

        /// <summary>
        /// The name of the NFT
        /// </summary>
        /// <value>The name of the NFT</value>
        /// <example>Sword</example>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// The description of the NFT
        /// </summary>
        /// <value>The description of the NFT</value>
        /// <example>This is a super awesome sword</example>
        [DataMember(Name = "description", IsRequired = true, EmitDefaultValue = true)]
        public string Description { get; set; }

        /// <summary>
        /// The image url of the NFT
        /// </summary>
        /// <value>The image url of the NFT</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "image", IsRequired = true, EmitDefaultValue = true)]
        public string Image { get; set; }

        /// <summary>
        /// The external website link of NFT
        /// </summary>
        /// <value>The external website link of NFT</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "external_link", IsRequired = true, EmitDefaultValue = true)]
        public string ExternalLink { get; set; }

        /// <summary>
        /// The animation url of the NFT
        /// </summary>
        /// <value>The animation url of the NFT</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "animation_url", IsRequired = true, EmitDefaultValue = true)]
        public string AnimationUrl { get; set; }

        /// <summary>
        /// The youtube URL of NFT
        /// </summary>
        /// <value>The youtube URL of NFT</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "youtube_url", IsRequired = true, EmitDefaultValue = true)]
        public string YoutubeUrl { get; set; }

        /// <summary>
        /// List of Metadata attributes
        /// </summary>
        /// <value>List of Metadata attributes</value>
        [DataMember(Name = "attributes", IsRequired = true, EmitDefaultValue = true)]
        public List<NFTMetadataAttribute> Attributes { get; set; }

        /// <summary>
        /// The amount of this NFT this account owns
        /// </summary>
        /// <value>The amount of this NFT this account owns</value>
        /// <example>11</example>
        [DataMember(Name = "balance", IsRequired = true, EmitDefaultValue = true)]
        public string Balance { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class NFTWithBalance {\n");
            sb.Append("  Chain: ").Append(Chain).Append("\n");
            sb.Append("  TokenId: ").Append(TokenId).Append("\n");
            sb.Append("  ContractAddress: ").Append(ContractAddress).Append("\n");
            sb.Append("  ContractType: ").Append(ContractType).Append("\n");
            sb.Append("  IndexedAt: ").Append(IndexedAt).Append("\n");
            sb.Append("  UpdatedAt: ").Append(UpdatedAt).Append("\n");
            sb.Append("  MetadataSyncedAt: ").Append(MetadataSyncedAt).Append("\n");
            sb.Append("  MetadataId: ").Append(MetadataId).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  Image: ").Append(Image).Append("\n");
            sb.Append("  ExternalLink: ").Append(ExternalLink).Append("\n");
            sb.Append("  AnimationUrl: ").Append(AnimationUrl).Append("\n");
            sb.Append("  YoutubeUrl: ").Append(YoutubeUrl).Append("\n");
            sb.Append("  Attributes: ").Append(Attributes).Append("\n");
            sb.Append("  Balance: ").Append(Balance).Append("\n");
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
