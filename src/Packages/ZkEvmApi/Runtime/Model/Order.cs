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
    /// Order
    /// </summary>
    [DataContract(Name = "Order")]
    public partial class Order
    {
        /// <summary>
        /// Order type
        /// </summary>
        /// <value>Order type</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TypeEnum
        {
            /// <summary>
            /// Enum LISTING for value: LISTING
            /// </summary>
            [EnumMember(Value = "LISTING")]
            LISTING = 1,

            /// <summary>
            /// Enum BID for value: BID
            /// </summary>
            [EnumMember(Value = "BID")]
            BID = 2,

            /// <summary>
            /// Enum COLLECTIONBID for value: COLLECTION_BID
            /// </summary>
            [EnumMember(Value = "COLLECTION_BID")]
            COLLECTIONBID = 3
        }


        /// <summary>
        /// Order type
        /// </summary>
        /// <value>Order type</value>
        /// <example>LISTING</example>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public TypeEnum Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Order" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Order() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Order" /> class.
        /// </summary>
        /// <param name="accountAddress">accountAddress (required).</param>
        /// <param name="buy">buy (required).</param>
        /// <param name="fees">fees (required).</param>
        /// <param name="chain">chain (required).</param>
        /// <param name="createdAt">Time the Order is created (required).</param>
        /// <param name="endAt">Time after which the Order is considered expired (required).</param>
        /// <param name="id">Global Order identifier (required).</param>
        /// <param name="orderHash">orderHash (required).</param>
        /// <param name="protocolData">protocolData (required).</param>
        /// <param name="salt">A random value added to the create Order request (required).</param>
        /// <param name="sell">sell (required).</param>
        /// <param name="signature">Digital signature generated by the user for the specific Order (required).</param>
        /// <param name="startAt">Time after which Order is considered active (required).</param>
        /// <param name="status">status (required).</param>
        /// <param name="type">Order type (required).</param>
        /// <param name="updatedAt">Time the Order is last updated (required).</param>
        /// <param name="fillStatus">fillStatus (required).</param>
        public Order(string accountAddress = default(string), List<Item> buy = default(List<Item>), List<Fee> fees = default(List<Fee>), Chain chain = default(Chain), DateTime createdAt = default(DateTime), DateTime endAt = default(DateTime), string id = default(string), string orderHash = default(string), ProtocolData protocolData = default(ProtocolData), string salt = default(string), List<Item> sell = default(List<Item>), string signature = default(string), DateTime startAt = default(DateTime), OrderStatus status = default(OrderStatus), TypeEnum type = default(TypeEnum), DateTime updatedAt = default(DateTime), FillStatus fillStatus = default(FillStatus))
        {
            // to ensure "accountAddress" is required (not null)
            if (accountAddress == null)
            {
                throw new ArgumentNullException("accountAddress is a required property for Order and cannot be null");
            }
            this.AccountAddress = accountAddress;
            // to ensure "buy" is required (not null)
            if (buy == null)
            {
                throw new ArgumentNullException("buy is a required property for Order and cannot be null");
            }
            this.Buy = buy;
            // to ensure "fees" is required (not null)
            if (fees == null)
            {
                throw new ArgumentNullException("fees is a required property for Order and cannot be null");
            }
            this.Fees = fees;
            // to ensure "chain" is required (not null)
            if (chain == null)
            {
                throw new ArgumentNullException("chain is a required property for Order and cannot be null");
            }
            this.Chain = chain;
            this.CreatedAt = createdAt;
            this.EndAt = endAt;
            // to ensure "id" is required (not null)
            if (id == null)
            {
                throw new ArgumentNullException("id is a required property for Order and cannot be null");
            }
            this.Id = id;
            // to ensure "orderHash" is required (not null)
            if (orderHash == null)
            {
                throw new ArgumentNullException("orderHash is a required property for Order and cannot be null");
            }
            this.OrderHash = orderHash;
            // to ensure "protocolData" is required (not null)
            if (protocolData == null)
            {
                throw new ArgumentNullException("protocolData is a required property for Order and cannot be null");
            }
            this.ProtocolData = protocolData;
            // to ensure "salt" is required (not null)
            if (salt == null)
            {
                throw new ArgumentNullException("salt is a required property for Order and cannot be null");
            }
            this.Salt = salt;
            // to ensure "sell" is required (not null)
            if (sell == null)
            {
                throw new ArgumentNullException("sell is a required property for Order and cannot be null");
            }
            this.Sell = sell;
            // to ensure "signature" is required (not null)
            if (signature == null)
            {
                throw new ArgumentNullException("signature is a required property for Order and cannot be null");
            }
            this.Signature = signature;
            this.StartAt = startAt;
            // to ensure "status" is required (not null)
            if (status == null)
            {
                throw new ArgumentNullException("status is a required property for Order and cannot be null");
            }
            this.Status = status;
            this.Type = type;
            this.UpdatedAt = updatedAt;
            // to ensure "fillStatus" is required (not null)
            if (fillStatus == null)
            {
                throw new ArgumentNullException("fillStatus is a required property for Order and cannot be null");
            }
            this.FillStatus = fillStatus;
        }

        /// <summary>
        /// Gets or Sets AccountAddress
        /// </summary>
        /// <example>0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266</example>
        [DataMember(Name = "account_address", IsRequired = true, EmitDefaultValue = true)]
        public string AccountAddress { get; set; }

        /// <summary>
        /// Gets or Sets Buy
        /// </summary>
        /// <example>[{&quot;type&quot;:&quot;NATIVE&quot;,&quot;amount&quot;:&quot;9750000000000000000&quot;,&quot;contract_address&quot;:&quot;0x0165878A594ca255338adfa4d48449f69242Eb8F&quot;}]</example>
        [DataMember(Name = "buy", IsRequired = true, EmitDefaultValue = true)]
        public List<Item> Buy { get; set; }

        /// <summary>
        /// Gets or Sets Fees
        /// </summary>
        /// <example>[]</example>
        [DataMember(Name = "fees", IsRequired = true, EmitDefaultValue = true)]
        public List<Fee> Fees { get; set; }

        /// <summary>
        /// Gets or Sets Chain
        /// </summary>
        [DataMember(Name = "chain", IsRequired = true, EmitDefaultValue = true)]
        public Chain Chain { get; set; }

        /// <summary>
        /// Time the Order is created
        /// </summary>
        /// <value>Time the Order is created</value>
        /// <example>2022-03-07T07:20:50.520Z</example>
        [DataMember(Name = "created_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Time after which the Order is considered expired
        /// </summary>
        /// <value>Time after which the Order is considered expired</value>
        /// <example>2022-03-10T05:00:50.520Z</example>
        [DataMember(Name = "end_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime EndAt { get; set; }

        /// <summary>
        /// Global Order identifier
        /// </summary>
        /// <value>Global Order identifier</value>
        /// <example>018792C9-4AD7-8EC4-4038-9E05C598534A</example>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = true)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets OrderHash
        /// </summary>
        /// <example>0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266</example>
        [DataMember(Name = "order_hash", IsRequired = true, EmitDefaultValue = true)]
        public string OrderHash { get; set; }

        /// <summary>
        /// Gets or Sets ProtocolData
        /// </summary>
        [DataMember(Name = "protocol_data", IsRequired = true, EmitDefaultValue = true)]
        public ProtocolData ProtocolData { get; set; }

        /// <summary>
        /// A random value added to the create Order request
        /// </summary>
        /// <value>A random value added to the create Order request</value>
        /// <example>12686911856931635052326433555881236148</example>
        [DataMember(Name = "salt", IsRequired = true, EmitDefaultValue = true)]
        public string Salt { get; set; }

        /// <summary>
        /// Gets or Sets Sell
        /// </summary>
        /// <example>[{&quot;type&quot;:&quot;ERC721&quot;,&quot;contract_address&quot;:&quot;0x692edAd005237c7E737bB2c0F3D8ccCc10D3479E&quot;,&quot;token_id&quot;:&quot;1&quot;}]</example>
        [DataMember(Name = "sell", IsRequired = true, EmitDefaultValue = true)]
        public List<Item> Sell { get; set; }

        /// <summary>
        /// Digital signature generated by the user for the specific Order
        /// </summary>
        /// <value>Digital signature generated by the user for the specific Order</value>
        /// <example>0x</example>
        [DataMember(Name = "signature", IsRequired = true, EmitDefaultValue = true)]
        public string Signature { get; set; }

        /// <summary>
        /// Time after which Order is considered active
        /// </summary>
        /// <value>Time after which Order is considered active</value>
        /// <example>2022-03-09T05:00:50.520Z</example>
        [DataMember(Name = "start_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime StartAt { get; set; }

        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Time the Order is last updated
        /// </summary>
        /// <value>Time the Order is last updated</value>
        /// <example>2022-03-07T07:20:50.520Z</example>
        [DataMember(Name = "updated_at", IsRequired = true, EmitDefaultValue = true)]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or Sets FillStatus
        /// </summary>
        [DataMember(Name = "fill_status", IsRequired = true, EmitDefaultValue = true)]
        public FillStatus FillStatus { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Order {\n");
            sb.Append("  AccountAddress: ").Append(AccountAddress).Append("\n");
            sb.Append("  Buy: ").Append(Buy).Append("\n");
            sb.Append("  Fees: ").Append(Fees).Append("\n");
            sb.Append("  Chain: ").Append(Chain).Append("\n");
            sb.Append("  CreatedAt: ").Append(CreatedAt).Append("\n");
            sb.Append("  EndAt: ").Append(EndAt).Append("\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  OrderHash: ").Append(OrderHash).Append("\n");
            sb.Append("  ProtocolData: ").Append(ProtocolData).Append("\n");
            sb.Append("  Salt: ").Append(Salt).Append("\n");
            sb.Append("  Sell: ").Append(Sell).Append("\n");
            sb.Append("  Signature: ").Append(Signature).Append("\n");
            sb.Append("  StartAt: ").Append(StartAt).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  UpdatedAt: ").Append(UpdatedAt).Append("\n");
            sb.Append("  FillStatus: ").Append(FillStatus).Append("\n");
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
