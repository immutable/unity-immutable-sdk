/*
 * TS SDK API
 *
 * running ts sdk as an api
 *
 * The version of the OpenAPI document: 1.0.0
 * Contact: contact@immutable.com
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Immutable.Orderbook.Model
{
    /// <summary>
    ///     OrderStatusOneOf1
    /// </summary>
    [DataContract(Name = "OrderStatus_oneOf_1")]
    public class OrderStatusOneOf1
    {
        /// <summary>
        ///     The order status indicating a order is has been cancelled or about to be cancelled.
        /// </summary>
        /// <value>The order status indicating a order is has been cancelled or about to be cancelled.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum NameEnum
        {
            /// <summary>
            ///     Enum CANCELLED for value: CANCELLED
            /// </summary>
            [EnumMember(Value = "CANCELLED")] CANCELLED
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrderStatusOneOf1" /> class.
        /// </summary>
        /// <param name="cancellationType">cancellationType.</param>
        /// <param name="name">The order status indicating a order is has been cancelled or about to be cancelled..</param>
        /// <param name="pending">Whether the cancellation of the order is pending.</param>
        public OrderStatusOneOf1(CancellationType? cancellationType = default, NameEnum? name = default,
            bool pending = default)
        {
            CancellationType = cancellationType;
            Name = name;
            Pending = pending;
        }

        /// <summary>
        ///     Gets or Sets CancellationType
        /// </summary>
        [DataMember(Name = "cancellation_type", EmitDefaultValue = false)]
        public CancellationType? CancellationType { get; set; }


        /// <summary>
        ///     The order status indicating a order is has been cancelled or about to be cancelled.
        /// </summary>
        /// <value>The order status indicating a order is has been cancelled or about to be cancelled.</value>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public NameEnum? Name { get; set; }

        /// <summary>
        ///     Whether the cancellation of the order is pending
        /// </summary>
        /// <value>Whether the cancellation of the order is pending</value>
        [DataMember(Name = "pending", EmitDefaultValue = true)]
        public bool Pending { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OrderStatusOneOf1 {\n");
            sb.Append("  CancellationType: ").Append(CancellationType).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Pending: ").Append(Pending).Append("\n");
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