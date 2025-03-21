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
    /// ERC721Item
    /// </summary>
    [DataContract(Name = "ERC721Item")]
    public partial class ERC721Item
    {
        /// <summary>
        /// Token type user is offering, which in this case is ERC721
        /// </summary>
        /// <value>Token type user is offering, which in this case is ERC721</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            /// <summary>
            /// Enum ERC721 for value: ERC721
            /// </summary>
            [EnumMember(Value = "ERC721")]
            ERC721 = 1
        }


        /// <summary>
        /// Token type user is offering, which in this case is ERC721
        /// </summary>
        /// <value>Token type user is offering, which in this case is ERC721</value>
        /// <example>ERC721</example>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ERC721Item" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ERC721Item() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ERC721Item" /> class.
        /// </summary>
        /// <param name="type">Token type user is offering, which in this case is ERC721 (required).</param>
        /// <param name="contractAddress">Address of ERC721 token (required).</param>
        /// <param name="tokenId">ID of ERC721 token (required).</param>
        public ERC721Item(TypeEnum type = default(TypeEnum), string contractAddress = default(string), string tokenId = default(string))
        {
            this.Type = type;
            // to ensure "contractAddress" is required (not null)
            if (contractAddress == null)
            {
                throw new ArgumentNullException("contractAddress is a required property for ERC721Item and cannot be null");
            }
            this.ContractAddress = contractAddress;
            // to ensure "tokenId" is required (not null)
            if (tokenId == null)
            {
                throw new ArgumentNullException("tokenId is a required property for ERC721Item and cannot be null");
            }
            this.TokenId = tokenId;
        }

        /// <summary>
        /// Address of ERC721 token
        /// </summary>
        /// <value>Address of ERC721 token</value>
        /// <example>0x692edAd005237c7E737bB2c0F3D8ccCc10D3479E</example>
        [DataMember(Name = "contract_address", IsRequired = true, EmitDefaultValue = true)]
        public string ContractAddress { get; set; }

        /// <summary>
        /// ID of ERC721 token
        /// </summary>
        /// <value>ID of ERC721 token</value>
        /// <example>1</example>
        [DataMember(Name = "token_id", IsRequired = true, EmitDefaultValue = true)]
        public string TokenId { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class ERC721Item {\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  ContractAddress: ").Append(ContractAddress).Append("\n");
            sb.Append("  TokenId: ").Append(TokenId).Append("\n");
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
