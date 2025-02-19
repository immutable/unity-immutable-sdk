/*
 * TS SDK API
 *
 * running ts sdk as an api
 *
 * The version of the OpenAPI document: 1.0.0
 * Contact: contact@immutable.com
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Immutable.Orderbook.Model
{
    /// <summary>
    ///     ERC721Item
    /// </summary>
    [DataContract(Name = "ERC721Item")]
    public class ERC721Item
    {
        /// <summary>
        ///     Defines Type
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            /// <summary>
            ///     Enum ERC721 for value: ERC721
            /// </summary>
            [EnumMember(Value = "ERC721")] ERC721
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ERC721Item" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ERC721Item()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ERC721Item" /> class.
        /// </summary>
        /// <param name="contractAddress">contractAddress (required).</param>
        /// <param name="tokenId">tokenId (required).</param>
        /// <param name="type">type (required).</param>
        public ERC721Item(string contractAddress = default, string tokenId = default, TypeEnum type = default)
        {
            // to ensure "contractAddress" is required (not null)
            if (contractAddress == null)
                throw new ArgumentNullException(
                    "contractAddress is a required property for ERC721Item and cannot be null");
            ContractAddress = contractAddress;
            // to ensure "tokenId" is required (not null)
            if (tokenId == null)
                throw new ArgumentNullException("tokenId is a required property for ERC721Item and cannot be null");
            TokenId = tokenId;
            Type = type;
        }


        /// <summary>
        ///     Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public TypeEnum Type { get; set; }

        /// <summary>
        ///     Gets or Sets ContractAddress
        /// </summary>
        [DataMember(Name = "contractAddress", IsRequired = true, EmitDefaultValue = true)]
        public string ContractAddress { get; set; }

        /// <summary>
        ///     Gets or Sets TokenId
        /// </summary>
        [DataMember(Name = "tokenId", IsRequired = true, EmitDefaultValue = true)]
        public string TokenId { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class ERC721Item {\n");
            sb.Append("  ContractAddress: ").Append(ContractAddress).Append("\n");
            sb.Append("  TokenId: ").Append(TokenId).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        ///     Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}