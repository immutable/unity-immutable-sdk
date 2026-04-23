using System;
using System.IO;
#if IMMUTABLE_AUDIENCE_GZIP
using System.IO.Compression;
#endif
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class HttpTransportTests
    {
        private string _testDir;
        private DiskStore _store;

        // Controllable clock for timing-sensitive tests. Tests that care about
        // backoff windows or NextAttemptAt pass `getUtcNow: _getUtcNow` to the transport
        // and use Advance(ms) to move time forward deterministically.
        private DateTime _utcNow;
        private Func<DateTime> _getUtcNow;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
            _store = new DiskStore(_testDir);
            _utcNow = new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc);
            _getUtcNow = () => _utcNow;
        }

        private void Advance(int milliseconds) =>
            _utcNow = _utcNow.AddMilliseconds(milliseconds);

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [Test]
        public async Task SendBatchAsync_200_DeletesFilesFromDisk()
        {
            _store.Write("{\"type\":\"track\",\"eventName\":\"a\"}");
            _store.Write("{\"type\":\"track\",\"eventName\":\"b\"}");

            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":2,\"rejected\":0}");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            var sent = await transport.SendBatchAsync();

            Assert.IsTrue(sent);
            Assert.AreEqual(0, _store.Count(), "files should be deleted after 200");
        }

#if IMMUTABLE_AUDIENCE_GZIP
        [Test]
        public async Task SendBatchAsync_200_SendsGzippedPayloadWithCorrectHeaders()
        {
            _store.Write("{\"type\":\"track\",\"eventName\":\"test\"}");

            byte[] capturedBody = null;
            string capturedKey = null;
            string capturedContentType = null;
            string capturedContentEncoding = null;
            // Read body inside the callback — the request content is disposed after SendAsync returns.
            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}",
                onRequest: req =>
                {
                    capturedKey = string.Join("", req.Headers.GetValues("x-immutable-publishable-key"));
                    capturedContentType = req.Content.Headers.ContentType.MediaType;
                    capturedContentEncoding = string.Join("", req.Content.Headers.ContentEncoding);
                    capturedBody = req.Content.ReadAsByteArrayAsync().Result;
                });
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual("pk_imapik-test-key1", capturedKey);
            Assert.AreEqual("application/json", capturedContentType);
            Assert.AreEqual("gzip", capturedContentEncoding);

            var decompressed = DecompressGzip(capturedBody);
            StringAssert.StartsWith("{\"batch\":[", decompressed);
            StringAssert.EndsWith("]}", decompressed);
            StringAssert.Contains("\"eventName\":\"test\"", decompressed);
        }
#else
        [Test]
        public async Task SendBatchAsync_200_SendsPlainJsonPayloadWithoutContentEncoding()
        {
            _store.Write("{\"type\":\"track\",\"eventName\":\"test\"}");

            string capturedKey = null;
            string capturedContentType = null;
            int capturedContentEncodingCount = -1;
            string capturedBody = null;
            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}",
                onRequest: req =>
                {
                    capturedKey = string.Join("", req.Headers.GetValues("x-immutable-publishable-key"));
                    capturedContentType = req.Content.Headers.ContentType.MediaType;
                    capturedContentEncodingCount = req.Content.Headers.ContentEncoding.Count;
                    capturedBody = req.Content.ReadAsStringAsync().Result;
                });
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual("pk_imapik-test-key1", capturedKey);
            Assert.AreEqual("application/json", capturedContentType);
            Assert.AreEqual(0, capturedContentEncodingCount, "no Content-Encoding header is permitted in v1");
            StringAssert.StartsWith("{\"batch\":[", capturedBody);
            StringAssert.EndsWith("]}", capturedBody);
            StringAssert.Contains("\"eventName\":\"test\"", capturedBody);
        }
