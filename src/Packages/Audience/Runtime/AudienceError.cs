#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    /// <summary>One validation failure the backend reported for a single message.</summary>
    public sealed class RejectionError
    {
        /// <summary>Name of the offending field, or a message-level sentinel when no single field applies.</summary>
        public string Field { get; }

        /// <summary>Machine-readable reason, e.g. <c>INVALID_ENUM</c>, <c>MISSING_REQUIRED_FIELD</c>.</summary>
        public string Code { get; }

        /// <summary>Human-readable description. Not contractual; wording may change.</summary>
        public string Message { get; }

        internal RejectionError(string field, string code, string message)
        {
            Field = field;
            Code = code;
            Message = message;
        }
    }

    /// <summary>A single message the backend rejected, with every reason it gave.</summary>
    public sealed class MessageRejection
    {
        /// <summary>Echoes the client-supplied messageId verbatim.</summary>
        public string MessageId { get; }

        /// <summary>Every violation reported for this message.</summary>
        public IReadOnlyList<RejectionError> Errors { get; }

        internal MessageRejection(string messageId, IReadOnlyList<RejectionError> errors)
        {
            MessageId = messageId;
            Errors = errors;
        }
    }

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
        /// Per-message rejection detail, when <see cref="Code"/> is
        /// <see cref="AudienceErrorCode.ValidationRejected"/> and the backend
        /// reported one.
        /// </summary>
        public IReadOnlyList<MessageRejection>? Rejections { get; }

        /// <summary>
        /// Wraps a code and message into an <see cref="AudienceError"/> for
        /// delivery through <see cref="AudienceConfig.OnError"/>.
        /// </summary>
        /// <param name="code">The reason for the failure.</param>
        /// <param name="message">Human-readable description.</param>
        /// <param name="rejections">Per-message rejection detail, when the backend reported one.</param>
        public AudienceError(AudienceErrorCode code, string message, IReadOnlyList<MessageRejection>? rejections = null)
            : base(message)
        {
            Code = code;
            Rejections = rejections;
        }
    }
}
