using System.Collections.Generic;

namespace Immutable.Audience
{
    // Typed event contract for ImmutableAudience.Track(IEvent).
    public interface IEvent
    {
        string EventName { get; }
        Dictionary<string, object> ToProperties();
    }
}