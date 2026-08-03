namespace Immutable.Audience.Tests
{
    // Paired serialise/deserialise fixtures shared between JsonTests and JsonReaderTests.
    // Each pair pins the round-trip guarantee.
    internal static class JsonRoundTripFixtures
    {
        // Single string key / value pair.
        internal const string KeyName = "key";
        internal const string HelloValue = "hello";
        internal const string KeyHelloEncoded = "{\"key\":\"hello\"}";

        // String containing every escape sequence the codec handles
        // (escaped quote, newline, backslash, tab).
        internal const string ValName = "val";
        internal const string EscapeRichString = "say \"hi\"\nback\\slash\ttab";
        internal const string ValEscapeRichEncoded = "{\"val\":\"say \\\"hi\\\"\\nback\\\\slash\\ttab\"}";

        // Nested object.
        internal const string OuterKey = "outer";
        internal const string InnerKey = "inner";
        internal const string InnerValue = "value";
        internal const string OuterInnerEncoded = "{\"outer\":{\"inner\":\"value\"}}";
    }
}
