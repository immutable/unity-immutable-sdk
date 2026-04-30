using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class PublishableKeyPrefixTests
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
            ImmutableAudience.LaunchContextProvider = null;
            ImmutableAudience.ContextProvider = null;
            ImmutableAudience.DefaultPersistentDataPathProvider = null;
            Identity.Reset(_testDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        // Fake fixtures on purpose: real keys would be a committed secret.
        private const string TestPrefixKey = "pk_imapik-test-fixture";
        private const string NonTestKey = "pk_imapik-fixture";
        private static readonly string ProductionUrl = Constants.ProductionBaseUrl;
        private static readonly string SandboxUrl = Constants.SandboxBaseUrl;

        private class UnauthorizedHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\":\"unknown publishable key\"}")
                });
        }

        private AudienceConfig MakeConfig(string publishableKey, string baseUrl = null) =>
            new AudienceConfig
            {
                PublishableKey = publishableKey,
                BaseUrl = baseUrl,
                Consent = ConsentLevel.Anonymous,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = TestDefaults.FlushIntervalSeconds,
                FlushSize = TestDefaults.FlushSize,
                HttpHandler = new UnauthorizedHandler()
            };

        // -----------------------------------------------------------------
        // Init-time mismatch warning
        // -----------------------------------------------------------------

        [Test]
        public void Init_TestKeyAgainstProductionUrl_LogsMismatchWarning()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig(TestPrefixKey, ProductionUrl));

                Assert.That(lines, Has.Some.Contains("test prefix").And.Contains("production"),
                    "Init must warn when a test-prefix key is paired with the production BaseUrl");
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Init_NonTestKeyAgainstSandboxUrl_LogsMismatchWarning()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig(NonTestKey, SandboxUrl));

                Assert.That(lines, Has.Some.Contains("not a test key").And.Contains("sandbox"),
                    "Init must warn when a non-test key is paired with the sandbox BaseUrl");
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Init_TestKeyAgainstSandboxUrl_DoesNotWarn()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig(TestPrefixKey, SandboxUrl));

                Assert.That(lines.Where(l => l.Contains("BaseUrl")), Is.Empty,
                    "test-key + sandbox-URL is the canonical pairing; no warning expected");
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Init_TestKeyAgainstCustomDevUrl_DoesNotWarn()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                // Fake fixture URL on purpose.
                ImmutableAudience.Init(MakeConfig(TestPrefixKey, "https://api.dev.example.com"));

                Assert.That(lines.Where(l => l.Contains("BaseUrl")), Is.Empty,
                    "custom BaseUrl must not be flagged as a mismatch");
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Init_NoBaseUrlOverride_DoesNotWarn()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig(TestPrefixKey, baseUrl: null));

                Assert.That(lines.Where(l => l.Contains("BaseUrl")), Is.Empty,
                    "no override means no mismatch; no warning expected");
            }
            finally { Log.Writer = null; }
        }

        [Test]
        public void Init_TrailingSlashOnProductionUrl_StillDetectsMismatch()
        {
            var lines = new List<string>();
            Log.Writer = lines.Add;
            try
            {
                ImmutableAudience.Init(MakeConfig(TestPrefixKey, ProductionUrl + "/"));

                Assert.That(lines, Has.Some.Contains("test prefix").And.Contains("production"),
                    "trailing slash on the production URL must not bypass the mismatch check");
            }
            finally { Log.Writer = null; }
        }

        // -----------------------------------------------------------------
        // Runtime: backend 401 surfaces via OnError
        // -----------------------------------------------------------------

        [Test]
        public async Task Track_BackendReturns401_SurfacesValidationRejected()
        {
            var errors = new ConcurrentBag<AudienceError>();
            var config = MakeConfig(TestPrefixKey, ProductionUrl);
            config.OnError = errors.Add;

            ImmutableAudience.Init(config);
            ImmutableAudience.Track("event_against_prod_with_test_key");
            await ImmutableAudience.FlushAsync();

            Assert.IsTrue(errors.Any(e => e.Code == AudienceErrorCode.ValidationRejected),
                $"401 from backend must surface as ValidationRejected via OnError; observed {errors.Count} error(s)");
            Assert.IsTrue(errors.Any(e => e.Message.Contains("401")),
                "OnError message should include the 401 status code so the studio can correlate with backend logs");
            Assert.IsTrue(errors.Any(e => e.Message.Contains("unknown publishable key")),
                "OnError should propagate the backend's error body so studios can see why");
        }
    }
}
