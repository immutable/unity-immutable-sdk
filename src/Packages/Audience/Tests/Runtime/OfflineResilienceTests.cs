using System.Collections.Concurrent;
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
    internal class OfflineResilienceTests
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
            // Restore queue dir (a test may have left it as a file).
            var queueDir = AudiencePaths.QueueDir(_testDir);
            if (File.Exists(queueDir)) File.Delete(queueDir);
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }

        private class KeepOnDiskHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        private AudienceConfig MakeConfig() => new AudienceConfig
        {
            PublishableKey = "pk_imapik-test-key",
            Consent = ConsentLevel.Anonymous,
            PersistentDataPath = _testDir,
            FlushIntervalSeconds = TestDefaults.FlushIntervalSeconds,
            FlushSize = TestDefaults.FlushSize,
            HttpHandler = new KeepOnDiskHandler()
        };

        // Cross-platform alternative to chmod/quota/admin: writes inside
        // a file (not a directory) fail.
        private void BlockDiskWrites()
        {
            var queueDir = AudiencePaths.QueueDir(_testDir);
            // Drain pre-block events so they don't pollute the assertion.
            ImmutableAudience.FlushQueueToDiskForTesting();
            if (Directory.Exists(queueDir))
            {
                foreach (var f in Directory.GetFiles(queueDir, AudiencePaths.QueueGlob)) File.Delete(f);
                Directory.Delete(queueDir);
            }
            File.WriteAllText(queueDir, "blocker");
        }

        // -----------------------------------------------------------------
        // Current-behaviour regression: blocked disk does not crash the SDK
        // -----------------------------------------------------------------

        [Test]
        public void Track_DiskWritesBlocked_DoesNotThrowToCallers()
        {
            ImmutableAudience.Init(MakeConfig());
            BlockDiskWrites();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 50; i++) ImmutableAudience.Track($"blocked_{i}");
                ImmutableAudience.FlushQueueToDiskForTesting();
            }, "Track must not propagate disk-write IOException to callers");

            Assert.IsTrue(ImmutableAudience.Initialized,
                "SDK should remain initialised after a sustained disk-write failure");
        }

        [Test]
        public void Shutdown_DiskWritesBlocked_DoesNotThrow()
        {
            // Shutdown is invoked from app-quit handlers; an exception would
            // crash the process.
            ImmutableAudience.Init(MakeConfig());
            ImmutableAudience.Track("event_pre_block");
            BlockDiskWrites();
            for (int i = 0; i < 20; i++) ImmutableAudience.Track($"blocked_{i}");

            Assert.DoesNotThrow(() => ImmutableAudience.Shutdown(),
                "Shutdown must absorb disk-write failure during the final drain");
        }

        [Test]
        [Ignore("Target behaviour: memory-only fallback without losing in-flight events. " +
                "EventQueue currently drops on IOException; remove [Ignore] once it retains.")]
        public void Track_DiskWritesBlocked_RetainsEventsInMemory_AndSurfacesOnError()
        {
            var errors = new ConcurrentBag<AudienceError>();
            var config = MakeConfig();
            config.OnError = errors.Add;

            ImmutableAudience.Init(config);
            BlockDiskWrites();

            const int eventCount = 50;
            for (int i = 0; i < eventCount; i++) ImmutableAudience.Track($"blocked_{i}");
            ImmutableAudience.FlushQueueToDiskForTesting();

            Assert.GreaterOrEqual(ImmutableAudience.QueueSize, eventCount,
                $"events must be retained when disk writes fail; QueueSize={ImmutableAudience.QueueSize}, expected >= {eventCount}");

            Assert.IsTrue(errors.Any(),
                "OnError must fire at least once when the drain thread cannot reach disk");
        }
    }
}
