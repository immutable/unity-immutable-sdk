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

namespace Immutable.Orderbook.Model
{
    /// <summary>
    ///     RecordStringTypedDataFieldValueInner
    /// </summary>
    [DataContract(Name = "RecordStringTypedDataField_value_inner")]
    public class RecordStringTypedDataFieldValueInner
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordStringTypedDataFieldValueInner" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected RecordStringTypedDataFieldValueInner()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordStringTypedDataFieldValueInner" /> class.
        /// </summary>
        /// <param name="name">name (required).</param>
        /// <param name="type">type (required).</param>
        public RecordStringTypedDataFieldValueInner(string name = default, string type = default)
        {
            // to ensure "name" is required (not null)
            if (name == null)
                throw new ArgumentNullException(
                    "name is a required property for RecordStringTypedDataFieldValueInner and cannot be null");
            Name = name;
            // to ensure "type" is required (not null)
            if (type == null)
                throw new ArgumentNullException(
                    "type is a required property for RecordStringTypedDataFieldValueInner and cannot be null");
            Type = type;
        }

        /// <summary>
        ///     Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public string Type { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class RecordStringTypedDataFieldValueInner {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
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