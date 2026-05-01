using System.Collections.Generic;

namespace Immutable.Audience.Tests
{
    // Builds JSON message envelopes from MessageFields / MessageTypes for tests.
    internal static class WireFixture
    {
        internal static string Track(params (string key, object value)[] extra) =>
            Build(MessageTypes.Track, extra);

        internal static string Identify(params (string key, object value)[] extra) =>
            Build(MessageTypes.Identify, extra);

        internal static string Alias(params (string key, object value)[] extra) =>
            Build(MessageTypes.Alias, extra);

        private static string Build(string type, (string key, object value)[] extra)
        {
            var dict = new Dictionary<string, object> { [MessageFields.Type] = type };
            foreach (var (key, value) in extra) dict[key] = value;
            return Json.Serialize(dict);
        }
    }
}
