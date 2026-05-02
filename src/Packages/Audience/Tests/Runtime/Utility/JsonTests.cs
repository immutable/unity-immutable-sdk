using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    public class JsonTests
    {
        // Per-scenario fixture keys/values for the serialise tests.
        private const string BoolFixtureKey = "flag";
        private const string NumericFixtureKey = "n";
        private const string NullFixtureKey = "x";
        private const string VShortFixture = "v";
        private const string ArrayFixtureKey = "items";

        // RealisticEventPayload: nested keys and arrays exercise nested-list serialisation.
        private const string PropLevelKey = "level";
        private const string PropScoreKey = "score";
        private const string PropPerfectKey = "perfect";
        private const string PropTagsKey = "tags";
        private const string TagFastValue = "fast";
        private const string TagCleanValue = "clean";

        // Cycle / depth guard fixtures.
        private const string DeepNestNextKey = "next";
        private const string SelfRefKey = "self";
        private const string CycleErrorMarker = "cycle";
        private const string NestingExceedsErrorMarker = "nesting exceeds";

        // Diamond scenario (shared child under sibling keys is NOT a cycle).
        private const string DiamondInnerKey = "k";
        private const string DiamondInnerValue = "v";
        private const string DiamondLeftKey = "a";
        private const string DiamondRightKey = "b";

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
            var data = new Dictionary<string, object> { { BoolFixtureKey, true } };

            Assert.AreEqual($"{{\"{BoolFixtureKey}\":true}}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_BoolFalse_ReturnsLowercaseFalse()
        {
            var data = new Dictionary<string, object> { { BoolFixtureKey, false } };

            Assert.AreEqual($"{{\"{BoolFixtureKey}\":false}}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_IntValue_ReturnsIntegerLiteral()
        {
            var data = new Dictionary<string, object> { { NumericFixtureKey, 42 } };

            Assert.AreEqual($"{{\"{NumericFixtureKey}\":42}}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_LongValue_ReturnsIntegerLiteral()
        {
            var data = new Dictionary<string, object> { { NumericFixtureKey, 9876543210L } };

            Assert.AreEqual($"{{\"{NumericFixtureKey}\":9876543210}}", Json.Serialize(data));
        }

        [Test]
        public void Serialize_NullValue_ReturnsJsonNull()
        {
            var data = new Dictionary<string, object> { { NullFixtureKey, null } };

            Assert.AreEqual($"{{\"{NullFixtureKey}\":null}}", Json.Serialize(data));
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
            Assert.AreEqual($"{{\"{VShortFixture}\":null}}", Json.Serialize(new Dictionary<string, object> { { VShortFixture, float.NaN } }));
        }

        [Test]
        public void Serialize_FloatPositiveInfinity_SerializesAsNull()
        {
            Assert.AreEqual($"{{\"{VShortFixture}\":null}}", Json.Serialize(new Dictionary<string, object> { { VShortFixture, float.PositiveInfinity } }));
        }

        [Test]
        public void Serialize_FloatNegativeInfinity_SerializesAsNull()
        {
            Assert.AreEqual($"{{\"{VShortFixture}\":null}}", Json.Serialize(new Dictionary<string, object> { { VShortFixture, float.NegativeInfinity } }));
        }

        [Test]
        public void Serialize_DoubleNaN_SerializesAsNull()
        {
            Assert.AreEqual($"{{\"{VShortFixture}\":null}}", Json.Serialize(new Dictionary<string, object> { { VShortFixture, double.NaN } }));
        }

        [Test]
        public void Serialize_DoubleInfinity_SerializesAsNull()
        {
            Assert.AreEqual($"{{\"{VShortFixture}\":null}}", Json.Serialize(new Dictionary<string, object> { { VShortFixture, double.PositiveInfinity } }));
        }

        [Test]
        public void Serialize_FloatValue_NormalRange()
        {
            var data = new Dictionary<string, object> { { VShortFixture, 3.14f } };
            var result = Json.Serialize(data);
            StringAssert.Contains($"\"{VShortFixture}\":", result);
            StringAssert.DoesNotContain($"\"{VShortFixture}\":\"", result);
        }

        [Test]
        public void Serialize_FloatValue_LargeExponent_PreservesValue()
        {
            // 1e30f in scientific notation is valid JSON; must not be silently zeroed
            var data = new Dictionary<string, object> { { VShortFixture, 1e30f } };
            var result = Json.Serialize(data);
            var serialised = result.Substring(result.IndexOf(':') + 1, result.Length - result.IndexOf(':') - 2);
            Assert.AreNotEqual("0", serialised);
            Assert.AreNotEqual("0.000000", serialised);
        }

        [Test]
        public void Serialize_FloatValue_SmallNegativeExponent_PreservesValue()
        {
            // 1e-30f: the old F6 fallback turned this into "0.000000"
            var data = new Dictionary<string, object> { { VShortFixture, 1e-30f } };
            var result = Json.Serialize(data);
            var serialised = result.Substring(result.IndexOf(':') + 1, result.Length - result.IndexOf(':') - 2);
            Assert.AreNotEqual("0", serialised);
            Assert.AreNotEqual("0.000000", serialised);
        }

        [Test]
        public void Serialize_DoubleValue_SmallNegativeExponent_PreservesValue()
        {
            var data = new Dictionary<string, object> { { VShortFixture, 1e-300 } };
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
                { ArrayFixtureKey, new List<object> { TestEventNames.PlaceholderA, 1, true } }
            };

            Assert.AreEqual($"{{\"{ArrayFixtureKey}\":[\"{TestEventNames.PlaceholderA}\",1,true]}}", Json.Serialize(data));
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
                        { PropLevelKey, 5 },
                        { PropScoreKey, 9800L },
                        { PropPerfectKey, true },
                        { PropTagsKey, new List<object> { TagFastValue, TagCleanValue } }
                    }
                }
            };

            var result = Json.Serialize(data);

            StringAssert.Contains($"\"{MessageFields.Type}\":\"{MessageTypes.Track}\"", result);
            StringAssert.Contains($"\"{MessageFields.EventName}\":\"{TestEventNames.LevelComplete}\"", result);
            StringAssert.Contains($"\"{MessageFields.UserId}\":null", result);
            StringAssert.Contains($"\"{PropLevelKey}\":5", result);
            StringAssert.Contains($"\"{PropPerfectKey}\":true", result);
            StringAssert.Contains($"\"{PropTagsKey}\":[\"{TagFastValue}\",\"{TagCleanValue}\"]", result);
        }

        [Test]
        public void Serialize_NestingExceedsMaxDepth_ThrowsFormatException()
        {
            var root = new Dictionary<string, object>();
            var current = root;
            for (var i = 0; i < Json.MaxDepth; i++)
            {
                var next = new Dictionary<string, object>();
                current[DeepNestNextKey] = next;
                current = next;
            }

            var ex = Assert.Throws<FormatException>(() => Json.Serialize(root));
            StringAssert.Contains(NestingExceedsErrorMarker, ex.Message);
        }

        [Test]
        public void Serialize_SelfReferentialDict_ThrowsFormatException()
        {
            var root = new Dictionary<string, object>();
            root[SelfRefKey] = root;

            var ex = Assert.Throws<FormatException>(() => Json.Serialize(root));
            StringAssert.Contains(CycleErrorMarker, ex.Message);
        }

        [Test]
        public void Serialize_SharedChildInSiblingKeys_IsNotTreatedAsCycle()
        {
            // Diamond: visited set tracks the current recursion stack, not all objects ever seen.
            var shared = new Dictionary<string, object> { [DiamondInnerKey] = DiamondInnerValue };
            var root = new Dictionary<string, object>
            {
                [DiamondLeftKey] = shared,
                [DiamondRightKey] = shared,
            };

            var result = Json.Serialize(root);

            StringAssert.Contains($"\"{DiamondLeftKey}\":{{\"{DiamondInnerKey}\":\"{DiamondInnerValue}\"}}", result);
            StringAssert.Contains($"\"{DiamondRightKey}\":{{\"{DiamondInnerKey}\":\"{DiamondInnerValue}\"}}", result);
        }
    }
}
