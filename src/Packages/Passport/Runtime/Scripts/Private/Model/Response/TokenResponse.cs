using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenResponse
    {
        [JsonProperty(Required = Required.Always)]
        public string AccessToken;
        public string? RefreshToken;
        [JsonProperty(Required = Required.Always)]
        public string IdToken;
        [JsonProperty(Required = Required.Always)]
        public string TokenType;
        [JsonProperty(Required = Required.Always)]
        public int ExpiresIn;
    }
}