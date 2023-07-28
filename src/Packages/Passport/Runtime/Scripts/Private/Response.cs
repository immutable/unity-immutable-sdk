using Newtonsoft.Json;

namespace Immutable.Passport
{
    public class Response
    {
        public string? responseFor;
        public string? requestId;
        [JsonProperty(Required = Required.Always)]
        public bool success;
        public string? errorType;
        public string? error;
    }

    public class DeviceConnectResponse
    {
        public string code;
        public string deviceCode;
        public string url;
        public int interval;
    }

    public class StringResponse : Response
    {
        public string? result;
    }
}
