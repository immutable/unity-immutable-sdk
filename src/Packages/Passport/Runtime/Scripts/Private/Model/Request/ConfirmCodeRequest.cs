using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    internal class ConfirmCodeRequest
    {
        public string DeviceCode;
        public int Interval;
        public long? TimeoutMs;
    }
}

