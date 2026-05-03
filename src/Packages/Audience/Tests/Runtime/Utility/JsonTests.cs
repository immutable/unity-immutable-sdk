using System;
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
            var data = new Dictionary<string, object> { { JsonRoundTripFixtures.KeyName, JsonRoundTripFixtures.HelloValue } };

            var result = Json.Serialize(data);

            Assert.AreEqual(JsonRoundTripFixtures.KeyHelloEncoded, result);
        }

        [Test]
        public void Serialize_StringWithSpecialChars_EscapesCorrectly()
        {
            var data = new Dictionary<string, object>
            {
                { JsonRoundTripFixtures.ValName, JsonRoundTripFixtures.EscapeRichString }
            };

            var result = Json.Serialize(data);

            Assert.AreEqual(JsonRoundTripFixtures.ValEscapeRichEncoded, result);
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
                    JsonRoundTripFixtures.OuterKey, new Dictionary<string, object>
                    {
                        { JsonRoundTripFixtures.InnerKey, JsonRoundTripFixtures.InnerValue }
                    }
                }
            };

            Assert.AreEqual(JsonRoundTripFixtures.OuterInnerEncoded, Json.Serialize(data));
        }

        [Test]
        public void Serialize_FloatNaN_SerializesAsNull()
        {
            Assert.AreEqual("{\"v\":null}", Json.Serialize(new Dictionary<string, object> { { "v", float.NaN } }));
        }

        [Test]
        public void Serialize_FloatPositiveInfinity_SerializesAsNull()
        {
            Assert.AreEqual("{\"v\":null}", Json.Serialize(new Dictionary<string, object> { { "v", float.PositiveInfinity } }));
        }

        [Test]
        public void Serialize_FloatNegativeInfinity_SerializesAsNull()
        {
            Assert.AreEqual("{\"v\":null}", Json.Serialize(new Dictionary<string, object> { { "v", float.NegativeInfinity } }));
        }

        [Test]
        public void Serialize_DoubleNaN_SerializesAsNull()
        {
            Assert.AreEqual("{\"v\":null}", Json.Serialize(new Dictionary<string, object> { { "v", double.NaN } }));
        }

        [Test]
        public void Serialize_DoubleInfinity_SerializesAsNull()
        {
            Assert.AreEqual("{\"v\":null}", Json.Serialize(new Dictionary<string, object> { { "v", double.PositiveInfinity } }));
        }

        [Test]
        public void Serialize_FloatValue_NormalRange()
        {
            var data = new Dictionary<string, object> { { "v", 3.14f } };
            var result = Json.Serialize(data);
            StringAssert.Contains("\"v\":", result);
            StringAssert.DoesNotContain("\"v\":\"", result); // must not be quoted
        }

        [Test]
        public void Serialize_FloatValue_LargeExponent_PreservesValue()
        {
            // 1e30f in scientific notation is valid JSON; must not be silently zeroed
            var data = new Dictionary<string, object> { { "v", 1e30f } };
            var result = Json.Serialize(data);
            var serialised = result.Substring(result.IndexOf(':') + 1, result.Length - result.IndexOf(':') - 2);
            Assert.AreNotEqual("0", serialised);
            Assert.AreNotEqual("0.000000", serialised);
        }

        [Test]
        public void Serialize_FloatValue_SmallNegativeExponent_PreservesValue()
        {
            // 1e-30f: the old F6 fallback turned this into "0.000000"
            var data = new Dictionary<string, object> { { "v", 1e-30f } };
            var result = Json.Serialize(data);
            var serialised = result.Substring(result.IndexOf(':') + 1, result.Length - result.IndexOf(':') - 2);
            Assert.AreNotEqual("0", serialised);
            Assert.AreNotEqual("0.000000", serialised);
        }

        [Test]
        public void Serialize_DoubleValue_SmallNegativeExponent_PreservesValue()
        {
            var data = new Dictionary<string, object> { { "v", 1e-300 } };
            var result = Json.Serialize(data);
            var serialised = result.Substring(result.IndexOf(':') + 1, result.Length - result.IndexOf(':') - 2);
            Assert.AreNotEqual("0", serialised);
            Assert.AreNotEqual("0.000000", serialised);
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
                { MessageFields.Type, MessageTypes.Track },
                { MessageFields.EventName, TestEventNames.LevelComplete },
                { MessageFields.AnonymousId, TestFixtures.AnonId123 },
                { MessageFields.UserId, null },
                { MessageFields.Properties, new Dictionary<string, object>
                    {
                        { "level", 5 },
                        { "score", 9800L },
                        { "perfect", true },
                        { "tags", new List<object> { "fast", "clean" } }
                    }
                }
            };

            var result = Json.Serialize(data);

            StringAssert.Contains($"\"{MessageFields.Type}\":\"{MessageTypes.Track}\"", result);
            StringAssert.Contains($"\"{MessageFields.EventName}\":\"level_complete\"", result);
            StringAssert.Contains($"\"{MessageFields.UserId}\":null", result);
            StringAssert.Contains("\"level\":5", result);
            StringAssert.Contains("\"perfect\":true", result);
            StringAssert.Contains("\"tags\":[\"fast\",\"clean\"]", result);
        }

        [Test]
        public void Serialize_NestingExceedsMaxDepth_ThrowsFormatException()
        {
            var root = new Dictionary<string, object>();
            var current = root;
            for (var i = 0; i < Json.MaxDepth; i++)
            {
                var next = new Dictionary<string, object>();
                current["next"] = next;
                current = next;
            }

            var ex = Assert.Throws<FormatException>(() => Json.Serialize(root));
            StringAssert.Contains("nesting exceeds", ex.Message);
        }

        [Test]
        public void Serialize_SelfReferentialDict_ThrowsFormatException()
        {
            var root = new Dictionary<string, object>();
            root["self"] = root;

            var ex = Assert.Throws<FormatException>(() => Json.Serialize(root));
            StringAssert.Contains("cycle", ex.Message);
        }

        [Test]
        public void Serialize_SharedChildInSiblingKeys_IsNotTreatedAsCycle()
        {
            // Diamond: visited set tracks the current recursion stack, not all objects ever seen.
            var shared = new Dictionary<string, object> { ["k"] = "v" };
            var root = new Dictionary<string, object>
            {
                ["a"] = shared,
                ["b"] = shared,
            };

            var result = Json.Serialize(root);

            StringAssert.Contains("\"a\":{\"k\":\"v\"}", result);
            StringAssert.Contains("\"b\":{\"k\":\"v\"}", result);
        }
    }
}
