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
    // Sends queued events from DiskStore to the Audience backend.
    internal sealed class HttpTransport : IDisposable
    {
        private readonly DiskStore _store;
        private readonly string _url;
        private readonly string _publishableKey;
        private readonly HttpClient _client;
        private readonly Action<AudienceError>? _onError;
        private readonly Func<DateTime> _getUtcNow;

        private readonly object _backoffLock = new object();
        private int _consecutiveFailures;
        private DateTime? _nextAttemptAt;

        // store: source of event batches.
        // publishableKey: sent as x-immutable-publishable-key on every request.
        // baseUrlOverride: explicit backend URL. Null = derive from publishableKey prefix.
        // onError: optional failure callback. Exceptions thrown inside it are caught.
        // handler / getUtcNow: test seams; null for production use.
        internal HttpTransport(
            DiskStore store,
            string publishableKey,
            string? baseUrlOverride = null,
            Action<AudienceError>? onError = null,
            HttpMessageHandler? handler = null,
            Func<DateTime>? getUtcNow = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
            _url = Constants.MessagesUrl(publishableKey, baseUrlOverride);
            _onError = onError;
            // disposeHandler: false so the consumer can reuse their handler
            // across Init/Shutdown cycles (matches _controlClient's policy).
            _client = handler != null
                ? new HttpClient(handler, disposeHandler: false)
                : new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(30);
            _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
        }

        // Processes one batch. Returns true if a batch was consumed
        // (outcome irrelevant), false if the queue was empty.
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

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _url);
                request.Headers.Add(Constants.PublishableKeyHeader, _publishableKey);
#if IMMUTABLE_AUDIENCE_GZIP
                var compressed = Gzip.Compress(payload);
                request.Content = new ByteArrayContent(compressed);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content.Headers.Add("Content-Encoding", "gzip");
#else
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
#endif

                using var response = await _client.SendAsync(request, ct).ConfigureAwait(false);

                var statusCode = (int)response.StatusCode;

                if (statusCode >= 200 && statusCode < 300)
                {
                    // Server accepted the batch. Count how many messages it
                    // rejected; if any, tell the studio via onError. Rejected
                    // messages are validation failures — retrying won't help,
                    // so the batch is deleted either way.
                    var rejected = await ParseRejectedCount(response).ConfigureAwait(false);
                    _store.Delete(batch);
                    ResetBackoff();
                    if (rejected > 0)
                    {
                        NotifyError(AudienceErrorCode.ValidationRejected,
                            $"Batch partially rejected: {rejected} of {batch.Count} events dropped");
                    }
                }
                else if (statusCode >= 400 && statusCode < 500)
                {
                    // 4xx: server rejected the payload. Drop it (retry won't help) and
                    // reset backoff — server is healthy, our data was the problem.
                    // Capture the response body so the caller's OnError surfaces
                    // the server's reason string ("unknown publishable key",
                    // "missing field X", etc.) rather than a bare status code.
                    var rejectionBody = await ReadBodyForErrorAsync(response).ConfigureAwait(false);
                    _store.Delete(batch);
                    ResetBackoff();
                    NotifyError(AudienceErrorCode.ValidationRejected,
                        FormatHttpError("Batch rejected", statusCode, rejectionBody));
                }
                else
                {
                    // 5xx (or other non-2xx/4xx): server is unhealthy or the response
                    // is anomalous. Keep batch on disk, back off, retry later.
                    var serverBody = await ReadBodyForErrorAsync(response).ConfigureAwait(false);
                    RecordFailure();
                    NotifyError(AudienceErrorCode.FlushFailed,
                        FormatHttpError("Server error, will retry", statusCode, serverBody));
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Caller cancelled the token (e.g. on shutdown). Events stay
                // on disk, no failure recorded. Rethrow so the caller's send
                // loop exits — swallowing here returns `true`, and the loop
                // would re-enter on the same cancelled token and spin because
                // the batch is still on disk. HttpClient timeouts throw the
                // same exception but without ct.IsCancellationRequested set,
                // so they fall through to the Exception branch below and
                // trigger backoff.
                throw;
            }
            catch (Exception ex)
            {
                RecordFailure();
                NotifyError(AudienceErrorCode.NetworkError, ex.Message);
            }

            return true;
        }

        internal int BackoffMs
        {
            get
            {
                lock (_backoffLock) return BackoffMsLocked();
            }
        }

        private int BackoffMsLocked() => _consecutiveFailures switch
        {
            <= 0 => 0,
            1 => 5_000,
            2 => 10_000,
            3 => 20_000,
            4 => 40_000,
            _ => 60_000,
        };

        // Earliest UTC time at which the next attempt may run.
        // Null when no backoff is active.
        internal DateTime? NextAttemptAt
        {
            get { lock (_backoffLock) return _nextAttemptAt; }
        }

        // True while UtcNow < NextAttemptAt. Flips false as the clock advances.
        internal bool IsInBackoffWindow
        {
            get { lock (_backoffLock) return _getUtcNow() < _nextAttemptAt; }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private void RecordFailure()
        {
            lock (_backoffLock)
            {
                var now = _getUtcNow();
                if (now < _nextAttemptAt) return;  // inside prior window — don't compound backoff
                _consecutiveFailures++;
                _nextAttemptAt = now.AddMilliseconds(BackoffMsLocked());
            }
        }

        private void ResetBackoff()
        {
            lock (_backoffLock)
            {
                _consecutiveFailures = 0;
                _nextAttemptAt = null;
            }
        }

        // Reads each path and wraps the concatenated JSON bodies in
        // {"messages":[msg1,msg2,...]}. Returns null if every path was
        // unreadable; the caller treats null as "nothing to send".
        // Wire-format envelope key matches the backend's MessagesRequest
        // schema (#/components/schemas/MessagesRequest property "messages").
        private static string? BuildPayload(IReadOnlyList<string> paths)
        {
            var sb = new StringBuilder("{\"messages\":[");
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

        // Reads the response body and pulls out the "rejected" count. Returns
        // 0 if the body is missing or unreadable — the body is only for
        // reporting, so failing to read it must not break the success path.
        private static async Task<int> ParseRejectedCount(HttpResponseMessage response)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
            if (string.IsNullOrEmpty(body)) return 0;

            try
            {
                var parsed = JsonReader.DeserializeObject(body);
                if (!parsed.TryGetValue("rejected", out var raw)) return 0;
                return raw switch
                {
                    int i => i,
                    long l => (int)l,
                    _ => 0,
                };
            }
            catch (FormatException)
            {
                return 0;
            }
        }

        // Cap the response-body excerpt the caller sees. Backends sometimes
        // return verbose stack traces or HTML error pages; trimming keeps
        // OnError consumers (loggers, UI) from being flooded.
        private const int MaxErrorBodyChars = 500;

        // Reads the response body for inclusion in an error message. Failures
        // (read throws, body is empty) collapse to null so the caller can fall
        // back to "<status code> only" formatting without nesting nulls.
        private static async Task<string?> ReadBodyForErrorAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body)) return null;
                body = body.Trim();
                return body.Length > MaxErrorBodyChars
                    ? body.Substring(0, MaxErrorBodyChars) + "…"
                    : body;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatHttpError(string prefix, int statusCode, string? body) =>
            string.IsNullOrEmpty(body)
                ? $"{prefix} with {statusCode}"
                : $"{prefix} with {statusCode}: {body}";

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