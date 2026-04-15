using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    public class JsonTests
    {
        [Test]
        public void Serialize_EmptyDict_ReturnsEmptyObject()
        {
            var result = Json.Serialize(new Dictionary<string, object>());

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Serialize_StringValue_ReturnsQuotedString()
        {
            var data = new Dictionary<string, object> { { "key", "hello" } };

            var result = Json.Serialize(data);

            Assert.AreEqual("{\"key\":\"hello\"}", result);
        }

        [Test]
        public void Serialize_StringWithSpecialChars_EscapesCorrectly()
        {
            var data = new Dictionary<string, object>
            {
                { "val", "say \"hi\"\nback\\slash\ttab" }
            };

            var result = Json.Serialize(data);

            Assert.AreEqual("{\"val\":\"say \\\"hi\\\"\\nback\\\\slash\\ttab\"}", result);
        }

        [Test]
        public void Serialize_BoolTrue_ReturnsLowercaseTrue()
        {
            var data = new Dictionary<string, object> { { "flag", true } };

            Assert.AreEqual("{\"flag\":true}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_BoolFalse_ReturnsLowercaseFalse()
        {
            var data = new Dictionary<string, object> { { "flag", false } };

            Assert.AreEqual("{\"flag\":false}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_IntValue_ReturnsIntegerLiteral()
        {
            var data = new Dictionary<string, object> { { "n", 42 } };

            Assert.AreEqual("{\"n\":42}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_LongValue_ReturnsIntegerLiteral()
        {
            var data = new Dictionary<string, object> { { "n", 9876543210L } };

            Assert.AreEqual("{\"n\":9876543210}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_NullValue_ReturnsJsonNull()
        {
            var data = new Dictionary<string, object> { { "x", null } };

            Assert.AreEqual("{\"x\":null}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_NestedDict_ReturnsNestedObject()
        {
            var data = new Dictionary<string, object>
            {
                {
                    "outer", new Dictionary<string, object>
                    {
                        { "inner", "value" }
                    }
                }
            };

            Assert.AreEqual("{\"outer\":{\"inner\":\"value\"}}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_ListValue_ReturnsJsonArray()
        {
            var data = new Dictionary<string, object>
            {
                { "items", new List<object> { "a", 1, true } }
            };

            Assert.AreEqual("{\"items\":[\"a\",1,true]}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_RealisticEventPayload_ProducesCorrectJson()
        {
            var data = new Dictionary<string, object>
            {
                { "type", "track" },
                { "eventName", "level_complete" },
                { "anonymousId", "anon-123" },
                { "userId", null },
                { "properties", new Dictionary<string, object>
                    {
                        { "level", 5 },
                        { "score", 9800L },
                        { "perfect", true },
                        { "tags", new List<object> { "fast", "clean" } }
                    }
                }
            };

            var result = Json.Serialize(data);

            StringAssert.Contains("\"type\":\"track\"", result);
            StringAssert.Contains("\"eventName\":\"level_complete\"", result);
            StringAssert.Contains("\"userId\":null", result);
            StringAssert.Contains("\"level\":5", result);
            StringAssert.Contains("\"perfect\":true", result);
            StringAssert.Contains("\"tags\":[\"fast\",\"clean\"]", result);
        }
    }
}
