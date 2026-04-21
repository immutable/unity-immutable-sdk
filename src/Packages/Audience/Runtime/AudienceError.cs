using System;

namespace Immutable.Audience
{
    public enum AudienceErrorCode
    {
        FlushFailed,
        ValidationRejected,
        ConsentSyncFailed,
        NetworkError,
        /// <summary>Failed to persist the consent level to disk. In-memory level still applied but will revert on next launch.</summary>
        ConsentPersistFailed
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
