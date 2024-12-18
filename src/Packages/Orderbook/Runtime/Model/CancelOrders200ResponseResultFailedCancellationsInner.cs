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
    ///     CancelOrders200ResponseResultFailedCancellationsInner
    /// </summary>
    [DataContract(Name = "cancelOrders_200_response_result_failed_cancellations_inner")]
    public class CancelOrders200ResponseResultFailedCancellationsInner
    {
        /// <summary>
        ///     Reason code indicating why the order failed to be cancelled
        /// </summary>
        /// <value>Reason code indicating why the order failed to be cancelled</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ReasonCodeEnum
        {
            /// <summary>
            ///     Enum FILLED for value: FILLED
            /// </summary>
            [EnumMember(Value = "FILLED")] FILLED
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CancelOrders200ResponseResultFailedCancellationsInner" /> class.
        /// </summary>
        /// <param name="order">ID of the order which failed to be cancelled.</param>
        /// <param name="reasonCode">Reason code indicating why the order failed to be cancelled.</param>
        public CancelOrders200ResponseResultFailedCancellationsInner(string order = default,
            ReasonCodeEnum? reasonCode = default)
        {
            Order = order;
            ReasonCode = reasonCode;
        }


        /// <summary>
        ///     Reason code indicating why the order failed to be cancelled
        /// </summary>
        /// <value>Reason code indicating why the order failed to be cancelled</value>
        [DataMember(Name = "reason_code", EmitDefaultValue = false)]
        public ReasonCodeEnum? ReasonCode { get; set; }

        /// <summary>
        ///     ID of the order which failed to be cancelled
        /// </summary>
        /// <value>ID of the order which failed to be cancelled</value>
        [DataMember(Name = "order", EmitDefaultValue = false)]
        public string Order { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class CancelOrders200ResponseResultFailedCancellationsInner {\n");
            sb.Append("  Order: ").Append(Order).Append("\n");
            sb.Append("  ReasonCode: ").Append(ReasonCode).Append("\n");
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