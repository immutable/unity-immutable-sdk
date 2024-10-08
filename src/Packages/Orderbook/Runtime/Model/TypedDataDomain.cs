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
    ///     TypedDataDomain
    /// </summary>
    [DataContract(Name = "TypedDataDomain")]
    public class TypedDataDomain
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TypedDataDomain" /> class.
        /// </summary>
        /// <param name="chainId">chainId.</param>
        /// <param name="name">name.</param>
        /// <param name="salt">salt.</param>
        /// <param name="verifyingContract">verifyingContract.</param>
        /// <param name="varVersion">varVersion.</param>
        public TypedDataDomain(string chainId = default, string name = default, string salt = default,
            string verifyingContract = default, string varVersion = default)
        {
            ChainId = chainId;
            Name = name;
            Salt = salt;
            VerifyingContract = verifyingContract;
            VarVersion = varVersion;
        }

        /// <summary>
        ///     Gets or Sets ChainId
        /// </summary>
        [DataMember(Name = "chainId", EmitDefaultValue = false)]
        public string ChainId { get; set; }

        /// <summary>
        ///     Gets or Sets Name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or Sets Salt
        /// </summary>
        [DataMember(Name = "salt", EmitDefaultValue = false)]
        public string Salt { get; set; }

        /// <summary>
        ///     Gets or Sets VerifyingContract
        /// </summary>
        [DataMember(Name = "verifyingContract", EmitDefaultValue = false)]
        public string VerifyingContract { get; set; }

        /// <summary>
        ///     Gets or Sets VarVersion
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string VarVersion { get; set; }

        /// <summary>
        ///     Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class TypedDataDomain {\n");
            sb.Append("  ChainId: ").Append(ChainId).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Salt: ").Append(Salt).Append("\n");
            sb.Append("  VerifyingContract: ").Append(VerifyingContract).Append("\n");
            sb.Append("  VarVersion: ").Append(VarVersion).Append("\n");
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