using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]

    internal class InitRequest
    {
        public string ClientId;
        public string Environment;
        public string? RedirectUri;
        public string EngineVersion;
    }
}

