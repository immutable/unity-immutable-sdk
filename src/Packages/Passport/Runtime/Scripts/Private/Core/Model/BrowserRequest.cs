using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Core
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BrowserRequest
    {
        public string FxName;
        public string RequestId;
        public string? Data;
    }
}

