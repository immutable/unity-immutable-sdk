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
            msg[MessageFields.EventName] = Truncate(eventName, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(anonymousId))
                msg[MessageFields.AnonymousId] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            if (properties != null && properties.Count > 0)
            {
                TruncateStringValues(properties);
                msg[MessageFields.Properties] = properties;
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
                msg[MessageFields.AnonymousId] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            msg[MessageFields.IdentityType] = Truncate(identityType, Constants.MaxFieldLength);

            if (traits != null && traits.Count > 0)
            {
                TruncateStringValues(traits);
                msg[MessageFields.Traits] = traits;
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
            msg[MessageFields.FromId] = Truncate(fromId, Constants.MaxFieldLength);
            msg[MessageFields.FromType] = Truncate(fromType, Constants.MaxFieldLength);
            msg[MessageFields.ToId] = Truncate(toId, Constants.MaxFieldLength);
            msg[MessageFields.ToType] = Truncate(toType, Constants.MaxFieldLength);
            return msg;
        }

        private static Dictionary<string, object> BuildBase(string type, string packageVersion)
        {
            return new Dictionary<string, object>
            {
                [MessageFields.Type] = type,
                [MessageFields.MessageId] = Guid.NewGuid().ToString(),
                [MessageFields.EventTimestamp] = DateTime.UtcNow.ToString(Constants.IsoTimestampFormat),
                [MessageFields.Context] = new Dictionary<string, object>
                {
                    [MessageFields.Library] = Constants.LibraryName,
                    [MessageFields.LibraryVersion] = Truncate(packageVersion, Constants.MaxFieldLength)
                },
                [MessageFields.Surface] = Constants.Surface
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
