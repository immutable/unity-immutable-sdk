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

namespace Immutable.Orderbook.Model
{
    /// <summary>
    ///     OrderFillStatus
    /// </summary>
    [DataContract(Name = "Order_fillStatus")]
    public class OrderFillStatus
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OrderFillStatus" /> class.
        /// </summary>
        /// <param name="denominator">denominator.</param>
        /// <param name="numerator">numerator.</param>
        public OrderFillStatus(string denominator = default, string numerator = default)
        {
            Denominator = denominator;
            Numerator = numerator;
        }

        /// <summary>
        ///     Gets or Sets Denominator
        /// </summary>
        [DataMember(Name = "denominator", EmitDefaultValue = false)]
        public string Denominator { get; set; }

        /// <summary>
        ///     Gets or Sets Numerator
        /// </summary>
        [DataMember(Name = "numerator", EmitDefaultValue = false)]
        public string Numerator { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class OrderFillStatus {\n");
            sb.Append("  Denominator: ").Append(Denominator).Append("\n");
            sb.Append("  Numerator: ").Append(Numerator).Append("\n");
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