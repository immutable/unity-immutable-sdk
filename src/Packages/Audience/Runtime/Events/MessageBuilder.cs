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
            string? deviceId,
            string packageVersion,
            string consentLevel,
            Dictionary<string, object>? properties = null,
            string? sessionId = null,
            bool testMode = false,
            DateTime? eventTimestamp = null)
        {
            var msg = BuildBase(MessageTypes.Track, packageVersion, consentLevel, testMode, eventTimestamp);
            msg["eventName"] = Truncate(eventName, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(deviceId))
                msg[MessageFields.DeviceId] = Truncate(deviceId, Constants.MaxFieldLength);

            if (properties != null && properties.Count > 0)
            {
                TruncateStringValues(properties);
                msg["properties"] = properties;
            }

            if (!string.IsNullOrEmpty(sessionId))
                msg[MessageFields.SessionId] = Truncate(sessionId, Constants.MaxFieldLength);

            return msg;
        }

        internal static Dictionary<string, object> Identify(
            string? anonymousId,
            string? userId,
            string? deviceId,
            string identityType,
            string packageVersion,
            string consentLevel,
            Dictionary<string, object>? traits = null,
            string? sessionId = null,
            bool testMode = false)
        {
            var msg = BuildBase(MessageTypes.Identify, packageVersion, consentLevel, testMode);

            if (!string.IsNullOrEmpty(anonymousId))
                msg["anonymousId"] = Truncate(anonymousId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(userId))
                msg[MessageFields.UserId] = Truncate(userId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(deviceId))
                msg[MessageFields.DeviceId] = Truncate(deviceId, Constants.MaxFieldLength);

            msg["identityType"] = Truncate(identityType, Constants.MaxFieldLength);

            if (traits != null && traits.Count > 0)
            {
                TruncateStringValues(traits);
                msg["traits"] = traits;
            }

            if (!string.IsNullOrEmpty(sessionId))
                msg[MessageFields.SessionId] = Truncate(sessionId, Constants.MaxFieldLength);

            return msg;
        }

        internal static Dictionary<string, object> Alias(
            string fromId,
            string fromType,
            string toId,
            string toType,
            string? deviceId,
            string packageVersion,
            string consentLevel,
            string? sessionId = null,
            bool testMode = false)
        {
            var msg = BuildBase(MessageTypes.Alias, packageVersion, consentLevel, testMode);
            msg["fromId"] = Truncate(fromId, Constants.MaxFieldLength);
            msg["fromType"] = Truncate(fromType, Constants.MaxFieldLength);
            msg["toId"] = Truncate(toId, Constants.MaxFieldLength);
            msg["toType"] = Truncate(toType, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(deviceId))
                msg[MessageFields.DeviceId] = Truncate(deviceId, Constants.MaxFieldLength);

            if (!string.IsNullOrEmpty(sessionId))
                msg[MessageFields.SessionId] = Truncate(sessionId, Constants.MaxFieldLength);

            return msg;
        }

        private static Dictionary<string, object> BuildBase(
            string type, string packageVersion, string consentLevel, bool testMode, DateTime? eventTimestamp = null)
        {
            var msg = new Dictionary<string, object>
            {
                [MessageFields.Type] = type,
                ["messageId"] = Guid.NewGuid().ToString(),
                // ToUniversalTime guards against a caller passing Local/Unspecified;
                // "o" only round-trips as UTC when Kind is actually Utc.
                ["eventTimestamp"] = (eventTimestamp ?? DateTime.UtcNow).ToUniversalTime().ToString("o"),
                ["context"] = new Dictionary<string, object>
                {
                    ["library"] = Constants.LibraryName,
                    ["libraryVersion"] = Truncate(packageVersion, Constants.MaxFieldLength)
                },
                ["surface"] = Constants.Surface,
                [MessageFields.ConsentLevel] = consentLevel
            };
            if (testMode)
                msg["test"] = true;
            return msg;
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
