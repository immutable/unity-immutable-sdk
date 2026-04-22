#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    internal static class MessageBuilder
    {
        internal static Dictionary<string, object> Track(
            string eventName,
            string? anonymousId,
            string? userId,
            string packageVersion,
            Dictionary<string, object>? properties = null)
        {
            var msg = BuildBase(MessageTypes.Track, packageVersion);
            msg["eventName"] = Truncate(eventName, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            if (properties != null && properties.Count > 0)
            {
                TruncateStringValues(properties);
                msg["properties"] = properties;
            }

            return msg;
        }

        internal static Dictionary<string, object> Identify(
            string? anonymousId,
            string? userId,
            string identityType,
            string packageVersion,
            Dictionary<string, object>? traits = null)
        {
            var msg = BuildBase(MessageTypes.Identify, packageVersion);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            msg["identityType"] = Truncate(identityType, Constants.MaxFieldLength);

            if (traits != null && traits.Count > 0)
            {
                TruncateStringValues(traits);
                msg["traits"] = traits;
            }

            return msg;
        }

        internal static Dictionary<string, object> Alias(
            string fromId,
            string fromType,
            string toId,
            string toType,
            string packageVersion)
        {
            var msg = BuildBase(MessageTypes.Alias, packageVersion);
            msg["fromId"] = Truncate(fromId, Constants.MaxFieldLength);
            msg["fromType"] = Truncate(fromType, Constants.MaxFieldLength);
            msg["toId"] = Truncate(toId, Constants.MaxFieldLength);
            msg["toType"] = Truncate(toType, Constants.MaxFieldLength);
            return msg;
        }

        private static Dictionary<string, object> BuildBase(string type, string packageVersion)
        {
            return new Dictionary<string, object>
            {
                [MessageFields.Type] = type,
                ["messageId"] = Guid.NewGuid().ToString(),
                ["eventTimestamp"] = DateTime.UtcNow.ToString("o"),
                ["context"] = new Dictionary<string, object>
                {
                    ["library"] = Constants.LibraryName,
                    ["libraryVersion"] = Truncate(packageVersion, Constants.MaxFieldLength)
                },
                ["surface"] = Constants.Surface
            };
        }

        private static string Truncate(string s, int maxLen)
        {
            if (s.Length <= maxLen)
                return s;
            return s.Substring(0, maxLen);
        }

        private static void TruncateStringValues(Dictionary<string, object> dict)
        {
            // Snapshot keys to avoid mutating the collection during iteration.
            var keys = new List<string>(dict.Keys);
            foreach (var key in keys)
            {
                if (dict[key] is string s && s.Length > Constants.MaxFieldLength)
                    dict[key] = Truncate(s, Constants.MaxFieldLength);
            }
        }
    }
}
