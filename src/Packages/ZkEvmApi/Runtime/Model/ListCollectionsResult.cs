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
    /// ListCollectionsResult
    /// </summary>
    [DataContract(Name = "ListCollectionsResult")]
    public partial class ListCollectionsResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListCollectionsResult" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ListCollectionsResult() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ListCollectionsResult" /> class.
        /// </summary>
        /// <param name="result">List of collections (required).</param>
        /// <param name="page">page (required).</param>
        public ListCollectionsResult(List<Collection> result = default(List<Collection>), Page page = default(Page))
        {
            // to ensure "result" is required (not null)
            if (result == null)
            {
                throw new ArgumentNullException("result is a required property for ListCollectionsResult and cannot be null");
            }
            this.Result = result;
            // to ensure "page" is required (not null)
            if (page == null)
            {
                throw new ArgumentNullException("page is a required property for ListCollectionsResult and cannot be null");
            }
            this.Page = page;
        }

        /// <summary>
        /// List of collections
        /// </summary>
        /// <value>List of collections</value>
        [DataMember(Name = "result", IsRequired = true, EmitDefaultValue = true)]
        public List<Collection> Result { get; set; }

        /// <summary>
        /// Gets or Sets Page
        /// </summary>
        [DataMember(Name = "page", IsRequired = true, EmitDefaultValue = true)]
        public Page Page { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class ListCollectionsResult {\n");
            sb.Append("  Result: ").Append(Result).Append("\n");
            sb.Append("  Page: ").Append(Page).Append("\n");
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