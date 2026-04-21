#nullable enable

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
    /// Sends queued events from <see cref="DiskStore"/> to the Audience backend.
    /// </summary>
    internal sealed class HttpTransport : IDisposable
    {
        private readonly DiskStore _store;
        private readonly string _url;
        private readonly string _publishableKey;
        private readonly HttpClient _client;
        private readonly Action<AudienceError>? _onError;
        private readonly Func<DateTime> _getUtcNow;

        private int _consecutiveFailures;
        private DateTime? _nextAttemptAt;

        /// <param name="store">Source of event batches.</param>
        /// <param name="publishableKey">Studio API key. Sent as <c>x-immutable-publishable-key</c> on every request.</param>
        /// <param name="onError">Optional failure callback. Exceptions thrown inside it are caught and ignored.</param>
        /// <param name="handler">Optional <see cref="HttpMessageHandler"/>. Callers can supply a custom pipeline (e.g. specific for test purposes). Defaults to the standard handler when null.</param>
        /// <param name="getUtcNow">Optional UTC clock source used for backoff timing (e.g. swappable for deterministic time). Defaults to <c>DateTime.UtcNow</c> when null.</param>
        internal HttpTransport(
            DiskStore store,
            string publishableKey,
            Action<AudienceError>? onError = null,
            HttpMessageHandler? handler = null,
            Func<DateTime>? getUtcNow = null)
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
        /// Attempts to process one batch: reads it from disk, gzips it, and POSTs it.
        /// Returns true if a batch was consumed (outcome irrelevant), false if the queue was empty.
        /// </summary>
        internal async Task<bool> SendBatchAsync(CancellationToken ct = default)
        {
            var batch = _store.ReadBatch(Constants.DefaultFlushSize);
            if (batch.Count == 0)
                return false;

            string? payload;
            try
            {
                payload = BuildPayload(batch);
            }
            catch (Exception ex)
            {
                // Non-IOException = unrecoverable storage failure (e.g. permissions);
                // retry won't help. Drop the batch, report via onError.
                _store.Delete(batch);
                NotifyError(AudienceErrorCode.FlushFailed, $"Local storage read failed: {ex.Message}");
                return true;
            }

            if (payload == null)
            {
                // Every file was unreadable (deleted or locked between ReadBatch and now).
                // Drop the refs, return.
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
                    // 2xx: server acked, drop the batch, healthy state.
                    _store.Delete(batch);
                    ResetBackoff();
                }
                else if (statusCode >= 400 && statusCode < 500)
                {
                    // 4xx: server rejected the payload. Drop it (retry won't help) and
                    // reset backoff — server is healthy, our data was the problem.
                    _store.Delete(batch);
                    ResetBackoff();
                    NotifyError(AudienceErrorCode.ValidationRejected,
                        $"Batch rejected with {statusCode}");
                }
                else
                {
                    // 5xx (or other non-2xx/4xx): server is unhealthy or the response
                    // is anomalous. Keep batch on disk, back off, retry later.
                    RecordFailure();
                    NotifyError(AudienceErrorCode.FlushFailed, $"Server error {statusCode}, will retry");
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Caller cancelled the token (e.g. on shutdown). Events stay on
                // disk, no failure recorded. HttpClient timeouts throw the same
                // exception but without ct.IsCancellationRequested set, so they
                // fall through to the Exception branch below and trigger backoff.
            }
            catch (Exception ex)
            {
                RecordFailure();
                NotifyError(AudienceErrorCode.NetworkError, ex.Message);
            }

            return true;
        }

        internal int BackoffMs => _consecutiveFailures switch
        {
            <= 0 => 0,
            1 => 5_000,
            2 => 10_000,
            3 => 20_000,
            4 => 40_000,
            _ => 60_000,
        };

        /// <summary>
        /// Earliest UTC time at which the next attempt may run.
        /// Null when no backoff is active (never failed, or last attempt succeeded).
        /// </summary>
        internal DateTime? NextAttemptAt => _nextAttemptAt;

        /// <summary>
        /// True while <c>UtcNow &lt; NextAttemptAt</c>. Flips false as the clock
        /// advances; no reset required.
        /// </summary>
        internal bool IsInBackoffWindow => _getUtcNow() < _nextAttemptAt;

        public void Dispose()
        {
            _client.Dispose();
        }

        private void RecordFailure()
        {
            var now = _getUtcNow();
            if (now < _nextAttemptAt) return;  // inside prior window — don't compound backoff

            _consecutiveFailures++;
            _nextAttemptAt = now.AddMilliseconds(BackoffMs);
        }

        private void ResetBackoff()
        {
            _consecutiveFailures = 0;
            _nextAttemptAt = null;
        }

        /// <summary>
        /// Reads each path and wraps the concatenated JSON bodies in
        /// <c>{"batch":[msg1,msg2,...]}</c>.
        /// </summary>
        /// <returns>
        /// The batched JSON, or <c>null</c> if every path was unreadable. Caller
        /// treats <c>null</c> as "nothing to send" and deletes the path list.
        /// </returns>
        private static string? BuildPayload(IReadOnlyList<string> paths)
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
                catch (IOException)
                {
                    // Transient disk race: the file was deleted or locked between
                    // ReadBatch and now. Safe to skip — the remaining paths in the
                    // batch may still read fine. Non-IOException failures escape
                    // and are handled by the caller (SendBatchAsync) as a batch-
                    // wide storage error.
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
                // Consumer callback threw. Swallow: the SDK must not surface
                // exceptions through the error-reporting path itself.
            }
        }
    }
}