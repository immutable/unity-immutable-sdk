#nullable enable

using System;
using System.Threading;

namespace Immutable.Audience
{
    internal static class TimerDisposal
    {
        // Disposes the timer and waits up to timeout for callbacks to finish.
        // - Finished in time: clean up normally.
        // - Timed out: leave the wait handle behind. Disposing it would race
        //   the timer's own cleanup signal and crash the process. The
        //   garbage collector reclaims it later.
        internal static bool DisposeAndWait(Timer? timer, TimeSpan timeout)
        {
            if (timer is null) return true;

            var handle = new ManualResetEvent(false);
            bool ownsHandle = true;
            try
            {
                if (!timer.Dispose(handle))
                {
                    // Already disposed — no signal to wait for.
                    return true;
                }

                if (handle.WaitOne(timeout))
                {
                    return true;
                }

                // Timeout: leak the handle (see preamble).
                ownsHandle = false;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            finally
            {
                if (ownsHandle) handle.Dispose();
            }
        }
    }
}
