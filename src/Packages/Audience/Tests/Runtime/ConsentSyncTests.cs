using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ConsentSyncTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            ImmutableAudience.ResetState();
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        [Test]
        public void SetConsent_FiresPut_WithExpectedBodyShape()
        {
            var handler = new CapturingHandler();
            var config = MakeConfig(handler, ConsentLevel.Anonymous);
            ImmutableAudience.Init(config);

            ImmutableAudience.SetConsent(ConsentLevel.Full);

            var put = WaitForPut(handler);
            var body = JsonReader.DeserializeObject(put.Body);

            Assert.AreEqual(Constants.ConsentUrl("pk_imapik-test-key1"), put.Url);
            Assert.AreEqual(ConsentLevel.Full.ToLowercaseString(), body[ConsentBodyFields.Status]);
            Assert.AreEqual(Constants.ConsentSource, body[ConsentBodyFields.Source]);
            Assert.IsTrue(body.ContainsKey("anonymousId"));
            Assert.IsNotNull(body["anonymousId"], "upgrade PUT must carry the current anonymousId");
        }

        [Test]
        public void SetConsent_None_PutCarriesOldAnonymousId_AfterReset()
        {
            // Regression guard: Identity.Reset runs before SyncConsentToBackend,
            // so the PUT must have captured the anonymousId beforehand.
            var handler = new CapturingHandler();
            var config = MakeConfig(handler, ConsentLevel.Anonymous);
            ImmutableAudience.Init(config);

            var seeded = Identity.Get(_testDir);
            Assert.IsNotNull(seeded, "Init under Anonymous should have minted an anonymousId");

            ImmutableAudience.SetConsent(ConsentLevel.None);

            var put = WaitForPut(handler);
            var body = JsonReader.DeserializeObject(put.Body);

            Assert.AreEqual(ConsentLevel.None.ToLowercaseString(), body[ConsentBodyFields.Status]);
            Assert.AreEqual(seeded, body["anonymousId"],
                "revocation PUT must carry the id that was revoked, not null");
            Assert.IsFalse(File.Exists(AudiencePaths.IdentityFile(_testDir)),
                "precondition: Identity.Reset ran");
        }

        [Test]
        public void SetConsent_PutFailure_InvokesOnErrorWithConsentSyncFailed()
        {
            var handler = new CapturingHandler { Status = HttpStatusCode.InternalServerError };
            var received = new ManualResetEventSlim(false);
            AudienceError captured = null;

            var config = MakeConfig(handler, ConsentLevel.Anonymous);
            config.OnError = err =>
            {
                if (err.Code == AudienceErrorCode.ConsentSyncFailed)
                {
                    captured = err;
                    received.Set();
                }
            };
            ImmutableAudience.Init(config);

            ImmutableAudience.SetConsent(ConsentLevel.Full);

            Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)),
                "OnError(ConsentSyncFailed) should fire on non-2xx");
            StringAssert.Contains("500", captured.Message);
        }

        [Test]
        public void SetConsent_429ThenSuccess_DoesNotFireConsentSyncFailed()
        {
            var handler = new CapturingHandler
            {
                StatusSequence = new[] { (HttpStatusCode)429, HttpStatusCode.NoContent },
                RetryAfterSeconds = 0,
            };
            AudienceError captured = null;
            var received = new ManualResetEventSlim(false);

            var config = MakeConfig(handler, ConsentLevel.Anonymous);
            config.OnError = err =>
            {
                if (err.Code == AudienceErrorCode.ConsentSyncFailed)
                {
                    captured = err;
                    received.Set();
                }
            };
            ImmutableAudience.Init(config);

            ImmutableAudience.SetConsent(ConsentLevel.Full);

            // Wait long enough for both attempts (Retry-After: 0).
            Assert.IsFalse(received.Wait(TimeSpan.FromSeconds(3)),
                "transient 429 followed by 2xx must not surface ConsentSyncFailed");
            Assert.IsNull(captured);
            Assert.GreaterOrEqual(handler.PutCount, 2,
                "429 must trigger at least one retry");
        }

        [Test]
        public void SetConsent_429Repeated_FiresConsentSyncFailedAfterRetries()
        {
            // RetryAfterSeconds=0 collapses the 1s/2s/4s production cadence
            // so the test runs in milliseconds.
            var handler = new CapturingHandler
            {
                Status = (HttpStatusCode)429,
                RetryAfterSeconds = 0,
            };
            AudienceError captured = null;
            var received = new ManualResetEventSlim(false);

            var config = MakeConfig(handler, ConsentLevel.Anonymous);
            config.OnError = err =>
            {
                if (err.Code == AudienceErrorCode.ConsentSyncFailed)
                {
                    captured = err;
                    received.Set();
                }
            };
            ImmutableAudience.Init(config);

            ImmutableAudience.SetConsent(ConsentLevel.Full);

            Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)),
                "exhausted 429 retries must surface ConsentSyncFailed");
            StringAssert.Contains("429", captured.Message);
            Assert.AreEqual(4, handler.PutCount, "must have made the full 4 attempts");
        }

        private AudienceConfig MakeConfig(CapturingHandler handler, ConsentLevel consent) =>
            new AudienceConfig
            {
                PublishableKey = "pk_imapik-test-key1",
                Consent = consent,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = 600,
                FlushSize = 1000,
                HttpHandler = handler,
            };

        private static CapturedRequest WaitForPut(CapturingHandler handler)
        {
            Assert.IsTrue(handler.PutReceived.Wait(TimeSpan.FromSeconds(5)),
                "consent PUT never fired");
            return handler.LastPut;
        }

        private class CapturedRequest
        {
            internal string Url;
            internal string Body;
        }

        private class CapturingHandler : HttpMessageHandler
        {
            internal readonly ManualResetEventSlim PutReceived = new ManualResetEventSlim(false);
            internal CapturedRequest LastPut;
            internal HttpStatusCode Status { get; set; } = HttpStatusCode.NoContent;

            // One status per call; falls back to Status once exhausted.
            internal HttpStatusCode[] StatusSequence { get; set; }

            // Adds Retry-After: <seconds> to 429 responses (0 = retry now).
            internal int? RetryAfterSeconds { get; set; }

            internal int PutCount { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken ct)
            {
                if (request.Method == HttpMethod.Put)
                {
                    PutCount++;
                    LastPut = new CapturedRequest
                    {
                        Url = request.RequestUri!.ToString(),
                        Body = request.Content != null
                            ? await request.Content.ReadAsStringAsync().ConfigureAwait(false)
                            : null,
                    };
                    PutReceived.Set();
                }

                var status = StatusSequence != null && PutCount - 1 < StatusSequence.Length
                    ? StatusSequence[PutCount - 1]
                    : Status;

                var response = new HttpResponseMessage(status);
                if ((int)status == 429 && RetryAfterSeconds.HasValue)
                {
                    response.Headers.Add("Retry-After", RetryAfterSeconds.Value.ToString());
                }
                return response;
            }
        }
    }
}
