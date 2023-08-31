using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class DeviceConnectResponse
    {
        public string Code;
        public string DeviceCode;
        public string Url;
        public int Interval;
    }
}
