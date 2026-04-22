using System;

namespace Immutable.Audience
{
    public enum AudienceErrorCode
    {
        // An event batch failed to flush. Either a local storage read error (batch dropped) or a non-2xx/non-4xx server response — typically 5xx (batch retained and retried with backoff).
        FlushFailed,
        // Server rejected an event batch with a 4xx status. The batch was dropped; retrying will not help (typically indicates a malformed payload).
        ValidationRejected,
        // Failed to sync a consent change to the backend. The local consent level has already been applied; the server-side audit trail may be out of date.
        ConsentSyncFailed,
        // A network call failed (exception, timeout, or non-2xx response on data deletion). Event batches are retained for retry; data-delete requests are not retried automatically.
        NetworkError,
        // Failed to persist the consent level to disk. In-memory level still applied but will revert on next launch.
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
