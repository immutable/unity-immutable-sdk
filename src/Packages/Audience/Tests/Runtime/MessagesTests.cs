using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    // Pins each message constant to its exact wording so a reword breaks the build.
    [TestFixture]
    internal class AudienceErrorMessagesTests
    {
        [Test]
        public void LocalStorageReadFailed_PrefixesAndAppendsExceptionMessage()
        {
            var ex = new InvalidOperationException("disk full");
            Assert.AreEqual(
                "Local storage read failed: disk full",
                AudienceErrorMessages.LocalStorageReadFailed(ex));
        }

        [Test]
        public void BatchPartiallyRejected_FormatsRejectedAndTotalCounts()
        {
            Assert.AreEqual(
                "Batch partially rejected: 3 of 20 events dropped",
                AudienceErrorMessages.BatchPartiallyRejected(3, 20));
        }

        [Test]
        public void BatchRejectedPrefix_IsExactWording()
        {
            Assert.AreEqual("Batch rejected", AudienceErrorMessages.BatchRejectedPrefix);
        }

        [Test]
        public void ServerErrorWillRetryPrefix_IsExactWording()
        {
            Assert.AreEqual("Server error, will retry", AudienceErrorMessages.ServerErrorWillRetryPrefix);
        }

        [Test]
        public void ConsentSyncFailedWithStatus_FormatsStatusCode()
        {
            Assert.AreEqual(
                "Consent sync failed with status 503",
                AudienceErrorMessages.ConsentSyncFailedWithStatus(503));
        }

        [Test]
        public void ConsentSyncThrew_PrefixesAndAppendsExceptionMessage()
        {
            var ex = new TimeoutException("timed out");
            Assert.AreEqual(
                "Consent sync threw: timed out",
                AudienceErrorMessages.ConsentSyncThrew(ex));
        }
    }

    [TestFixture]
    internal class AudienceArgumentMessagesTests
    {
        [Test]
        public void PublishableKeyRequired_IsExactWording()
        {
            Assert.AreEqual("PublishableKey is required",
                AudienceArgumentMessages.PublishableKeyRequired);
        }

        [Test]
        public void PersistentDataPathRequired_IsExactWording()
        {
            Assert.AreEqual("PersistentDataPath is required",
                AudienceArgumentMessages.PersistentDataPathRequired);
        }

        [Test]
        public void ProgressionStatusRequired_IsExactWording()
        {
            Assert.AreEqual(
                "Progression.Status is required. Set it before calling Track(IEvent).",
                AudienceArgumentMessages.ProgressionStatusRequired);
        }

        [Test]
        public void ResourceFlowRequired_IsExactWording()
        {
            Assert.AreEqual(
                "Resource.Flow is required. Set it before calling Track(IEvent).",
                AudienceArgumentMessages.ResourceFlowRequired);
        }

        [Test]
        public void ResourceCurrencyRequired_IsExactWording()
        {
            Assert.AreEqual(
                "Resource.Currency is required. Set a non-empty string before calling Track(IEvent).",
                AudienceArgumentMessages.ResourceCurrencyRequired);
        }

        [Test]
        public void ResourceAmountRequired_IsExactWording()
        {
            Assert.AreEqual(
                "Resource.Amount is required. Set it before calling Track(IEvent).",
                AudienceArgumentMessages.ResourceAmountRequired);
        }

        [Test]
        public void PurchaseValueRequired_IsExactWording()
        {
            Assert.AreEqual(
                "Purchase.Value is required. Set it before calling Track(IEvent).",
                AudienceArgumentMessages.PurchaseValueRequired);
        }

        [Test]
        public void MilestoneReachedNameRequired_IsExactWording()
        {
            Assert.AreEqual(
                "MilestoneReached.Name must not be null or empty",
                AudienceArgumentMessages.MilestoneReachedNameRequired);
        }

        [TestCase("USD",
            "Purchase.Currency 'USD' must be a three-letter uppercase ISO 4217 code")]
        [TestCase(null,
            "Purchase.Currency '' must be a three-letter uppercase ISO 4217 code")]
        [TestCase("usd",
            "Purchase.Currency 'usd' must be a three-letter uppercase ISO 4217 code")]
        public void PurchaseCurrencyInvalid_FormatsCurrencyValue(string? currency, string expected)
        {
            Assert.AreEqual(expected, AudienceArgumentMessages.PurchaseCurrencyInvalid(currency));
        }
    }
}
