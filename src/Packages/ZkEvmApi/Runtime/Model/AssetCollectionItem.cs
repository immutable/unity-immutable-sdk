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
using System.Reflection;

namespace Immutable.Api.ZkEvm.Model
{
    /// <summary>
    /// AssetCollectionItem
    /// </summary>
    [JsonConverter(typeof(AssetCollectionItemJsonConverter))]
    [DataContract(Name = "AssetCollectionItem")]
    public partial class AssetCollectionItem : AbstractOpenAPISchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCollectionItem" /> class
        /// with the <see cref="ERC721CollectionItem" /> class
        /// </summary>
        /// <param name="actualInstance">An instance of ERC721CollectionItem.</param>
        public AssetCollectionItem(ERC721CollectionItem actualInstance)
        {
            this.IsNullable = false;
            this.SchemaType= "oneOf";
            this.ActualInstance = actualInstance ?? throw new ArgumentException("Invalid instance found. Must not be null.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCollectionItem" /> class
        /// with the <see cref="ERC1155CollectionItem" /> class
        /// </summary>
        /// <param name="actualInstance">An instance of ERC1155CollectionItem.</param>
        public AssetCollectionItem(ERC1155CollectionItem actualInstance)
        {
            this.IsNullable = false;
            this.SchemaType= "oneOf";
            this.ActualInstance = actualInstance ?? throw new ArgumentException("Invalid instance found. Must not be null.");
        }


        private Object _actualInstance;

        /// <summary>
        /// Gets or Sets ActualInstance
        /// </summary>
        public override Object ActualInstance
        {
            get
            {
                return _actualInstance;
            }
            set
            {
                if (value.GetType() == typeof(ERC1155CollectionItem) || value is ERC1155CollectionItem)
                {
                    this._actualInstance = value;
                }
                else if (value.GetType() == typeof(ERC721CollectionItem) || value is ERC721CollectionItem)
                {
                    this._actualInstance = value;
                }
                else
                {
                    throw new ArgumentException("Invalid instance found. Must be the following types: ERC1155CollectionItem, ERC721CollectionItem");
                }
            }
        }

        /// <summary>
        /// Get the actual instance of `ERC721CollectionItem`. If the actual instance is not `ERC721CollectionItem`,
        /// the InvalidClassException will be thrown
        /// </summary>
        /// <returns>An instance of ERC721CollectionItem</returns>
        public ERC721CollectionItem GetERC721CollectionItem()
        {
            return (ERC721CollectionItem)this.ActualInstance;
        }

        /// <summary>
        /// Get the actual instance of `ERC1155CollectionItem`. If the actual instance is not `ERC1155CollectionItem`,
        /// the InvalidClassException will be thrown
        /// </summary>
        /// <returns>An instance of ERC1155CollectionItem</returns>
        public ERC1155CollectionItem GetERC1155CollectionItem()
        {
            return (ERC1155CollectionItem)this.ActualInstance;
        }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AssetCollectionItem {\n");
            sb.Append("  ActualInstance: ").Append(this.ActualInstance).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public override string ToJson()
        {
            return JsonConvert.SerializeObject(this.ActualInstance, AssetCollectionItem.SerializerSettings);
        }

        /// <summary>
        /// Converts the JSON string into an instance of AssetCollectionItem
        /// </summary>
        /// <param name="jsonString">JSON string</param>
        /// <returns>An instance of AssetCollectionItem</returns>
        public static AssetCollectionItem FromJson(string jsonString)
        {
            AssetCollectionItem newAssetCollectionItem = null;

            if (string.IsNullOrEmpty(jsonString))
            {
                return newAssetCollectionItem;
            }
            int match = 0;
            List<string> matchedTypes = new List<string>();

            try
            {
                // if it does not contains "AdditionalProperties", use SerializerSettings to deserialize
                if (typeof(ERC1155CollectionItem).GetProperty("AdditionalProperties") == null)
                {
                    newAssetCollectionItem = new AssetCollectionItem(JsonConvert.DeserializeObject<ERC1155CollectionItem>(jsonString, AssetCollectionItem.SerializerSettings));
                }
                else
                {
                    newAssetCollectionItem = new AssetCollectionItem(JsonConvert.DeserializeObject<ERC1155CollectionItem>(jsonString, AssetCollectionItem.AdditionalPropertiesSerializerSettings));
                }
                matchedTypes.Add("ERC1155CollectionItem");
                match++;
            }
            catch (Exception exception)
            {
                // deserialization failed, try the next one
                System.Diagnostics.Debug.WriteLine(string.Format("Failed to deserialize `{0}` into ERC1155CollectionItem: {1}", jsonString, exception.ToString()));
            }

            try
            {
                // if it does not contains "AdditionalProperties", use SerializerSettings to deserialize
                if (typeof(ERC721CollectionItem).GetProperty("AdditionalProperties") == null)
                {
                    newAssetCollectionItem = new AssetCollectionItem(JsonConvert.DeserializeObject<ERC721CollectionItem>(jsonString, AssetCollectionItem.SerializerSettings));
                }
                else
                {
                    newAssetCollectionItem = new AssetCollectionItem(JsonConvert.DeserializeObject<ERC721CollectionItem>(jsonString, AssetCollectionItem.AdditionalPropertiesSerializerSettings));
                }
                matchedTypes.Add("ERC721CollectionItem");
                match++;
            }
            catch (Exception exception)
            {
                // deserialization failed, try the next one
                System.Diagnostics.Debug.WriteLine(string.Format("Failed to deserialize `{0}` into ERC721CollectionItem: {1}", jsonString, exception.ToString()));
            }

            if (match == 0)
            {
                throw new InvalidDataException("The JSON string `" + jsonString + "` cannot be deserialized into any schema defined.");
            }
            else if (match > 1)
            {
                throw new InvalidDataException("The JSON string `" + jsonString + "` incorrectly matches more than one schema (should be exactly one match): " + String.Join(",", matchedTypes));
            }

            // deserialization is considered successful at this point if no exception has been thrown.
            return newAssetCollectionItem;
        }

    }

    /// <summary>
    /// Custom JSON converter for AssetCollectionItem
    /// </summary>
    public class AssetCollectionItemJsonConverter : JsonConverter
    {
        /// <summary>
        /// To write the JSON string
        /// </summary>
        /// <param name="writer">JSON writer</param>
        /// <param name="value">Object to be converted into a JSON string</param>
        /// <param name="serializer">JSON Serializer</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue((string)(typeof(AssetCollectionItem).GetMethod("ToJson").Invoke(value, null)));
        }

        /// <summary>
        /// To convert a JSON string into an object
        /// </summary>
        /// <param name="reader">JSON reader</param>
        /// <param name="objectType">Object type</param>
        /// <param name="existingValue">Existing value</param>
        /// <param name="serializer">JSON Serializer</param>
        /// <returns>The object converted from the JSON string</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch(reader.TokenType) 
            {
                case JsonToken.StartObject:
                    return AssetCollectionItem.FromJson(JObject.Load(reader).ToString(Formatting.None));
                case JsonToken.StartArray:
                    return AssetCollectionItem.FromJson(JArray.Load(reader).ToString(Formatting.None));
                default:
                    return null;
            }
        }

        /// <summary>
        /// Check if the object can be converted
        /// </summary>
        /// <param name="objectType">Object type</param>
        /// <returns>True if the object can be converted</returns>
        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }

}
