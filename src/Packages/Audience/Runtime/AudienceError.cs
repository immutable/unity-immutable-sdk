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

        public AudienceError(AudienceErrorCode code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
