using System;
using Immutable.Passport.Core.Logging;

namespace Immutable.Passport.Helpers
{
    public static class UriExtensions
    {
        /// <summary>
        /// Gets the specified query parameter from the given URI
        /// </summary> 
        public static string? GetQueryParameter(this Uri uri, string key)
        {
            try
            {
                var query = uri.Query;
                var queryParameters = query.Split(new char[] { '?', '&' });
                foreach (var queryParameter in queryParameters)
                {
                    string[] keyValue = queryParameter.Split('=');
                    if (keyValue[0] == key)
                    {
                        return keyValue[1];
                    }
                }
            }
            catch (Exception e)
            {
                PassportLogger.Debug($"Failed to get query parameter {key}: {e.Message}");
            }
            return null;
        }
    }
}