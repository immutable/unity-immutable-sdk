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
            var evt = new Progression { World = TestFixtures.ProgressionWorldTutorial };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain(nameof(Progression.Status)));
        }

        [Test]
        public void Progression_Complete_ProducesCorrectProperties()
        {
            var evt = new Progression
            {
                Status = ProgressionStatus.Complete,
                World = TestFixtures.ProgressionWorldTutorial,
                Level = TestFixtures.ProgressionLevelFixture,
                Score = TestFixtures.ProgressionScoreFixture,
                DurationSec = TestFixtures.ProgressionDurationSecFixture
            };

            var props = evt.ToProperties();

            Assert.AreEqual(ProgressionStatus.Complete.ToLowercaseString(), props[EventPropertyKeys.Status]);
            Assert.AreEqual(TestFixtures.ProgressionWorldTutorial, props[EventPropertyKeys.World]);
            Assert.AreEqual(TestFixtures.ProgressionLevelFixture, props[EventPropertyKeys.Level]);
            Assert.AreEqual(TestFixtures.ProgressionScoreFixture, props[EventPropertyKeys.Score]);
            Assert.AreEqual(TestFixtures.ProgressionDurationSecFixture, props[EventPropertyKeys.DurationSec]);
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
                Currency = TestFixtures.ResourceCurrency,
                Amount = TestFixtures.ResourceAmountFixture,
                ItemType = TestFixtures.ResourceItemType,
                ItemId = TestFixtures.ResourceItemId
            };

            var props = evt.ToProperties();

            Assert.AreEqual(ResourceFlow.Source.ToLowercaseString(), props[EventPropertyKeys.Flow]);
            Assert.AreEqual(TestFixtures.ResourceCurrency, props[EventPropertyKeys.Currency]);
            Assert.AreEqual(TestFixtures.ResourceAmountFixture, props[EventPropertyKeys.Amount]);
            Assert.AreEqual(TestFixtures.ResourceItemType, props[EventPropertyKeys.ItemType]);
            Assert.AreEqual(TestFixtures.ResourceItemId, props[EventPropertyKeys.ItemId]);
        }

        [Test]
        public void Resource_EventName_IsResource()
        {
            Assert.AreEqual(EventNames.Resource, new Resource().EventName);
        }

        [Test]
        public void Resource_WithoutFlow_ThrowsOnToProperties()
        {
            var evt = new Resource { Currency = TestFixtures.ResourceCurrency, Amount = TestFixtures.ResourceAmountFixture };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain(nameof(Resource.Flow)));
        }

        [Test]
        public void Resource_WithoutCurrency_ThrowsOnToProperties()
        {
            var evt = new Resource { Flow = ResourceFlow.Source, Amount = TestFixtures.ResourceAmountFixture };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain(nameof(Resource.Currency)));
        }

        [Test]
        public void Resource_WithoutAmount_ThrowsOnToProperties()
        {
            var evt = new Resource { Flow = ResourceFlow.Source, Currency = TestFixtures.ResourceCurrency };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain(nameof(Resource.Amount)));
        }

        [Test]
        public void Purchase_ProducesCorrectProperties()
        {
            var evt = new Purchase
            {
                Currency = TestFixtures.UsdCurrency,
                Value = 9.99m,
                ItemId = TestFixtures.PurchaseItemId,
                ItemName = TestFixtures.PurchaseItemName,
                Quantity = 1,
                TransactionId = TestFixtures.PurchaseTransactionId
            };

            var props = evt.ToProperties();

            Assert.AreEqual(TestFixtures.UsdCurrency, props[EventPropertyKeys.Currency]);
            Assert.AreEqual(9.99m, props[EventPropertyKeys.Value]);
            Assert.AreEqual(TestFixtures.PurchaseItemId, props[EventPropertyKeys.ItemId]);
            Assert.AreEqual(TestFixtures.PurchaseItemName, props[EventPropertyKeys.ItemName]);
            Assert.AreEqual(1, props[EventPropertyKeys.Quantity]);
            Assert.AreEqual(TestFixtures.PurchaseTransactionId, props[EventPropertyKeys.TransactionId]);
        }

        [Test]
        public void Purchase_OptionalFieldsOmitted_WhenNull()
        {
            var props = new Purchase { Currency = TestFixtures.EurCurrency, Value = 5.00m }.ToProperties();

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
            Assert.That(ex!.Message, Does.Contain(nameof(Purchase.Currency)));
        }

        [Test]
        public void Purchase_WithoutValue_ThrowsOnToProperties()
        {
            var evt = new Purchase { Currency = TestFixtures.UsdCurrency };

            var ex = Assert.Throws<ArgumentException>(() => evt.ToProperties());
            Assert.That(ex!.Message, Does.Contain(nameof(Purchase.Value)));
        }

        [Test]
        public void MilestoneReached_ProducesCorrectProperties()
        {
            var props = new MilestoneReached { Name = TestFixtures.MilestoneName }.ToProperties();

            Assert.AreEqual(TestFixtures.MilestoneName, props[EventPropertyKeys.Name]);
            Assert.AreEqual(1, props.Count);
        }

        [Test]
        public void MilestoneReached_EventName_IsMilestoneReached()
        {
            Assert.AreEqual(EventNames.MilestoneReached, new MilestoneReached().EventName);
        }
    }
}