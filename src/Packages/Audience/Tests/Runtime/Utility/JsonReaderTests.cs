using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    public class JsonReaderTests
    {
        [Test]
        public void EmptyObject()
        {
            var result = JsonReader.DeserializeObject("{}");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void StringValue()
        {
            var result = JsonReader.DeserializeObject("{\"key\":\"hello\"}");
            Assert.AreEqual("hello", result["key"]);
        }

        [Test]
        public void StringWithEscapes()
        {
            var result = JsonReader.DeserializeObject("{\"val\":\"say \\\"hi\\\"\\nback\\\\slash\\ttab\"}");
            Assert.AreEqual("say \"hi\"\nback\\slash\ttab", result["val"]);
        }

        [Test]
        public void IntAndLong()
        {
            var result = JsonReader.DeserializeObject("{\"small\":42,\"big\":12345678901234}");
            Assert.AreEqual(42, result["small"]);
            Assert.AreEqual(12345678901234L, result["big"]);
        }

        [Test]
        public void BoolAndNull()
        {
            var result = JsonReader.DeserializeObject("{\"t\":true,\"f\":false,\"n\":null}");
            Assert.AreEqual(true, result["t"]);
            Assert.AreEqual(false, result["f"]);
            Assert.IsNull(result["n"]);
        }

        [Test]
        public void NestedObject()
        {
            var result = JsonReader.DeserializeObject("{\"outer\":{\"inner\":\"value\"}}");
            var inner = (Dictionary<string, object>)result["outer"];
            Assert.AreEqual("value", inner["inner"]);
        }

        [Test]
        public void Array()
        {
            var result = JsonReader.DeserializeObject("{\"arr\":[1,\"two\",true,null]}");
            var arr = (List<object>)result["arr"];
            Assert.AreEqual(4, arr.Count);
            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual("two", arr[1]);
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
                [MessageFields.AnonymousId] = "abc",
                [MessageFields.UserId] = "76561198012345"
            };

            var serialized = Json.Serialize(original);
            var parsed = JsonReader.DeserializeObject(serialized);

            Assert.AreEqual(MessageTypes.Track, parsed[MessageFields.Type]);
            Assert.AreEqual(EventNames.Progression, parsed[MessageFields.EventName]);
            Assert.AreEqual("abc", parsed[MessageFields.AnonymousId]);
            Assert.AreEqual("76561198012345", parsed[MessageFields.UserId]);
            var props = (Dictionary<string, object>)parsed[MessageFields.Properties];
            Assert.AreEqual(ProgressionStatus.Complete.ToLowercaseString(), props[EventPropertyKeys.Status]);
            Assert.AreEqual(1500, props[EventPropertyKeys.Score]);
        }

        [Test]
        public void MalformedThrows()
        {
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject("{not valid}"));
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject("{\"a\":}"));
            Assert.Throws<System.FormatException>(() => JsonReader.DeserializeObject("{\"a\":\"unterminated"));
        }
    }
}
