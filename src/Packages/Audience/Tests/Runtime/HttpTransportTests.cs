using System;
using System.IO;
using System.IO.Compression;
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

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
            _store = new DiskStore(_testDir);
        }

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

        [Test]
        public async Task SendBatchAsync_200_SendsGzippedPayloadWithCorrectHeaders()
        {
            _store.Write("{\"type\":\"track\",\"eventName\":\"test\"}");

            byte[] capturedBody = null;
            string capturedKey = null;
            string capturedContentType = null;
            // Read body inside the callback — the request content is disposed after SendAsync returns.
            var handler = new MockHandler(HttpStatusCode.OK, "{\"accepted\":1,\"rejected\":0}",
                onRequest: req =>
                {
                    capturedKey = string.Join("", req.Headers.GetValues("x-immutable-publishable-key"));
                    capturedContentType = req.Content.Headers.ContentType.MediaType;
                    capturedBody = req.Content.ReadAsByteArrayAsync().Result;
                });
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            await transport.SendBatchAsync();

            Assert.AreEqual("pk_imapik-test-key1", capturedKey);
            Assert.AreEqual("application/json", capturedContentType);

            var decompressed = DecompressGzip(capturedBody);
            StringAssert.StartsWith("{\"batch\":[", decompressed);
            StringAssert.EndsWith("]}", decompressed);
            StringAssert.Contains("\"eventName\":\"test\"", decompressed);
        }

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
            Assert.IsFalse(transport.IsBackingOff);
            Assert.IsNotNull(reportedError);
            Assert.AreEqual(AudienceErrorCode.ValidationRejected, reportedError.Code);
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
            Assert.IsTrue(transport.IsBackingOff);
            Assert.AreEqual(5000, transport.BackoffMs, "first failure = 5s backoff");
            Assert.IsNotNull(reportedError);
            Assert.AreEqual(AudienceErrorCode.FlushFailed, reportedError.Code);
        }

        [Test]
        public async Task BackoffMs_ExponentialWithCap()
        {
            _store.Write("{\"type\":\"track\"}");
            var handler = new MockHandler(HttpStatusCode.InternalServerError, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            // Each SendBatch re-reads the same file (5xx doesn't delete it) and increments backoff.
            await transport.SendBatchAsync();
            Assert.AreEqual(5000, transport.BackoffMs);
            await transport.SendBatchAsync();
            Assert.AreEqual(10000, transport.BackoffMs);
            await transport.SendBatchAsync();
            Assert.AreEqual(20000, transport.BackoffMs);
            await transport.SendBatchAsync();
            Assert.AreEqual(40000, transport.BackoffMs);
            await transport.SendBatchAsync();
            Assert.AreEqual(60000, transport.BackoffMs, "capped at 60s");
            await transport.SendBatchAsync();
            Assert.AreEqual(60000, transport.BackoffMs, "stays at cap");
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
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1", handler: handler);

            await transport.SendBatchAsync();
            Assert.AreEqual(5000, transport.BackoffMs);

            await transport.SendBatchAsync();
            Assert.AreEqual(10000, transport.BackoffMs);

            await transport.SendBatchAsync();
            Assert.AreEqual(0, transport.BackoffMs, "backoff resets after success");
            Assert.IsFalse(transport.IsBackingOff);
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
            Assert.IsTrue(transport.IsBackingOff);
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
            Assert.IsTrue(transport.IsBackingOff, "timeout must increment failures and engage backoff");
            Assert.IsNotNull(reportedError, "NetworkError callback must fire on timeout");
            Assert.AreEqual(AudienceErrorCode.NetworkError, reportedError.Code);
        }

        [Test]
        public async Task SendBatchAsync_ErrorCallbackThrows_DoesNotCrash()
        {
            _store.Write("{\"type\":\"track\"}");

            var handler = new MockHandler(HttpStatusCode.BadRequest, "");
            using var transport = new HttpTransport(_store, "pk_imapik-test-key1",
                onError: _ => throw new InvalidOperationException("callback bug"),
                handler: handler);

            Assert.DoesNotThrowAsync(() => transport.SendBatchAsync());
        }

        private static string DecompressGzip(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Minimal HttpMessageHandler that returns a canned response.
        /// Optionally captures the request for inspection.
        /// </summary>
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