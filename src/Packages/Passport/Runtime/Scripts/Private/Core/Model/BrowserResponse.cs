using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Core
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BrowserResponse
    {
        public string? ResponseFor;
        public string? RequestId;
        [JsonProperty(Required = Required.Always)]
        public bool Success;
        public string? ErrorType;
        public string? Error;
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class StringResponse : BrowserResponse
    {
        public string? Result;
    }
}
