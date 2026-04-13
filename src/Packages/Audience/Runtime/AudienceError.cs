using System;

namespace Immutable.Audience
{
    public enum AudienceErrorCode
    {
        FlushFailed,
        ValidationRejected,
        ConsentSyncFailed,
        NetworkError
    }

    public class AudienceError : Exception
    {
        public AudienceErrorCode Code { get; }
        public int Status { get; }
        public string Endpoint { get; }

        public AudienceError(AudienceErrorCode code, string message, int status = 0, string endpoint = null)
            : base(message)
        {
            Code = code;
            Status = status;
            Endpoint = endpoint;
        }
    }
}
