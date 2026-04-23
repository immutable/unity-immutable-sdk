#nullable enable

using System;

namespace Immutable.Audience
{
    // Configuration passed to ImmutableAudience.Init.
    public class AudienceConfig
    {
        // Studio API key. Required — Init throws if null.
        public string? PublishableKey { get; set; }

        // Target backend environment. Defaults to Sandbox so a
        // misconfigured integration cannot accidentally send production
        // traffic — pin to Production explicitly when shipping to live
        // players. Set Dev to hit Immutable's internal backend (engineers
        // only).
        public AudienceEnvironment Environment { get; set; } = AudienceEnvironment.Sandbox;

        // Initial consent level.
        public ConsentLevel Consent { get; set; } = ConsentLevel.None;

        // Distribution platform the game is running on.
        public string? DistributionPlatform { get; set; }

        // Enable debug logging.
        public bool Debug { get; set; } = false;

        // How often pending events are flushed to the backend.
        public int FlushIntervalSeconds { get; set; } = Constants.DefaultFlushIntervalSeconds;

        // Flush as soon as this many events are queued.
        public int FlushSize { get; set; } = Constants.DefaultFlushSize;

        // Optional error callback.
        public Action<AudienceError>? OnError { get; set; }

        // Directory the SDK uses for identity, consent, and queued events.
        // Unity hooks populate this from Application.persistentDataPath.
        public string? PersistentDataPath { get; set; }

        // Library version sent on every message.
        public string PackageVersion { get; set; } = Constants.LibraryVersion;

        // Maximum time Shutdown waits for the final flush.
        public int ShutdownFlushTimeoutMs { get; set; } = 2_000;

        // Test seam for HttpTransport; not part of the public API.
        internal System.Net.Http.HttpMessageHandler? HttpHandler { get; set; }
    }
}
