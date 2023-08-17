namespace Immutable.Passport
{
    public class Request
    {
        public string fxName;
        public string requestId;
        public string? data;

        public Request(string fxName, string requestId, string? data)
        {
            this.fxName = fxName;
            this.requestId = requestId;
            this.data = data;
        }
    }

    internal class ConfirmCodeRequest
    {
        public string deviceCode;
        public int interval;
        public long? timeoutMs;
    }

    internal class ConnectPKCERequest
    {
        public string authorizationCode;
        public string state;
    }

    internal class InitRequest
    {
        public string clientId;
        public string environment;
        public string? redirectUri;
    }
}

