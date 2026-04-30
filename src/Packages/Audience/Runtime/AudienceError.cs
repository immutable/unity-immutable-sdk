using System;

namespace Immutable.Audience
{
    /// <summary>
    /// Categorises errors raised through <see cref="AudienceConfig.OnError"/>.
    /// </summary>
    public enum AudienceErrorCode
    {
        /// <summary>
        /// An event batch failed to flush.
        /// </summary>
        /// <remarks>
        /// Either a local storage read error (batch dropped) or a non-2xx /
        /// non-4xx server response, typically 5xx (batch retained and retried
        /// with backoff).
        /// </remarks>
        FlushFailed,

        /// <summary>
        /// Server rejected an event batch with a 4xx status.
        /// </summary>
        /// <remarks>
        /// The batch was dropped. Retrying will not help (typically indicates
        /// a malformed payload).
        /// </remarks>
        ValidationRejected,

        /// <summary>
        /// Failed to sync a consent change to the backend.
        /// </summary>
        /// <remarks>
        /// The local consent level has already been applied. The server-side
        /// audit trail may be out of date.
        /// </remarks>
        ConsentSyncFailed,

        /// <summary>
        /// A network call failed.
        /// </summary>
        /// <remarks>
        /// Causes include exceptions, timeouts, or a non-2xx response on data
        /// deletion. Event batches are retained for retry. Data-delete
        /// requests are not retried automatically.
        /// </remarks>
        NetworkError,

        /// <summary>
        /// Failed to persist the consent level to disk.
        /// </summary>
        /// <remarks>
        /// The in-memory level is still applied but will revert on next
        /// launch.
        /// </remarks>
        ConsentPersistFailed
    }

    /// <summary>
    /// Error raised through <see cref="AudienceConfig.OnError"/> when the SDK
    /// encounters a recoverable failure.
    /// </summary>
    public class AudienceError : Exception
    {
        /// <summary>
        /// The reason this error was raised.
        /// </summary>
        public AudienceErrorCode Code { get; }

        /// <summary>
        /// Wraps a code and message into an <see cref="AudienceError"/> for
        /// delivery through <see cref="AudienceConfig.OnError"/>.
        /// </summary>
        /// <param name="code">The reason for the failure.</param>
        /// <param name="message">Human-readable description.</param>
        public AudienceError(AudienceErrorCode code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
