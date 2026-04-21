using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class TypedEventTests
    {
        [Test]
        public void Progression_EventName_IsProgression()
        {
            Assert.AreEqual("progression", new Progression().EventName);
        }

        [Test]
        public void Progression_Complete_ProducesCorrectProperties()
        {
            var evt = new Progression
            {
                Status = ProgressionStatus.Complete,
                World = "tutorial",
                Level = "1",
                Score = 1500,
                DurationSec = 120.5f
            };

            var props = evt.ToProperties();

            Assert.AreEqual("complete", props["status"]);
            Assert.AreEqual("tutorial", props["world"]);
            Assert.AreEqual("1", props["level"]);
            Assert.AreEqual(1500, props["score"]);
            Assert.AreEqual(120.5f, props["durationSec"]);
        }

        [Test]
        public void Progression_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Progression { Status = ProgressionStatus.Start }.ToProperties();

            Assert.IsTrue(props.ContainsKey("status"));
            Assert.IsFalse(props.ContainsKey("world"));
            Assert.IsFalse(props.ContainsKey("level"));
            Assert.IsFalse(props.ContainsKey("stage"));
            Assert.IsFalse(props.ContainsKey("score"));
            Assert.IsFalse(props.ContainsKey("durationSec"));
        }

        [Test]
        public void Resource_Source_ProducesCorrectProperties()
        {
            var evt = new Resource
            {
                Flow = ResourceFlow.Source,
                Currency = "gold",
                Amount = 100,
                ItemType = "quest_reward",
                ItemId = "main_quest_01"
            };

            var props = evt.ToProperties();

            Assert.AreEqual("source", props["flow"]);
            Assert.AreEqual("gold", props["currency"]);
            Assert.AreEqual(100m, props["amount"]);
            Assert.AreEqual("quest_reward", props["itemType"]);
            Assert.AreEqual("main_quest_01", props["itemId"]);
        }

        [Test]
        public void Resource_EventName_IsResource()
        {
            Assert.AreEqual("resource", new Resource().EventName);
        }

        [Test]
        public void Purchase_ProducesCorrectProperties()
        {
            var evt = new Purchase
            {
                Currency = "USD",
                Value = 9.99m,
                ItemId = "gem_pack_01",
                ItemName = "Starter Gem Pack",
                Quantity = 1,
                TransactionId = "txn_abc123"
            };

            var props = evt.ToProperties();

            Assert.AreEqual("USD", props["currency"]);
            Assert.AreEqual(9.99m, props["value"]);
            Assert.AreEqual("gem_pack_01", props["itemId"]);
            Assert.AreEqual("Starter Gem Pack", props["itemName"]);
            Assert.AreEqual(1, props["quantity"]);
            Assert.AreEqual("txn_abc123", props["transactionId"]);
        }

        [Test]
        public void Purchase_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Purchase { Currency = "EUR", Value = 5.00m }.ToProperties();

            Assert.IsTrue(props.ContainsKey("currency"));
            Assert.IsTrue(props.ContainsKey("value"));
            Assert.IsFalse(props.ContainsKey("itemId"));
            Assert.IsFalse(props.ContainsKey("itemName"));
            Assert.IsFalse(props.ContainsKey("quantity"));
            Assert.IsFalse(props.ContainsKey("transactionId"));
        }

        [Test]
        public void Purchase_EventName_IsPurchase()
        {
            Assert.AreEqual("purchase", new Purchase().EventName);
        }

        [Test]
        public void MilestoneReached_ProducesCorrectProperties()
        {
            var props = new MilestoneReached { Name = "first_boss_defeated" }.ToProperties();

            Assert.AreEqual("first_boss_defeated", props["name"]);
            Assert.AreEqual(1, props.Count);
        }

        [Test]
        public void MilestoneReached_EventName_IsMilestoneReached()
        {
            Assert.AreEqual("milestone_reached", new MilestoneReached().EventName);
        }
    }
}