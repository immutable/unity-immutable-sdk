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

    internal class AddressResponse : Response
    {
        public string? address;
    }

    internal class GetImxProviderResponse : Response
    {
    }

    internal class StringResponse : Response
    {
        public string? result;
    }
}
