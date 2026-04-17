using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Immutable.Audience
{
    /// <summary>
    /// Reads event batches from <see cref="DiskStore"/>, gzip-compresses them,
    /// and POSTs to <c>/v1/audience/messages</c>. Runs entirely on background
    /// threads via <see cref="HttpClient"/> — no main thread involvement.
    ///
    /// <para>Retry policy: 5xx and network errors keep events on disk with
    /// exponential backoff (5s → 10s → 20s → 60s cap). Escalation only
    /// occurs after the previous backoff window has elapsed — premature
    /// retries don't grow the backoff further. 4xx and 200 with rejected
    /// events are dropped — they won't succeed on retry.</para>
    /// </summary>
    internal sealed class HttpTransport : IDisposable
    {
        private readonly DiskStore _store;
        private readonly string _url;
        private readonly string _publishableKey;
        private readonly HttpClient _client;
        private readonly Action<AudienceError> _onError;
        private readonly Func<DateTime> _getUtcNow;

        private int _consecutiveFailures;
        private DateTime? _nextAttemptAt;

        /// <param name="store">Disk store to read batches from.</param>
        /// <param name="publishableKey">Sent as <c>x-immutable-publishable-key</c> header.</param>
        /// <param name="onError">Optional error callback. Never throws to the caller.</param>
        /// <param name="handler">Optional HttpMessageHandler for testing.</param>
        /// <param name="getUtcNow">Optional UTC clock for deterministic testing of backoff timing.</param>
        internal HttpTransport(
            DiskStore store,
            string publishableKey,
            Action<AudienceError> onError = null,
            HttpMessageHandler handler = null,
            Func<DateTime> getUtcNow = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _url = Constants.BaseUrl(publishableKey) + Constants.MessagesPath;
            _onError = onError;
            _client = handler != null ? new HttpClient(handler) : new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(30);
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Reads one batch from disk and sends it to the backend.
        /// Returns true if a batch was sent (regardless of outcome), false if the queue was empty.
        /// </summary>
        internal async Task<bool> SendBatchAsync(CancellationToken ct = default)
        {
            var batch = _store.ReadBatch(Constants.DefaultFlushSize);
            if (batch.Count == 0)
                return false;

            var payload = BuildPayload(batch);
            if (payload == null)
            {
                // All files were unreadable — delete them and move on.
                _store.Delete(batch);
                return true;
            }

            var compressed = Gzip.Compress(payload);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Headers.Add("x-immutable-publishable-key", _publishableKey);
                request.Content = new ByteArrayContent(compressed);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content.Headers.Add("Content-Encoding", "gzip");

                using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);

                var statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    _store.Delete(batch);
                    ResetBackoff();
                }
                else if (statusCode >= 400 && statusCode < 500)
                {
                    _store.Delete(batch);
                    ResetBackoff();
                    NotifyError(AudienceErrorCode.ValidationRejected,
                        $"Batch rejected with {statusCode}");
                }
                else
                {
                    RecordFailure();
                    NotifyError(AudienceErrorCode.FlushFailed, $"Server error {statusCode}, will retry");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutdown requested via cancellation token — don't increment failures,
                // events stay on disk. HttpClient timeouts throw TaskCanceledException too;
                // the `when` guard ensures those fall through to the general Exception
                // handler so backoff and the NetworkError callback fire correctly.
            }
            catch (Exception ex)
            {
                RecordFailure();
                NotifyError(AudienceErrorCode.NetworkError, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Backoff delay in milliseconds based on consecutive failures.
        /// Schedule per plan: 0 → 5s → 10s → 20s → 60s cap.
        /// </summary>
        internal int BackoffMs => _consecutiveFailures switch
        {
            <= 0 => 0,
            1 => 5_000,
            2 => 10_000,
            3 => 20_000,
            _ => 60_000,
        };

        /// <summary>
        /// Timestamp after which the next send attempt should run. Null when there's
        /// no active backoff (never failed, or last attempt succeeded). Callers should
        /// skip sending while <c>UtcNow &lt; NextAttemptAt</c>.
        /// </summary>
        internal DateTime? NextAttemptAt => _nextAttemptAt;

        /// <summary>
        /// True if a failure occurred recently and the backoff window has not yet
        /// elapsed. Becomes false naturally once enough time passes, allowing the
        /// next send attempt to proceed.
        /// </summary>
        internal bool IsInBackoffWindow => _getUtcNow() < _nextAttemptAt;

        public void Dispose()
        {
            _client.Dispose();
        }

        private void RecordFailure()
        {
            var now = _getUtcNow();

            // If we're still inside the previous backoff window, the caller retried
            // before honoring the wait. Don't escalate — the existing NextAttemptAt
            // deadline stands. When _nextAttemptAt is null (no prior failure),
            // `now < null` is false via lifted nullable comparison, so we escalate.
            if (now < _nextAttemptAt)
                return;

            _consecutiveFailures++;
            _nextAttemptAt = now.AddMilliseconds(BackoffMs);
        }

        private void ResetBackoff()
        {
            _consecutiveFailures = 0;
            _nextAttemptAt = null;
        }

        /// <summary>
        /// Reads file contents for each path and builds the batch JSON payload:
        /// <c>{"batch":[msg1,msg2,...]}</c>
        /// Returns null if no files could be read.
        /// </summary>
        private static string BuildPayload(IReadOnlyList<string> paths)
        {
            var sb = new StringBuilder("{\"batch\":[");
            var count = 0;

            for (var i = 0; i < paths.Count; i++)
            {
                try
                {
                    var json = File.ReadAllText(paths[i]);
                    if (count > 0) sb.Append(',');
                    sb.Append(json);
                    count++;
                }
                catch (Exception)
                {
                    // File disappeared between ReadBatch and now — skip it.
                }
            }

            if (count == 0) return null;

            sb.Append("]}");
            return sb.ToString();
        }

        private void NotifyError(AudienceErrorCode code, string message)
        {
            if (_onError == null) return;
            try
            {
                _onError(new AudienceError(code, message));
            }
            catch
            {
                // Error callback itself threw — swallow to protect the SDK.
            }
        }
    }
}