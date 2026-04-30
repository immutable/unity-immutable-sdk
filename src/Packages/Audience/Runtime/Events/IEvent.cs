#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    /// <summary>
    /// Typed event contract for
    /// <see cref="ImmutableAudience.Track(IEvent)"/>.
    /// </summary>
    /// <remarks>
    /// Implementations validate required fields inside
    /// <see cref="ToProperties"/>;
    /// <see cref="ImmutableAudience.Track(IEvent)"/> catches the throw and
    /// drops the event with a warning.
    /// </remarks>
    public interface IEvent
    {
        /// <summary>
        /// The event name sent (e.g. <c>"purchase"</c>, <c>"progression"</c>).
        /// </summary>
        string EventName { get; }

        /// <summary>
        /// Returns the event's properties as a dictionary, validating
        /// required fields.
        /// </summary>
        /// <returns>A fresh dictionary on every call.</returns>
        /// <exception cref="ArgumentException">
        /// A required field is missing or invalid.
        /// </exception>
        Dictionary<string, object> ToProperties();
    }
}
