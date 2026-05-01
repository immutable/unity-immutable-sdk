using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class TypedEventTests
    {
        [Test]
        public void Progression_EventName_IsProgression()
        {
            Assert.AreEqual(EventNames.Progression, new Progression().EventName);
        }

        [Test]
        public void Progression_WithoutStatus_ThrowsOnToProperties()
        {
            var evt = new Progression { World = "tutorial" };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Status"));
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

            Assert.AreEqual(ProgressionStatus.Complete.ToLowercaseString(), props[EventPropertyKeys.Status]);
            Assert.AreEqual("tutorial", props[EventPropertyKeys.World]);
            Assert.AreEqual("1", props[EventPropertyKeys.Level]);
            Assert.AreEqual(1500, props[EventPropertyKeys.Score]);
            Assert.AreEqual(120.5f, props[EventPropertyKeys.DurationSec]);
        }

        [Test]
        public void Progression_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Progression { Status = ProgressionStatus.Start }.ToProperties();

            Assert.IsTrue(props.ContainsKey(EventPropertyKeys.Status));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.World));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.Level));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.Stage));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.Score));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.DurationSec));
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

            Assert.AreEqual(ResourceFlow.Source.ToLowercaseString(), props[EventPropertyKeys.Flow]);
            Assert.AreEqual("gold", props[EventPropertyKeys.Currency]);
            Assert.AreEqual(100m, props[EventPropertyKeys.Amount]);
            Assert.AreEqual("quest_reward", props[EventPropertyKeys.ItemType]);
            Assert.AreEqual("main_quest_01", props[EventPropertyKeys.ItemId]);
        }

        [Test]
        public void Resource_EventName_IsResource()
        {
            Assert.AreEqual(EventNames.Resource, new Resource().EventName);
        }

        [Test]
        public void Resource_WithoutFlow_ThrowsOnToProperties()
        {
            var evt = new Resource { Currency = "gold", Amount = 100 };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Flow"));
        }

        [Test]
        public void Resource_WithoutCurrency_ThrowsOnToProperties()
        {
            var evt = new Resource { Flow = ResourceFlow.Source, Amount = 100 };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Currency"));
        }

        [Test]
        public void Resource_WithoutAmount_ThrowsOnToProperties()
        {
            var evt = new Resource { Flow = ResourceFlow.Source, Currency = "gold" };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Amount"));
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

            Assert.AreEqual("USD", props[EventPropertyKeys.Currency]);
            Assert.AreEqual(9.99m, props[EventPropertyKeys.Value]);
            Assert.AreEqual("gem_pack_01", props[EventPropertyKeys.ItemId]);
            Assert.AreEqual("Starter Gem Pack", props[EventPropertyKeys.ItemName]);
            Assert.AreEqual(1, props[EventPropertyKeys.Quantity]);
            Assert.AreEqual("txn_abc123", props[EventPropertyKeys.TransactionId]);
        }

        [Test]
        public void Purchase_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Purchase { Currency = "EUR", Value = 5.00m }.ToProperties();

            Assert.IsTrue(props.ContainsKey(EventPropertyKeys.Currency));
            Assert.IsTrue(props.ContainsKey(EventPropertyKeys.Value));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.ItemId));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.ItemName));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.Quantity));
            Assert.IsFalse(props.ContainsKey(EventPropertyKeys.TransactionId));
        }

        [Test]
        public void Purchase_EventName_IsPurchase()
        {
            Assert.AreEqual(EventNames.Purchase, new Purchase().EventName);
        }

        [Test]
        public void Purchase_WithoutCurrency_ThrowsOnToProperties()
        {
            var evt = new Purchase { Value = 9.99m };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Currency"));
        }

        [Test]
        public void Purchase_WithoutValue_ThrowsOnToProperties()
        {
            var evt = new Purchase { Currency = "USD" };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("Value"));
        }

        [Test]
        public void MilestoneReached_ProducesCorrectProperties()
        {
            var props = new MilestoneReached { Name = "first_boss_defeated" }.ToProperties();

            Assert.AreEqual("first_boss_defeated", props[EventPropertyKeys.Name]);
            Assert.AreEqual(1, props.Count);
        }

        [Test]
        public void MilestoneReached_EventName_IsMilestoneReached()
        {
            Assert.AreEqual(EventNames.MilestoneReached, new MilestoneReached().EventName);
        }
    }
}