#endif

        [Test]
        public async Task SendBatchAsync_200_UsesCorrectUrlForTestKey()
        {
            _store.Write("{\"type\":\"track\"}");

            HttpRequestMessage captured = null;
            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}",
                onRequest: req => captured = req);
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            await transport.SendBatchAsync();

            StringAssert.StartsWith(Constants.SandboxBaseUrl, captured.RequestUri.ToString());
        }

        [Test]
        public async Task SendBatchAsync_200_UsesCorrectUrlForProdKey()
        {
            _store.Write("{\"type\":\"track\"}");

            HttpRequestMessage captured = null;
            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}",
                onRequest: req => captured = req);
            using var transport = new HttpTransport(_store, "pk_imapik-prodkey", handler: handler);

            await transport.SendBatchAsync();

            StringAssert.StartsWith(Constants.ProductionBaseUrl, captured.RequestUri.ToString());
        }

        [Test]
        public async Task SendBatchAsync_EmptyQueue_ReturnsFalse()
        {
            var handler = new MockHandler(HttpStatusCode.OK, "{}");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            var sent = await transport.SendBatchAsync();

            Assert.IsFalse(sent);
            Assert.AreEqual(0, handler.CallCount, "should not make HTTP call when queue is empty");
        }

        [Test]
        public async Task SendBatchAsync_4xx_DeletesFilesAndResetsBackoff()
        {
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(HttpStatusCode.BadRequest, "");
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual(0, _store.Count(), "4xx should delete files — won't succeed on retry");
            Assert.IsFalse(transport.IsInBackoffWindow);
            Assert.IsNotNull(reportedError);
            Assert.AreEqual(AudienceErrorCode.ValidationRejected, reportedError.Code);
        }

        [Test]
        public async Task SendBatchAsync_200_WithRejected_DeletesFilesAndSurfacesValidationRejected()
        {
            // Per Unity Implementation Plan §4.6, a 200 with rejected>0 means
            // per-message validation errors. The batch is deleted (retries
            // would not help) and the count is surfaced via onError so
            // studios can observe silently dropped events.
            _store.Write("{\"type\":\"track\",\"eventName\":\"a\"}");
            _store.Write("{\"type\":\"track\",\"eventName\":\"b\"}");

            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":1}");
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual(0, _store.Count(), "200 with rejected>0 should still delete the batch");
            Assert.IsNotNull(reportedError, "partial rejection must surface via onError");
            Assert.AreEqual(AudienceErrorCode.ValidationRejected, reportedError.Code);
            StringAssert.Contains("1", reportedError.Message, "message should include the rejected count");
        }

        [Test]
        public async Task SendBatchAsync_200_ZeroRejected_DoesNotFireOnError()
        {
            _store.Write("{\"type\":\"track\",\"eventName\":\"a\"}");

            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}");
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.IsNull(reportedError, "zero rejected must not fire onError");
        }

        [Test]
        public async Task SendBatchAsync_200_MalformedBody_TreatsAsZeroRejected()
        {
            // Malformed diagnostic body must not block the success path.
            _store.Write("{\"type\":\"track\",\"eventName\":\"a\"}");

            var handler = new MockHandler(HttpStatusCode.OK, "not-json");
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual(0, _store.Count(), "files should still be deleted on 200");
            Assert.IsNull(reportedError, "malformed body must not surface an error");
        }

        [Test]
        public async Task SendBatchAsync_5xx_KeepsFilesAndIncreasesBackoff()
        {
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(HttpStatusCode.InternalServerError, "");
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual(1, _store.Count(), "5xx should keep files for retry");
            Assert.IsTrue(transport.IsInBackoffWindow);
            Assert.AreEqual(5000, transport.BackoffMs, "first failure = 5s backoff");
            Assert.IsNotNull(reportedError);
            Assert.AreEqual(AudienceErrorCode.FlushFailed, reportedError.Code);
        }

        [Test]
        public async Task BackoffMs_EscalatesOnlyAfterWindowElapsed()
        {
            _store.Write("{\"type\":\"track\"}");
            var handler = new MockHandler(HttpStatusCode.InternalServerError, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                handler: handler, getUtcNow: _getUtcNow);

            // Schedule: 5s → 10s → 20s → 40s → 60s cap.
            // Each escalation requires the previous window to have elapsed.
            await transport.SendBatchAsync();
            Assert.AreEqual(5_000, transport.BackoffMs);

            Advance(5_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(10_000, transport.BackoffMs);

            Advance(10_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(20_000, transport.BackoffMs);

            Advance(20_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(40_000, transport.BackoffMs);

            Advance(40_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(60_000, transport.BackoffMs, "reaches 60s cap after 40s step");

            Advance(60_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(60_000, transport.BackoffMs, "stays at cap");
        }

        [Test]
        public async Task BackoffMs_DoesNotEscalateWhileInsidePreviousWindow()
        {
            _store.Write("{\"type\":\"track\"}");
            var handler = new MockHandler(HttpStatusCode.InternalServerError, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                handler: handler, getUtcNow: _getUtcNow);

            await transport.SendBatchAsync();
            Assert.AreEqual(5_000, transport.BackoffMs);
            var firstDeadline = transport.NextAttemptAt;
            Assert.IsNotNull(firstDeadline);

            // Caller ignores the window and retries immediately. Must not escalate.
            Advance(100);
            await transport.SendBatchAsync();
            Assert.AreEqual(5_000, transport.BackoffMs,
                "failures inside the previous window must not escalate backoff");
            Assert.AreEqual(firstDeadline, transport.NextAttemptAt,
                "NextAttemptAt should not move when the window hasn't elapsed");

            // Another premature retry — still no escalation.
            Advance(3_000);
            await transport.SendBatchAsync();
            Assert.AreEqual(5_000, transport.BackoffMs);

            // Wait out the window, fail again → now we escalate.
            _utcNow = firstDeadline.Value.AddMilliseconds(1);
            await transport.SendBatchAsync();
            Assert.AreEqual(10_000, transport.BackoffMs);
        }

        [Test]
        public async Task BackoffMs_ResetsAfterSuccess()
        {
            _store.Write("{\"type\":\"track\"}");

            var callCount = 0;
            var handler = new MockHandler(() =>
            {
                callCount++;
                // Fail twice, then succeed.
                return callCount <= 2
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent("{\"accepted\":1,\"rejected\":0}") };
            });
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                handler: handler, getUtcNow: _getUtcNow);

            await transport.SendBatchAsync();
            Assert.AreEqual(5_000, transport.BackoffMs);

            Advance(5_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(10_000, transport.BackoffMs);

            Advance(10_001);
            await transport.SendBatchAsync();
            Assert.AreEqual(0, transport.BackoffMs, "backoff resets after success");
            Assert.IsFalse(transport.IsInBackoffWindow);
        }

        [Test]
        public async Task SendBatchAsync_NetworkError_KeepsFilesAndBacksOff()
        {
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(() => throw new HttpRequestException("connection refused"));
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual(1, _store.Count(), "network error should keep files for retry");
            Assert.IsTrue(transport.IsInBackoffWindow);
            Assert.IsNotNull(reportedError);
            Assert.AreEqual(AudienceErrorCode.NetworkError, reportedError.Code);
        }

        [Test]
        public async Task SendBatchAsync_HttpClientTimeout_TreatedAsNetworkError()
        {
            // Regression guard: HttpClient.Timeout throws TaskCanceledException, which
            // derives from OperationCanceledException. Without a `when (ct.IsCancellationRequested)`
            // guard, timeouts would be silently swallowed as "shutdown" — no backoff, no error
            // callback, next cycle hot-loops. This test ensures timeouts flow through the
            // NetworkError path.
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(() => throw new TaskCanceledException("Request timed out"));
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            // Pass default CancellationToken so ct.IsCancellationRequested is false — this
            // simulates a real HttpClient timeout (not a caller-initiated cancellation).
            await transport.SendBatchAsync();

            Assert.AreEqual(1, _store.Count(), "timeout should keep files for retry");
            Assert.IsTrue(transport.IsInBackoffWindow, "timeout must increment failures and engage backoff");
            Assert.IsNotNull(reportedError, "NetworkError callback must fire on timeout");
            Assert.AreEqual(AudienceErrorCode.NetworkError, reportedError.Code);
        }

        [Test]
        public void SendBatchAsync_CallerCancelled_Throws_DoesNotDeleteOrRecordFailure()
        {
            // Regression guard for PR #701 review: caller cancellation must
            // propagate. If the `when (ct.IsCancellationRequested)` branch
            // swallowed the exception, SendBatchAsync would return `true`
            // with the batch still on disk, and a FlushAsync loop watching
            // that return value would re-enter on the same cancelled token
            // forever — nothing ever drains, nothing ever throws.
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(() => throw new OperationCanceledException("simulated"));
            AudienceError reportedError = null;
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: e => reportedError = e, handler: handler);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // CatchAsync (not ThrowsAsync) because HttpClient re-wraps our mock's
            // OperationCanceledException as TaskCanceledException before rethrowing.
            // We want to assert the cancellation *family*, not a specific subclass.
            Assert.CatchAsync<OperationCanceledException>(
                async () => await transport.SendBatchAsync(cts.Token));

            Assert.AreEqual(1, _store.Count(), "cancelled send must not delete the batch");
            Assert.IsFalse(transport.IsInBackoffWindow, "cancel is not a failure — no backoff engaged");
            Assert.IsNull(reportedError, "cancel is caller-initiated — no onError fires");
        }

        [Test]
        public async Task IsInBackoffWindow_ClearsAfterNextAttemptAtElapses()
        {
            _store.Write("{\"type\":\"track\"}");

            var now = new DateTime(2026, 4, 17, 12, 0, 0, DateTimeKind.Utc);
            var handler = new MockHandler(HttpStatusCode.InternalServerError, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                handler: handler, getUtcNow: () => now);

            await transport.SendBatchAsync();

            Assert.IsTrue(transport.IsInBackoffWindow, "within window immediately after failure");
            Assert.AreEqual(now.AddMilliseconds(5_000), transport.NextAttemptAt);

            // Advance the clock just before NextAttemptAt — still backing off.
            now = now.AddMilliseconds(4_999);
            Assert.IsTrue(transport.IsInBackoffWindow);

            // Advance past NextAttemptAt — window closed, next send may proceed.
            now = now.AddMilliseconds(2);
            Assert.IsFalse(transport.IsInBackoffWindow, "window closes at NextAttemptAt");
        }

        [Test]
        public async Task SendBatchAsync_ErrorCallbackThrows_DoesNotCrash()
        {
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(HttpStatusCode.BadRequest, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: _ => throw new InvalidOperationException("callback bug"),
                handler: handler);

            await transport.SendBatchAsync();
        }

#if IMMUTABLE_AUDIENCE_GZIP
        private static string DecompressGzip(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }
#endif

        // Minimal HttpMessageHandler that returns a canned response.
        // Optionally captures the request for inspection.
        private class MockHandler : HttpMessageHandler
        {
            private readonly Func<HttpResponseMessage> _factory;
            private readonly Action<HttpRequestMessage> _onRequest;

            internal int CallCount { get; private set; }

            internal MockHandler(HttpStatusCode status, string body, Action<HttpRequestMessage> onRequest = null)
            {
                _factory = () => new HttpResponseMessage(status)
                {
                    Content = new StringContent(body)
                };
                _onRequest = onRequest;
            }

            internal MockHandler(Func<HttpResponseMessage> factory, Action<HttpRequestMessage> onRequest = null)
            {
                _factory = factory;
                _onRequest = onRequest;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                CallCount++;
                _onRequest?.Invoke(request);
                return Task.FromResult(_factory());
            }
        }
    }
}