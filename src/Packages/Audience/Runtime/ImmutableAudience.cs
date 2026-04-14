using System;

namespace Immutable.Audience
{
    /// <summary>
    /// Entry point for the Immutable Audience SDK.
    /// Call <see cref="Init"/> once on startup, then use the static methods from any thread.
    /// </summary>
    public static class ImmutableAudience
    {
        // Scaffold only -- implementation follows in subsequent sub-issues (see SDK-99).

        /// <summary>
        /// Raised when the SDK encounters an error (e.g. a failed flush or consent sync).
        /// Subscribe before calling <see cref="Init"/>.
        /// </summary>
        public static event Action<AudienceError>? OnError;

        /// <summary>Initialise the SDK. Call once, typically in your game's entry scene.</summary>
        public static void Init(AudienceConfig config)
        {
            throw new System.NotImplementedException(
                "Immutable Audience SDK: implementation pending. See SDK-99.");
        }
    }
}
