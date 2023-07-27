using Newtonsoft.Json;

namespace Immutable.Passport.Json
{
    public static class JsonExtensions
    {
        /// <summary>
        /// Return null if the deserialization fails. 
        /// To require non-null values apply [JsonProperty(Required = Required.Always)] 
        /// above member declarations.
        /// </summary> 
        public static T? OptDeserializeObject<T>(this string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonSerializationException)
            {
                return null;
            }
        }
    }
}