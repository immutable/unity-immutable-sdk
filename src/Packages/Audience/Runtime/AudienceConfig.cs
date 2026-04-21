namespace Immutable.Audience
{
    // Configuration passed to ImmutableAudience.Init.
    public class AudienceConfig
    {
        // Studio API key.
        public string PublishableKey { get; set; }

        // Initial consent level.
        public ConsentLevel Consent { get; set; } = ConsentLevel.None;

        // Distribution platform the game is running on.
        public string DistributionPlatform { get; set; }

        // Enable debug logging.
        public bool Debug { get; set; } = false;

        // How often pending events are flushed to the backend.
        public int FlushIntervalSeconds { get; set; } = Constants.DefaultFlushIntervalSeconds;

        // Flush as soon as this many events are queued.
        public int FlushSize { get; set; } = Constants.DefaultFlushSize;
    }
}
