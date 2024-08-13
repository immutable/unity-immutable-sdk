namespace Immutable.Passport.Core.Logging
{
    /// <summary>
    /// Defines the logging levels used within the SDK to categorise the severity of log messages.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs detailed information for debugging the SDK
        /// </summary>
        Debug,

        /// <summary>
        /// Logs general information about SDK operations
        /// </summary>
        Info,

        /// <summary>
        /// Logs warnings about potential issues or unexpected behaviour
        /// </summary>
        Warn,

        /// <summary>
        /// Logs errors indicating failures in SDK operations
        /// </summary>
        Error
    }
}