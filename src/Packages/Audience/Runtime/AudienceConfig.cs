using System;

namespace Immutable.Audience
{
    /// <summary>Configuration passed to <see cref="ImmutableAudience.Init"/>.</summary>
    public class AudienceConfig
    {
        public string PublishableKey { get; set; }
        public ConsentLevel Consent { get; set; } = ConsentLevel.None;
        public string DistributionPlatform { get; set; }
        public bool Debug { get; set; } = false;
        public int FlushIntervalSeconds { get; set; } = Constants.DefaultFlushIntervalSeconds;
        public int FlushSize { get; set; } = Constants.DefaultFlushSize;
        public Action<AudienceError> OnError { get; set; }
    }
}
