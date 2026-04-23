#nullable enable

using System;

namespace Immutable.Audience
{
    // Configuration passed to ImmutableAudience.Init.
    public class AudienceConfig
    {
        // Studio API key. Required — Init throws if null.
        public string? PublishableKey { get; set; }

        // Target backend environment. Default Auto picks sandbox for
        // pk_imapik-test- keys and production for everything else, so
        // most integrations leave this alone. Set Dev to hit Immutable's
        // internal development backend, or Sandbox/Production to override
        // the key-based default (e.g. staging a test key against prod
        // infra during a regression sweep).
        public AudienceEnvironment Environment { get; set; } = AudienceEnvironment.Auto;

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
