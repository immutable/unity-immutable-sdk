using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    public class JsonReaderTests
    {
        // Per-scenario fixture keys / values used by the deserialise tests.
        private const string IntFixtureKey = "small";
        private const string LongFixtureKey = "big";
        private const string BoolTrueKey = "t";
        private const string BoolFalseKey = "f";
        private const string NullKey = "n";
        private const string ArrayKey = "arr";
        private const string StringElementValue = "two";

        // Anonymous ID placeholder for RoundTripViaSerializer.
        private const string AnonymousIdFixture = "abc";

        // MalformedThrows test: three deliberately invalid JSON inputs that
        // exercise distinct parser failure modes.
        private const string MalformedNotValid = "{not valid}";
        private const string MalformedEmptyValue = "{\"a\":}";
        private const string MalformedUnterminatedString = "{\"a\":\"unterminated";

        [Test]
        public void EmptyObject()
        {
            var result = JsonReader.DeserializeObject("{}");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void StringValue()
        {
            var result = JsonReader.DeserializeObject(JsonRoundTripFixtures.KeyHelloEncoded);
            Assert.AreEqual(JsonRoundTripFixtures.HelloValue, result[JsonRoundTripFixtures.KeyName]);
        }

        [Test]
        public void StringWithEscapes()
        {
            var result = JsonReader.DeserializeObject(JsonRoundTripFixtures.ValEscapeRichEncoded);
            Assert.AreEqual(JsonRoundTripFixtures.EscapeRichString, result[JsonRoundTripFixtures.ValName]);
        }

        [Test]
        public void IntAndLong()
        {
            var result = JsonReader.DeserializeObject($"{{\"{IntFixtureKey}\":42,\"{LongFixtureKey}\":12345678901234}}");
            Assert.AreEqual(42, result[IntFixtureKey]);
            Assert.AreEqual(12345678901234L, result[LongFixtureKey]);
        }

        [Test]
        public void BoolAndNull()
        {
            var result = JsonReader.DeserializeObject($"{{\"{BoolTrueKey}\":true,\"{BoolFalseKey}\":false,\"{NullKey}\":null}}");
            Assert.AreEqual(true, result[BoolTrueKey]);
            Assert.AreEqual(false, result[BoolFalseKey]);
            Assert.IsNull(result[NullKey]);
        }

        [Test]
        public void NestedObject()
        {
            var result = JsonReader.DeserializeObject(JsonRoundTripFixtures.OuterInnerEncoded);
            var inner = (Dictionary<string, object>)result[JsonRoundTripFixtures.OuterKey];
            Assert.AreEqual(JsonRoundTripFixtures.InnerValue, inner[JsonRoundTripFixtures.InnerKey]);
        }

        [Test]
        public void Array()
        {
            var result = JsonReader.DeserializeObject($"{{\"{ArrayKey}\":[1,\"{StringElementValue}\",true,null]}}");
            var arr = (List<object>)result[ArrayKey];
            Assert.AreEqual(4, arr.Count);
            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(StringElementValue, arr[1]);
            Assert.AreEqual(true, arr[2]);
            Assert.IsNull(arr[3]);
        }

        [Test]
        public void RoundTripViaSerializer()
        {
            var original = new Dictionary<string, object>
            {
                [MessageFields.Type] = MessageTypes.Track,
                [MessageFields.EventName] = EventNames.Progression,
                [MessageFields.Properties] = new Dictionary<string, object>
                {
                    [EventPropertyKeys.Status] = ProgressionStatus.Complete.ToLowercaseString(),
                    [EventPropertyKeys.Score] = 1500
                },
                [MessageFields.AnonymousId] = AnonymousIdFixture,
                [MessageFields.UserId] = TestFixtures.SteamId64
            };

            var serialized = Json.Serialize(original);
            var parsed = JsonReader.DeserializeObject(serialized);

            Assert.AreEqual(MessageTypes.Track, parsed[MessageFields.Type]);
            Assert.AreEqual(EventNames.Progression, parsed[MessageFields.EventName]);
            Assert.AreEqual(AnonymousIdFixture, parsed[MessageFields.AnonymousId]);
            Assert.AreEqual(TestFixtures.SteamId64, parsed[MessageFields.UserId]);
            var props = (Dictionary<string, object>)parsed[MessageFields.Properties];
            Assert.AreEqual(ProgressionStatus.Complete.ToLowercaseString(), props[EventPropertyKeys.Status]);
            Assert.AreEqual(1500, props[EventPropertyKeys.Score]);
        }

        [Test]
        public void MalformedThrows()
        {
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject(MalformedNotValid));
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject(MalformedEmptyValue));
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject(MalformedUnterminatedString));
        }
    }
}
