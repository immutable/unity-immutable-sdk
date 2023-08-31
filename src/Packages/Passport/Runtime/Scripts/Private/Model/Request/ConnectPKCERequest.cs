using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]

    internal class ConnectPKCERequest
    {
        public string AuthorizationCode;
        public string State;
    }
}

