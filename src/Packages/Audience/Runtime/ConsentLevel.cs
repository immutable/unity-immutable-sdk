namespace Immutable.Audience
{
    /// <summary>Controls what the Audience SDK tracks.</summary>
    public enum ConsentLevel
    {
        /// <summary>SDK inert. No events queued or sent. No IDs persisted to disk.</summary>
        None,
        /// <summary>Track events with anonymousId only. Identify/Alias discarded with warning.</summary>
        Anonymous,
        /// <summary>All events. Identify/Alias send. userId attached to track events.</summary>
        Full
    }
}
