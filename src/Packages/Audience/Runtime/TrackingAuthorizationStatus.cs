#nullable enable

namespace Immutable.Audience
{
    /// <summary>
    /// Result of <see cref="ImmutableAudience.RequestTrackingAuthorizationAsync"/>.
    /// </summary>
    /// <remarks>
    /// Maps directly to Apple's
    /// <c>ATTrackingManagerAuthorizationStatus</c>. On platforms other than
    /// iOS, or on iOS &lt; 14, the request resolves to
    /// <see cref="NotDetermined"/>.
    /// </remarks>
    public enum TrackingAuthorizationStatus
    {
        /// <summary>The user has not yet been prompted, or the prompt was dismissed without a choice.</summary>
        NotDetermined = 0,

        /// <summary>Tracking is restricted by a profile or parental control. The system prompt is not shown.</summary>
        Restricted = 1,

        /// <summary>The user denied tracking. IDFA is unavailable.</summary>
        Denied = 2,

        /// <summary>The user authorized tracking. IDFA is available.</summary>
        Authorized = 3,
    }
}
