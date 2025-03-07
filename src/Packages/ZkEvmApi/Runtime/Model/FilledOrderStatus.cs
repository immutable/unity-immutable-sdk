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
    /// FilledOrderStatus
    /// </summary>
    [DataContract(Name = "FilledOrderStatus")]
    public partial class FilledOrderStatus
    {
        /// <summary>
        /// A terminal order status indicating that an order has been fulfilled.
        /// </summary>
        /// <value>A terminal order status indicating that an order has been fulfilled.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum NameEnum
        {
            /// <summary>
            /// Enum FILLED for value: FILLED
            /// </summary>
            [EnumMember(Value = "FILLED")]
            FILLED = 1
        }


        /// <summary>
        /// A terminal order status indicating that an order has been fulfilled.
        /// </summary>
        /// <value>A terminal order status indicating that an order has been fulfilled.</value>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public NameEnum Name { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilledOrderStatus" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected FilledOrderStatus() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilledOrderStatus" /> class.
        /// </summary>
        /// <param name="name">A terminal order status indicating that an order has been fulfilled. (required).</param>
        public FilledOrderStatus(NameEnum name = default(NameEnum))
        {
            this.Name = name;
        }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class FilledOrderStatus {\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
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
