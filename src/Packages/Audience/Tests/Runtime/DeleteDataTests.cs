using System;
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
    internal class DeleteDataTests
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

        private AudienceConfig MakeConfig(CapturingHandler handler, ConsentLevel consent = ConsentLevel.Full)
        {
            return new AudienceConfig
            {
                PublishableKey = TestDefaults.PublishableKey,
                Consent = consent,
                PersistentDataPath = _testDir,
                FlushIntervalSeconds = 600,
                FlushSize = 1000,
                HttpHandler = handler
            };
        }

        /// <summary>
        /// Records every request and returns a caller-configurable status.
        /// Signals when a request lands so tests can await the async Task.Run path.
        /// </summary>
        private class CapturingHandler : HttpMessageHandler
        {
            internal readonly List<HttpRequestMessage> Requests = new List<HttpRequestMessage>();
            internal readonly ManualResetEventSlim RequestSent = new ManualResetEventSlim(false);
            internal HttpStatusCode Status { get; set; } = HttpStatusCode.Accepted;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Requests.Add(request);
                RequestSent.Set();
                return Task.FromResult(new HttpResponseMessage(Status));
            }
        }

        private static void WaitForRequest(CapturingHandler handler)
        {
            Assert.IsTrue(handler.RequestSent.Wait(TimeSpan.FromSeconds(5)),
                "DeleteData's background HTTP call never fired");
        }

        [Test]
        public void DeleteData_WithUserId_FiresDelete_WithUserIdQuery()
        {
            var handler = new CapturingHandler();
            ImmutableAudience.Init(MakeConfig(handler));

            ImmutableAudience.DeleteData(userId: TestFixtures.PlayerCustomId);
            WaitForRequest(handler);

            // Filter out the game_launch POST from Init.
            HttpRequestMessage deleteRequest = null;
            foreach (var r in handler.Requests)
                if (r.Method == HttpMethod.Delete) { deleteRequest = r; break; }

            Assert.IsNotNull(deleteRequest, "expected a DELETE request");
            StringAssert.Contains(Constants.DataPath, deleteRequest.RequestUri!.ToString());
            StringAssert.Contains($"{MessageFields.UserId}=player-42", deleteRequest.RequestUri.Query);
            Assert.IsTrue(deleteRequest.Headers.Contains(Constants.PublishableKeyHeader),
                "publishable key header must be attached");
        }

        [Test]
        public void DeleteData_NoUserId_WithExistingAnonymousId_FiresDelete_WithAnonymousIdQuery()
        {
            // Seed an anonymousId as if the player had tracked in a prior session.
            var seeded = Identity.GetOrCreate(_testDir, ConsentLevel.Anonymous);
            Assert.IsNotNull(seeded);

            var handler = new CapturingHandler();
            ImmutableAudience.Init(MakeConfig(handler));

            ImmutableAudience.DeleteData();
            WaitForRequest(handler);

            HttpRequestMessage deleteRequest = null;
            foreach (var r in handler.Requests)
                if (r.Method == HttpMethod.Delete) { deleteRequest = r; break; }

            Assert.IsNotNull(deleteRequest);
            StringAssert.Contains($"{MessageFields.AnonymousId}={seeded}", deleteRequest.RequestUri!.Query);
        }

        [Test]
        public void DeleteData_NoUserId_NoAnonymousId_DoesNotFireRequest()
        {
            // Use Consent.None so Init's game_launch is suppressed: the only way
            // to guarantee no HTTP request fires at all.
            var handler = new CapturingHandler();
            ImmutableAudience.Init(MakeConfig(handler, ConsentLevel.None));

            Assert.IsFalse(handler.RequestSent.IsSet,
                "precondition: no request yet");

            ImmutableAudience.DeleteData();

            // Give any errant background task a moment to fire.
            Thread.Sleep(250);

            Assert.IsFalse(handler.RequestSent.IsSet,
                "no anonymousId and no userId must mean no request");
        }

        [Test]
        public void DeleteData_DoesNotCreateAnonymousIdFile()
        {
            var handler = new CapturingHandler();
            ImmutableAudience.Init(MakeConfig(handler, ConsentLevel.None));

            ImmutableAudience.DeleteData(userId: "some-user");
            // Even with a userId request, the anonymousId file must not materialise.
            Thread.Sleep(250);

            var identityPath = AudiencePaths.IdentityFile(_testDir);
            Assert.IsFalse(File.Exists(identityPath),
                "DeleteData must not create the anonymousId file as a side effect");
        }

        [Test]
        public async Task DeleteData_ReturnsTask_ThatCompletesAfterRequest()
        {
            var handler = new CapturingHandler();
            ImmutableAudience.Init(MakeConfig(handler));

            var task = ImmutableAudience.DeleteData(userId: TestFixtures.PlayerCustomId);
            Assert.IsNotNull(task, "DeleteData must return a non-null Task");

            // Await directly: no need for the RequestSent gate when the task
            // already represents completion.
            await task;

            Assert.IsTrue(handler.Requests.Any(r => r.Method == HttpMethod.Delete),
                "DELETE request must have been sent by the time the task completes");
        }

        [Test]
        public void DeleteData_BeforeInit_ReturnsCompletedTask()
        {
            // Not initialised: must not throw, must return a completed Task.
            ImmutableAudience.ResetState();

            var task = ImmutableAudience.DeleteData(userId: TestFixtures.PlayerCustomId);

            Assert.IsNotNull(task);
            Assert.IsTrue(task.IsCompleted, "DeleteData before Init must return an already-completed Task");
        }

        [Test]
        public void DeleteData_ServerError_InvokesOnError()
        {
            var handler = new CapturingHandler { Status = HttpStatusCode.InternalServerError };
            var received = new ManualResetEventSlim(false);
            AudienceError captured = null;

            var config = MakeConfig(handler);
            config.OnError = err =>
            {
                captured = err;
                received.Set();
            };
            ImmutableAudience.Init(config);

            ImmutableAudience.DeleteData(userId: TestFixtures.PlayerCustomId);

            Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)),
                "OnError should fire when DeleteData's response is non-2xx");
            Assert.AreEqual(AudienceErrorCode.NetworkError, captured.Code);
            StringAssert.Contains("500", captured.Message);
        }
    }
}
