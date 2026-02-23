using System;
using Immutable.Passport.Core.Logging;
using Immutable.Passport.Model;

namespace Immutable.Passport.Core
{
    /// <summary>
    /// Maps game bridge error responses into typed Passport exceptions.
    /// </summary>
    internal static class BrowserResponseErrorMapper
    {
        private const string TAG = "[Browser Response Error Mapper]";

        /// <summary>
        /// Converts a failed BrowserResponse into the appropriate PassportException.
        /// </summary>
        internal static PassportException MapToException(BrowserResponse response)
        {
            try
            {
                if (!string.IsNullOrEmpty(response.error) && !string.IsNullOrEmpty(response.errorType))
                {
                    var type = (PassportErrorType)Enum.Parse(typeof(PassportErrorType), response.errorType);
                    return new PassportException(response.error, type);
                }

                return new PassportException(!string.IsNullOrEmpty(response.error) ? response.error : "Unknown error");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Parse passport type error: {ex.Message}");
            }

            return new PassportException(response.error ?? "Failed to parse error");
        }
    }
}
