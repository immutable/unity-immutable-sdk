using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    internal static class MessageBuilder
    {
        internal static Dictionary<string, object> Track(
            string eventName,
            string anonymousId,
            string userId,
            string packageVersion,
            Dictionary<string, object> properties = null)
        {
            var msg = BuildBase("track", packageVersion);
            msg["eventName"] = Truncate(eventName, 256);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, 256);

            if (!string.IsNullOrEmpty(userId))
                msg["userId"] = Truncate(userId, 256);

            if (properties != null && properties.Count > 0)
                msg["properties"] = properties;

            return msg;
        }

        internal static Dictionary<string, object> Identify(
            string anonymousId,
            string userId,
            string identityType,
            string packageVersion,
            Dictionary<string, object> traits = null)
        {
            var msg = BuildBase("identify", packageVersion);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, 256);

            if (!string.IsNullOrEmpty(userId))
                msg["userId"] = Truncate(userId, 256);

            if (!string.IsNullOrEmpty(identityType))
                msg["identityType"] = identityType;

            if (traits != null && traits.Count > 0)
                msg["traits"] = traits;

            return msg;
        }

        internal static Dictionary<string, object> Alias(
            string fromId,
            string fromType,
            string toId,
            string toType,
            string packageVersion)
        {
            var msg = BuildBase("alias", packageVersion);
            msg["fromId"] = Truncate(fromId, 256);
            msg["fromType"] = fromType;
            msg["toId"] = Truncate(toId, 256);
            msg["toType"] = toType;
            return msg;
        }

        private static Dictionary<string, object> BuildBase(string type, string packageVersion)
        {
            return new Dictionary<string, object>
            {
                ["type"] = type,
                ["messageId"] = Guid.NewGuid().ToString(),
                ["eventTimestamp"] = DateTime.UtcNow.ToString("o"),
                ["context"] = new Dictionary<string, object>
                {
                    ["library"] = Constants.LibraryName,
                    ["libraryVersion"] = Truncate(packageVersion, 256)
                },
                ["surface"] = Constants.Surface
            };
        }

        private static string Truncate(string s, int maxLen)
        {
            if (s == null || s.Length <= maxLen)
                return s;
            return s.Substring(0, maxLen);
        }
    }
}
