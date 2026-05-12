using System;
using System.Threading;
using NUnit.Framework;

namespace Immutable.Audience.Tests
{
    [TestFixture]
    internal class TimerDisposalTests
    {
        [Test]
        public void DisposeAndWait_NullTimer_ReturnsTrue()
        {
            bool result = TimerDisposal.DisposeAndWait(null, TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(result);
        }

        [Test]
        public void DisposeAndWait_AlreadyDisposedTimer_ReturnsTrue()
        {
            var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();

            bool result = TimerDisposal.DisposeAndWait(timer, TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(result);
        }

        [Test]
        public void DisposeAndWait_IdleTimer_SignalsBeforeTimeout()
        {
            var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);

            bool result = TimerDisposal.DisposeAndWait(timer, TimeSpan.FromSeconds(2));
            Assert.IsTrue(result, "idle timer should signal completion within the budget");
        }

        [Test]
        public void DisposeAndWait_LongCallback_ReturnsFalseAndLeaksHandle()
        {
            // Mono player builds (Unity Standalone Mono targets) signal
            // Timer.Dispose's wait handle ahead of in-flight callbacks
            // completing, which breaks this unit test's premise. Production
            // code is unaffected. DrainHeartbeatTimer is exercised
            // end-to-end by the SampleApp PlayMode tests on the same Mono
            // builds and works correctly. This unit test asserts a
            // lower-level WaitHandle invariant that doesn't hold under Mono.
            if (Type.GetType("Mono.Runtime") != null)
                Assert.Ignore("Skipped on Mono: Timer.Dispose(WaitHandle) signals before in-flight callbacks complete.");

            using var release = new ManualResetEventSlim(false);
            using var callbackEntered = new ManualResetEventSlim(false);

            var timer = new Timer(_ =>
            {
                callbackEntered.Set();
                release.Wait();
            }, null, 0, Timeout.Infinite);

            Assert.IsTrue(callbackEntered.Wait(TimeSpan.FromSeconds(2)),
                "timer callback should have started before we attempt dispose");

            bool result = TimerDisposal.DisposeAndWait(timer, TimeSpan.FromMilliseconds(100));
            Assert.IsFalse(result, "expected timeout while the callback is still running");

            // Sleep gives the leaked handle a window to be touched.
            // If we reach the next line without crashing, the leak held.
            release.Set();
            Thread.Sleep(300);

            var fresh = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            Assert.IsTrue(TimerDisposal.DisposeAndWait(fresh, TimeSpan.FromSeconds(2)),
                "subsequent calls must remain functional after a leak");
        }
    }
}
