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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Immutable.Orderbook.Model
{
    /// <summary>
    ///     OrderComponents
    /// </summary>
    [DataContract(Name = "OrderComponents")]
    public class OrderComponents
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OrderComponents" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected OrderComponents()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrderComponents" /> class.
        /// </summary>
        /// <param name="conduitKey">conduitKey (required).</param>
        /// <param name="consideration">consideration (required).</param>
        /// <param name="endTime">endTime (required).</param>
        /// <param name="offer">offer (required).</param>
        /// <param name="offerer">offerer (required).</param>
        /// <param name="orderType">orderType (required).</param>
        /// <param name="salt">salt (required).</param>
        /// <param name="startTime">startTime (required).</param>
        /// <param name="totalOriginalConsiderationItems">totalOriginalConsiderationItems (required).</param>
        /// <param name="zone">zone (required).</param>
        /// <param name="zoneHash">zoneHash (required).</param>
        /// <param name="counter">counter (required).</param>
        public OrderComponents(string conduitKey = default,
            List<OrderComponentsConsiderationInner> consideration = default, string endTime = default,
            List<OrderComponentsOfferInner> offer = default, string offerer = default, OrderType orderType = default,
            string salt = default, string startTime = default, string totalOriginalConsiderationItems = default,
            string zone = default, string zoneHash = default, string counter = default)
        {
            // to ensure "conduitKey" is required (not null)
            if (conduitKey == null)
                throw new ArgumentNullException(
                    "conduitKey is a required property for OrderComponents and cannot be null");
            ConduitKey = conduitKey;
            // to ensure "consideration" is required (not null)
            if (consideration == null)
                throw new ArgumentNullException(
                    "consideration is a required property for OrderComponents and cannot be null");
            Consideration = consideration;
            // to ensure "endTime" is required (not null)
            if (endTime == null)
                throw new ArgumentNullException(
                    "endTime is a required property for OrderComponents and cannot be null");
            EndTime = endTime;
            // to ensure "offer" is required (not null)
            if (offer == null)
                throw new ArgumentNullException("offer is a required property for OrderComponents and cannot be null");
            Offer = offer;
            // to ensure "offerer" is required (not null)
            if (offerer == null)
                throw new ArgumentNullException(
                    "offerer is a required property for OrderComponents and cannot be null");
            Offerer = offerer;
            OrderType = orderType;
            // to ensure "salt" is required (not null)
            if (salt == null)
                throw new ArgumentNullException("salt is a required property for OrderComponents and cannot be null");
            Salt = salt;
            // to ensure "startTime" is required (not null)
            if (startTime == null)
                throw new ArgumentNullException(
                    "startTime is a required property for OrderComponents and cannot be null");
            StartTime = startTime;
            // to ensure "totalOriginalConsiderationItems" is required (not null)
            if (totalOriginalConsiderationItems == null)
                throw new ArgumentNullException(
                    "totalOriginalConsiderationItems is a required property for OrderComponents and cannot be null");
            TotalOriginalConsiderationItems = totalOriginalConsiderationItems;
            // to ensure "zone" is required (not null)
            if (zone == null)
                throw new ArgumentNullException("zone is a required property for OrderComponents and cannot be null");
            Zone = zone;
            // to ensure "zoneHash" is required (not null)
            if (zoneHash == null)
                throw new ArgumentNullException(
                    "zoneHash is a required property for OrderComponents and cannot be null");
            ZoneHash = zoneHash;
            // to ensure "counter" is required (not null)
            if (counter == null)
                throw new ArgumentNullException(
                    "counter is a required property for OrderComponents and cannot be null");
            Counter = counter;
        }

        /// <summary>
        ///     Gets or Sets OrderType
        /// </summary>
        [DataMember(Name = "orderType", IsRequired = true, EmitDefaultValue = true)]
        public OrderType OrderType { get; set; }

        /// <summary>
        ///     Gets or Sets ConduitKey
        /// </summary>
        [DataMember(Name = "conduitKey", IsRequired = true, EmitDefaultValue = true)]
        public string ConduitKey { get; set; }

        /// <summary>
        ///     Gets or Sets Consideration
        /// </summary>
        [DataMember(Name = "consideration", IsRequired = true, EmitDefaultValue = true)]
        public List<OrderComponentsConsiderationInner> Consideration { get; set; }

        /// <summary>
        ///     Gets or Sets EndTime
        /// </summary>
        [DataMember(Name = "endTime", IsRequired = true, EmitDefaultValue = true)]
        public string EndTime { get; set; }

        /// <summary>
        ///     Gets or Sets Offer
        /// </summary>
        [DataMember(Name = "offer", IsRequired = true, EmitDefaultValue = true)]
        public List<OrderComponentsOfferInner> Offer { get; set; }

        /// <summary>
        ///     Gets or Sets Offerer
        /// </summary>
        [DataMember(Name = "offerer", IsRequired = true, EmitDefaultValue = true)]
        public string Offerer { get; set; }

        /// <summary>
        ///     Gets or Sets Salt
        /// </summary>
        [DataMember(Name = "salt", IsRequired = true, EmitDefaultValue = true)]
        public string Salt { get; set; }

        /// <summary>
        ///     Gets or Sets StartTime
        /// </summary>
        [DataMember(Name = "startTime", IsRequired = true, EmitDefaultValue = true)]
        public string StartTime { get; set; }

        /// <summary>
        ///     Gets or Sets TotalOriginalConsiderationItems
        /// </summary>
        [DataMember(Name = "totalOriginalConsiderationItems", IsRequired = true, EmitDefaultValue = true)]
        public string TotalOriginalConsiderationItems { get; set; }

        /// <summary>
        ///     Gets or Sets Zone
        /// </summary>
        [DataMember(Name = "zone", IsRequired = true, EmitDefaultValue = true)]
        public string Zone { get; set; }

        /// <summary>
        ///     Gets or Sets ZoneHash
        /// </summary>
        [DataMember(Name = "zoneHash", IsRequired = true, EmitDefaultValue = true)]
        public string ZoneHash { get; set; }

        /// <summary>
        ///     Gets or Sets Counter
        /// </summary>
        [DataMember(Name = "counter", IsRequired = true, EmitDefaultValue = true)]
        public string Counter { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OrderComponents {\n");
            sb.Append("  ConduitKey: ").Append(ConduitKey).Append("\n");
            sb.Append("  Consideration: ").Append(Consideration).Append("\n");
            sb.Append("  EndTime: ").Append(EndTime).Append("\n");
            sb.Append("  Offer: ").Append(Offer).Append("\n");
            sb.Append("  Offerer: ").Append(Offerer).Append("\n");
            sb.Append("  OrderType: ").Append(OrderType).Append("\n");
            sb.Append("  Salt: ").Append(Salt).Append("\n");
            sb.Append("  StartTime: ").Append(StartTime).Append("\n");
            sb.Append("  TotalOriginalConsiderationItems: ").Append(TotalOriginalConsiderationItems).Append("\n");
            sb.Append("  Zone: ").Append(Zone).Append("\n");
            sb.Append("  ZoneHash: ").Append(ZoneHash).Append("\n");
            sb.Append("  Counter: ").Append(Counter).Append("\n");
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