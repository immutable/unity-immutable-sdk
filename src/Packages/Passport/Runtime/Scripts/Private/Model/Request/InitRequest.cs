using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Immutable.Passport.Model;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]

    internal class InitRequest
    {
        public string ClientId;
        public string Environment;
        public string? RedirectUri;
        public VersionInfo EngineVersion;
    }
}
