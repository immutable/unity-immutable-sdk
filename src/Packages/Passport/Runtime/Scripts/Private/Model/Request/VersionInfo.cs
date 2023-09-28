using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VersionInfo
    {
        public string? Engine;
        public string? EngineVersion;
        public string? Platform;
        public string? PlatformVersion;
    }
}
