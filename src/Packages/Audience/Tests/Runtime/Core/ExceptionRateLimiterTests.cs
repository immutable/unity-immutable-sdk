#nullable enable

using System;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class ExceptionRateLimiterTests
    {
        private DateTime _now;

        private ExceptionRateLimiter MakeLimiter(int capacity = 20, int refillSeconds = 10)
        {
            _now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return new ExceptionRateLimiter(capacity, TimeSpan.FromSeconds(refillSeconds), () => _now);
        }

        [Test]
        public void TryConsume_BurstUpToCapacity_AllAllowed()
        {
            var limiter = MakeLimiter(capacity: 3);

            for (var i = 0; i < 3; i++)
            {
                var (allowed, firstTimeLimited) = limiter.TryConsume("Exception");
                Assert.IsTrue(allowed, $"attempt {i} should be within capacity");
                Assert.IsFalse(firstTimeLimited);
            }
        }

        [Test]
        public void TryConsume_BeyondCapacity_Dropped()
        {
            var limiter = MakeLimiter(capacity: 3);
            for (var i = 0; i < 3; i++) limiter.TryConsume("Exception");

            var (allowed, firstTimeLimited) = limiter.TryConsume("Exception");

            Assert.IsFalse(allowed);
            Assert.IsTrue(firstTimeLimited, "the first drop after exhausting the bucket must report firstTimeLimited");
        }

        [Test]
        public void TryConsume_FirstTimeLimited_OnlyReportedOnce()
        {
            var limiter = MakeLimiter(capacity: 1);
            limiter.TryConsume("Exception");

            var (_, first) = limiter.TryConsume("Exception");
            var (_, second) = limiter.TryConsume("Exception");

            Assert.IsTrue(first, "first drop after exhaustion should report firstTimeLimited");
            Assert.IsFalse(second, "subsequent drops in the same exhaustion episode should not report it again");
        }

        [Test]
        public void TryConsume_RefillsOverTime_AllowsOneMore()
        {
            var limiter = MakeLimiter(capacity: 1, refillSeconds: 10);
            limiter.TryConsume("Exception");
            Assert.IsFalse(limiter.TryConsume("Exception").allowed, "bucket should be empty right after the burst");

            _now = _now.AddSeconds(10);

            Assert.IsTrue(limiter.TryConsume("Exception").allowed, "one refill interval should free exactly one token");
        }

        [Test]
        public void TryConsume_DoesNotOverflowPastCapacity()
        {
            var limiter = MakeLimiter(capacity: 3, refillSeconds: 10);
            limiter.TryConsume("Exception");

            _now = _now.AddSeconds(1000); // far more than enough to refill several times over

            for (var i = 0; i < 3; i++)
                Assert.IsTrue(limiter.TryConsume("Exception").allowed, $"attempt {i} should still be within capacity");
            Assert.IsFalse(limiter.TryConsume("Exception").allowed, "capacity must not overflow past its max even after a long idle period");
        }

        [Test]
        public void TryConsume_DifferentExceptionTypes_HaveIndependentBuckets()
        {
            var limiter = MakeLimiter(capacity: 1);
            limiter.TryConsume("NullReferenceException");

            Assert.IsFalse(limiter.TryConsume("NullReferenceException").allowed,
                "NullReferenceException's bucket should already be empty");
            Assert.IsTrue(limiter.TryConsume("ArgumentException").allowed,
                "a different exception type must have its own, untouched bucket");
        }

        [Test]
        public void Reset_ClearsAllBuckets()
        {
            var limiter = MakeLimiter(capacity: 1);
            limiter.TryConsume("Exception");
            Assert.IsFalse(limiter.TryConsume("Exception").allowed);

            limiter.Reset();

            Assert.IsTrue(limiter.TryConsume("Exception").allowed, "a bucket must start full again after Reset");
        }
    }
}
