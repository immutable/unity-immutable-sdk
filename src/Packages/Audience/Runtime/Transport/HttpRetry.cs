#nullable enable

using System;
using System.Net.Http;

namespace Immutable.Audience
{
    internal static class HttpRetry
    {
        // Past HTTP-date returns null so callers fall through to their default backoff.
        internal static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
        {
            var ra = response.Headers.RetryAfter;
            if (ra == null) return null;
            if (ra.Delta.HasValue) return ra.Delta.Value;
            if (ra.Date.HasValue)
            {
                var d = ra.Date.Value - DateTimeOffset.UtcNow;
                return d > TimeSpan.Zero ? d : (TimeSpan?)null;
            }
            return null;
        }
    }
}
