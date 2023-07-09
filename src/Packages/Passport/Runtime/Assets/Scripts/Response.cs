namespace Immutable.Passport
{
    public class Response
    {
        public string? responseFor;
        public string? requestId;
        public bool success;
        public string? errorType;
        public string? error;
    }

    public class InitResponse : Response
    {
    }

    public class DeviceConnectResponse
    {
        public string code;
        public string deviceCode;
        public string url;
        public int interval;
    }

    public class AddressResponse : Response
    {
        public string? address;
    }

    internal class GetImxProviderResponse : Response
    {
    }

    public class StringResponse : Response
    {
        public string? result;
    }
}
