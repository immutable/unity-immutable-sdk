#nullable enable

using System;
using System.Collections.Generic;

namespace Immutable.Audience
{
    /// <summary>
    /// Per-exception-type token bucket. Keeps one repeating exception from
    /// flooding the event queue without silencing a different, unrelated one.
    /// </summary>
    /// <remarks>
    /// Each exception type gets its own bucket, starting full. A capture
    /// spends one token; the bucket refills by one token per
    /// <c>refillInterval</c>, capped at <c>capacity</c>. A type that keeps
    /// recurring settles into a steady trickle instead of going silent
    /// once, or drowning out a different type sharing one global count.
    /// </remarks>
    internal sealed class ExceptionRateLimiter
    {
        private readonly int _capacity;
        private readonly TimeSpan _refillInterval;
        private readonly Func<DateTime> _now;
        private readonly Dictionary<string, Bucket> _buckets = new Dictionary<string, Bucket>();
        private readonly object _lock = new object();

        internal ExceptionRateLimiter(int capacity, TimeSpan refillInterval, Func<DateTime>? now = null)
        {
            _capacity = capacity;
            _refillInterval = refillInterval;
            _now = now ?? (() => DateTime.UtcNow);
        }

        /// <summary>
        /// Attempts to spend one token for <paramref name="exceptionType"/>.
        /// </summary>
        /// <returns>
        /// <c>allowed</c>: true if a token was spent and the caller should proceed.
        /// <c>firstTimeLimited</c>: true the moment this type's bucket runs out,
        /// so the caller can log a warning once per exhaustion episode rather
        /// than once per dropped exception.
        /// </returns>
        internal (bool allowed, bool firstTimeLimited) TryConsume(string exceptionType)
        {
            var now = _now();
            lock (_lock)
            {
                if (!_buckets.TryGetValue(exceptionType, out var bucket))
                {
                    bucket = new Bucket { Tokens = _capacity, LastRefill = now };
                    _buckets[exceptionType] = bucket;
                }
                else
                {
                    Refill(bucket, now);
                }

                if (bucket.Tokens < 1)
                {
                    var firstTimeLimited = !bucket.HasWarned;
                    bucket.HasWarned = true;
                    return (false, firstTimeLimited);
                }

                bucket.Tokens -= 1;
                bucket.HasWarned = false;
                return (true, false);
            }
        }

        private void Refill(Bucket bucket, DateTime now)
        {
            var elapsed = now - bucket.LastRefill;
            if (elapsed <= TimeSpan.Zero) return;

            var tokensToAdd = elapsed.Ticks / (double)_refillInterval.Ticks;
            if (tokensToAdd <= 0) return;

            bucket.Tokens = Math.Min(_capacity, bucket.Tokens + tokensToAdd);
            bucket.LastRefill = now;
        }

        internal void Reset()
        {
            lock (_lock) { _buckets.Clear(); }
        }

        private sealed class Bucket
        {
            internal double Tokens;
            internal DateTime LastRefill;
            internal bool HasWarned;
        }
    }
}
