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
    /// Collection
    /// </summary>
    [DataContract(Name = "Collection")]
    public partial class Collection
    {

        /// <summary>
        /// Gets or Sets ContractType
        /// </summary>
        [DataMember(Name = "contract_type", IsRequired = true, EmitDefaultValue = true)]
        public CollectionContractType ContractType { get; set; }

        /// <summary>
        /// Gets or Sets VerificationStatus
        /// </summary>
        [DataMember(Name = "verification_status", IsRequired = true, EmitDefaultValue = true)]
        public AssetVerificationStatus VerificationStatus { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Collection() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class.
        /// </summary>
        /// <param name="chain">chain (required).</param>
        /// <param name="name">The name of the collection (required).</param>
        /// <param name="symbol">The symbol of contract (required).</param>
        /// <param name="contractType">contractType (required).</param>
        /// <param name="contractAddress">The address of the contract (required).</param>
        /// <param name="description">The description of collection (required).</param>
        /// <param name="image">The url of the collection image (required).</param>
        /// <param name="externalLink">The url of external link (required).</param>
        /// <param name="contractUri">The uri for the metadata of the collection.</param>
        /// <param name="baseUri">The metadata uri for nft (required).</param>
        /// <param name="verificationStatus">verificationStatus (required).</param>
        /// <param name="indexedAt">When the collection was first indexed (required).</param>
        /// <param name="updatedAt">When the collection was last updated (required).</param>
        /// <param name="metadataSyncedAt">When the collection metadata was last synced (required).</param>
        public Collection(Chain chain = default(Chain), string name = default(string), string symbol = default(string), CollectionContractType contractType = default(CollectionContractType), string contractAddress = default(string), string description = default(string), string image = default(string), string externalLink = default(string), string contractUri = default(string), string baseUri = default(string), AssetVerificationStatus verificationStatus = default(AssetVerificationStatus), DateTime indexedAt = default(DateTime), DateTime updatedAt = default(DateTime), DateTime? metadataSyncedAt = default(DateTime?))
        {
            // to ensure "chain" is required (not null)
            if (chain == null)
            {
                throw new ArgumentNullException("chain is a required property for Collection and cannot be null");
            }
            this.Chain = chain;
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for Collection and cannot be null");
            }
            this.Name = name;
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol is a required property for Collection and cannot be null");
            }
            this.Symbol = symbol;
            this.ContractType = contractType;
            // to ensure "contractAddress" is required (not null)
            if (contractAddress == null)
            {
                throw new ArgumentNullException("contractAddress is a required property for Collection and cannot be null");
            }
            this.ContractAddress = contractAddress;
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new ArgumentNullException("description is a required property for Collection and cannot be null");
            }
            this.Description = description;
            // to ensure "image" is required (not null)
            if (image == null)
            {
                throw new ArgumentNullException("image is a required property for Collection and cannot be null");
            }
            this.Image = image;
            // to ensure "externalLink" is required (not null)
            if (externalLink == null)
            {
                throw new ArgumentNullException("externalLink is a required property for Collection and cannot be null");
            }
            this.ExternalLink = externalLink;
            // to ensure "baseUri" is required (not null)
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri is a required property for Collection and cannot be null");
            }
            this.BaseUri = baseUri;
            this.VerificationStatus = verificationStatus;
            this.IndexedAt = indexedAt;
            this.UpdatedAt = updatedAt;
            // to ensure "metadataSyncedAt" is required (not null)
            if (metadataSyncedAt == null)
            {
                throw new ArgumentNullException("metadataSyncedAt is a required property for Collection and cannot be null");
            }
            this.MetadataSyncedAt = metadataSyncedAt;
            this.ContractUri = contractUri;
        }

        /// <summary>
        /// Gets or Sets Chain
        /// </summary>
        [DataMember(Name = "chain", IsRequired = true, EmitDefaultValue = true)]
        public Chain Chain { get; set; }

        /// <summary>
        /// The name of the collection
        /// </summary>
        /// <value>The name of the collection</value>
        /// <example>0x8a90cab2b38dba80c64b7734e58ee1db38b8992e</example>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// The symbol of contract
        /// </summary>
        /// <value>The symbol of contract</value>
        /// <example>BASP</example>
        [DataMember(Name = "symbol", IsRequired = true, EmitDefaultValue = true)]
        public string Symbol { get; set; }

        /// <summary>
        /// The address of the contract
        /// </summary>
        /// <value>The address of the contract</value>
        /// <example>0x8a90cab2b38dba80c64b7734e58ee1db38b8992e</example>
        [DataMember(Name = "contract_address", IsRequired = true, EmitDefaultValue = true)]
        public string ContractAddress { get; set; }

        /// <summary>
        /// The description of collection
        /// </summary>
        /// <value>The description of collection</value>
        /// <example>Some description</example>
        [DataMember(Name = "description", IsRequired = true, EmitDefaultValue = true)]
        public string Description { get; set; }

        /// <summary>
        /// The url of the collection image
        /// </summary>
        /// <value>The url of the collection image</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "image", IsRequired = true, EmitDefaultValue = true)]
        public string Image { get; set; }

        /// <summary>
        /// The url of external link
        /// </summary>
        /// <value>The url of external link</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "external_link", IsRequired = true, EmitDefaultValue = true)]
        public string ExternalLink { get; set; }

        /// <summary>
        /// The uri for the metadata of the collection
        /// </summary>
        /// <value>The uri for the metadata of the collection</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "contract_uri", EmitDefaultValue = true)]
        public string ContractUri { get; set; }

        /// <summary>
        /// The metadata uri for nft
        /// </summary>
        /// <value>The metadata uri for nft</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "base_uri", IsRequired = true, EmitDefaultValue = true)]
        public string BaseUri { get; set; }

        /// <summary>
        /// When the collection was first indexed
        /// </summary>
        /// <value>When the collection was first indexed</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "indexed_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime IndexedAt { get; set; }

        /// <summary>
        /// When the collection was last updated
        /// </summary>
        /// <value>When the collection was last updated</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "updated_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// When the collection metadata was last synced
        /// </summary>
        /// <value>When the collection metadata was last synced</value>
        /// <example>2022-08-16T17:43:26.991388Z</example>
        [DataMember(Name = "metadata_synced_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime? MetadataSyncedAt { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Collection {\n");
            sb.Append("  Chain: ").Append(Chain).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  ContractType: ").Append(ContractType).Append("\n");
            sb.Append("  ContractAddress: ").Append(ContractAddress).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  Image: ").Append(Image).Append("\n");
            sb.Append("  ExternalLink: ").Append(ExternalLink).Append("\n");
            sb.Append("  ContractUri: ").Append(ContractUri).Append("\n");
            sb.Append("  BaseUri: ").Append(BaseUri).Append("\n");
            sb.Append("  VerificationStatus: ").Append(VerificationStatus).Append("\n");
            sb.Append("  IndexedAt: ").Append(IndexedAt).Append("\n");
            sb.Append("  UpdatedAt: ").Append(UpdatedAt).Append("\n");
            sb.Append("  MetadataSyncedAt: ").Append(MetadataSyncedAt).Append("\n");
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
