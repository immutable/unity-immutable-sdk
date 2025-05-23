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
    /// CollectionMetadata
    /// </summary>
    [DataContract(Name = "CollectionMetadata")]
    public partial class CollectionMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionMetadata" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected CollectionMetadata() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionMetadata" /> class.
        /// </summary>
        /// <param name="name">The name of the collection (required).</param>
        /// <param name="symbol">The symbol of contract (required).</param>
        /// <param name="description">The description of collection (required).</param>
        /// <param name="image">The url of the collection image (required).</param>
        /// <param name="externalLink">The url of external link (required).</param>
        /// <param name="contractUri">The uri for the metadata of the collection (required).</param>
        /// <param name="baseUri">The metadata uri for nft (required).</param>
        public CollectionMetadata(string name = default(string), string symbol = default(string), string description = default(string), string image = default(string), string externalLink = default(string), string contractUri = default(string), string baseUri = default(string))
        {
            // to ensure "name" is required (not null)
            if (name == null)
            {
                throw new ArgumentNullException("name is a required property for CollectionMetadata and cannot be null");
            }
            this.Name = name;
            // to ensure "symbol" is required (not null)
            if (symbol == null)
            {
                throw new ArgumentNullException("symbol is a required property for CollectionMetadata and cannot be null");
            }
            this.Symbol = symbol;
            // to ensure "description" is required (not null)
            if (description == null)
            {
                throw new ArgumentNullException("description is a required property for CollectionMetadata and cannot be null");
            }
            this.Description = description;
            // to ensure "image" is required (not null)
            if (image == null)
            {
                throw new ArgumentNullException("image is a required property for CollectionMetadata and cannot be null");
            }
            this.Image = image;
            // to ensure "externalLink" is required (not null)
            if (externalLink == null)
            {
                throw new ArgumentNullException("externalLink is a required property for CollectionMetadata and cannot be null");
            }
            this.ExternalLink = externalLink;
            // to ensure "contractUri" is required (not null)
            if (contractUri == null)
            {
                throw new ArgumentNullException("contractUri is a required property for CollectionMetadata and cannot be null");
            }
            this.ContractUri = contractUri;
            // to ensure "baseUri" is required (not null)
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri is a required property for CollectionMetadata and cannot be null");
            }
            this.BaseUri = baseUri;
        }

        /// <summary>
        /// The name of the collection
        /// </summary>
        /// <value>The name of the collection</value>
        /// <example>Gigantic Lizards</example>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// The symbol of contract
        /// </summary>
        /// <value>The symbol of contract</value>
        /// <example>GLZ</example>
        [DataMember(Name = "symbol", IsRequired = true, EmitDefaultValue = true)]
        public string Symbol { get; set; }

        /// <summary>
        /// The description of collection
        /// </summary>
        /// <value>The description of collection</value>
        /// <example>This is the Gigantic Lizards collection</example>
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
        [DataMember(Name = "contract_uri", IsRequired = true, EmitDefaultValue = true)]
        public string ContractUri { get; set; }

        /// <summary>
        /// The metadata uri for nft
        /// </summary>
        /// <value>The metadata uri for nft</value>
        /// <example>https://some-url</example>
        [DataMember(Name = "base_uri", IsRequired = true, EmitDefaultValue = true)]
        public string BaseUri { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class CollectionMetadata {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Symbol: ").Append(Symbol).Append("\n");
            sb.Append("  Description: ").Append(Description).Append("\n");
            sb.Append("  Image: ").Append(Image).Append("\n");
            sb.Append("  ExternalLink: ").Append(ExternalLink).Append("\n");
            sb.Append("  ContractUri: ").Append(ContractUri).Append("\n");
            sb.Append("  BaseUri: ").Append(BaseUri).Append("\n");
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
