using System;
using System.Linq;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class TypedEventTests
    {
        // Guards against a new built-in typed event being added without a
        // matching ReservedEvents entry, which would leave it silently
        // unvalidated on the Track(string, Dictionary) path.
        [Test]
        public void EveryBuiltInEventHasAReservedEventsEntry()
        {
            var builtInTypes = typeof(IBuiltInEvent).Assembly.GetTypes()
                .Where(t => typeof(IBuiltInEvent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in builtInTypes)
            {
                var evt = (IEvent)Activator.CreateInstance(type)!;
                Assert.That(ReservedEvents.RequiredProperties.ContainsKey(evt.EventName), Is.True,
                    $"{type.Name} implements IBuiltInEvent but ReservedEvents.RequiredProperties has no entry " +
                    $"for \"{evt.EventName}\" (add one, even []).");
            }
        }

        [Test]
        public void Progression_EventName_IsProgression()
        {
            Assert.AreEqual("progression", new Progression().EventName);
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

            Assert.AreEqual("complete", props["status"]);
            Assert.AreEqual("tutorial", props["world"]);
            Assert.AreEqual("1", props["level"]);
            Assert.AreEqual(1500, props["score"]);
            Assert.AreEqual(120.5f, props["duration_sec"]);
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
            Assert.IsFalse(props.ContainsKey("duration_sec"));
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
            Assert.AreEqual("quest_reward", props["item_type"]);
            Assert.AreEqual("main_quest_01", props["item_id"]);
        }

        [Test]
        public void Resource_EventName_IsResource()
        {
            Assert.AreEqual("resource", new Resource().EventName);
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
                Value = "9.99",
                ItemId = "gem_pack_01",
                ItemName = "Starter Gem Pack",
                Quantity = 1,
                TransactionId = "txn_abc123"
            };

            var props = evt.ToProperties();

            Assert.AreEqual("USD", props["currency"]);
            Assert.AreEqual("9.99", props["value"]);
            Assert.AreEqual("gem_pack_01", props["item_id"]);
            Assert.AreEqual("Starter Gem Pack", props["item_name"]);
            Assert.AreEqual(1, props["quantity"]);
            Assert.AreEqual("txn_abc123", props["transaction_id"]);
        }

        [Test]
        public void Purchase_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Purchase { Currency = "EUR", Value = "5.00" }.ToProperties();

            Assert.IsTrue(props.ContainsKey("currency"));
            Assert.IsTrue(props.ContainsKey("value"));
            Assert.IsFalse(props.ContainsKey("item_id"));
            Assert.IsFalse(props.ContainsKey("item_name"));
            Assert.IsFalse(props.ContainsKey("quantity"));
            Assert.IsFalse(props.ContainsKey("transaction_id"));
        }

        [Test]
        public void Purchase_EventName_IsPurchase()
        {
            Assert.AreEqual("purchase", new Purchase().EventName);
        }

        [Test]
        public void Purchase_WithoutCurrency_ThrowsOnToProperties()
        {
            var evt = new Purchase { Value = "9.99" };

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
        public void AchievementUnlocked_EventName_IsAchievementUnlocked()
        {
            Assert.AreEqual("achievement_unlocked", new AchievementUnlocked().EventName);
        }

        [Test]
        public void AchievementUnlocked_WithoutAchievementId_ThrowsOnToProperties()
        {
            var evt = new AchievementUnlocked { AchievementName = "100 Enemies Defeated" };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("AchievementId"));
        }

        [Test]
        public void AchievementUnlocked_WithoutAchievementName_ThrowsOnToProperties()
        {
            var evt = new AchievementUnlocked { AchievementId = "ach_enemies_100" };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain("AchievementName"));
        }

        [Test]
        public void AchievementUnlocked_ProducesCorrectProperties()
        {
            var evt = new AchievementUnlocked
            {
                AchievementId = "ach_enemies_100",
                AchievementName = "100 Enemies Defeated",
                AchievementType = Immutable.Audience.AchievementType.Mastery,
            };

            var props = evt.ToProperties();

            Assert.AreEqual("ach_enemies_100", props["achievement_id"]);
            Assert.AreEqual("100 Enemies Defeated", props["achievement_name"]);
            Assert.AreEqual("mastery", props["achievement_type"]);
        }

        [Test]
        public void AchievementUnlocked_OptionalFieldsOmitted_WhenNull()
        {
            var props = new AchievementUnlocked
            {
                AchievementId = "ach_enemies_100",
                AchievementName = "100 Enemies Defeated",
            }.ToProperties();

            Assert.IsTrue(props.ContainsKey("achievement_id"));
            Assert.IsTrue(props.ContainsKey("achievement_name"));
            Assert.IsFalse(props.ContainsKey("achievement_type"));
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