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
            Assert.AreEqual("full", body["status"]);
            Assert.AreEqual(Constants.ConsentSource, body["source"]);
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

            Assert.AreEqual("none", body["status"]);
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

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken ct)
            {
                if (request.Method == HttpMethod.Put)
                {
                    LastPut = new CapturedRequest
                    {
                        Url = request.RequestUri!.ToString(),
                        Body = request.Content != null
                            ? await request.Content.ReadAsStringAsync().ConfigureAwait(false)
                            : null,
                    };
                    PutReceived.Set();
                }
                return new HttpResponseMessage(Status);
            }
        }
    }
}
