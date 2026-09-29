#nullable enable

namespace Immutable.Audience
{
    /// <summary>
    /// Exception-capture settings passed to <see cref="ImmutableAudience.Init"/>
    /// via <see cref="AudienceConfig.ErrorTracking"/>.
    /// </summary>
    public class ErrorTrackingConfig
    {
        /// <summary>
        /// Opts into sending uncaught C# exceptions as <c>exception_captured</c>
        /// events. Default <c>false</c>.
        /// </summary>
        /// <remarks>
        /// Managed exceptions only. Native crashes (segfaults, ANRs) are not
        /// covered. Capture is capped per run so a repeating exception can't
        /// flood the event queue.
        /// </remarks>
        public bool CaptureExceptions { get; set; } = false;
    }
}
