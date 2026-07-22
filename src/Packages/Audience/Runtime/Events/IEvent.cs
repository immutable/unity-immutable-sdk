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
    /// Implementations validate required fields inside <see cref="ToProperties"/>.
    /// <see cref="ImmutableAudience.Track(IEvent)"/> catches the throw and warns
    /// for a consumer-authored <see cref="IEvent"/>; see <see cref="IBuiltInEvent"/>
    /// for the exception to that.
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

    /// <summary>
    /// Marks an <see cref="IEvent"/> implementation as one of Immutable's own
    /// built-in event types (<see cref="Purchase"/>, <see cref="Progression"/>,
    /// <see cref="Resource"/>, <see cref="AchievementUnlocked"/>,
    /// <see cref="MilestoneReached"/>). Not implementable outside this
    /// assembly: it exists only so <see cref="ImmutableAudience.Track(IEvent)"/>
    /// can tell a validation failure in one of these apart from an exception
    /// thrown by a consumer's own custom <see cref="IEvent"/>.
    /// </summary>
    internal interface IBuiltInEvent : IEvent
    {
    }
}
