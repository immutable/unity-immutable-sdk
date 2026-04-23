#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Immutable.Audience
{
    // Thread-safe, disk-persistent batch event queue.
    // Enqueue is lock-free and safe from any thread. A background drain
    // thread moves events from the in-memory ConcurrentQueue to DiskStore,
    // flushing on a time interval or when the batch reaches FlushSize.
    // Call Shutdown before process exit.
    internal sealed class EventQueue : IDisposable
    {
        private readonly DiskStore _store;
        private readonly int _flushIntervalMs;
        private readonly int _flushSize;

        // Dictionaries rather than serialised strings: Json.Serialize runs on the drain thread.
        private readonly ConcurrentQueue<Dictionary<string, object>> _memory
            = new ConcurrentQueue<Dictionary<string, object>>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Thread _drainThread;
        private readonly ManualResetEventSlim _flushGate = new ManualResetEventSlim(false);

        // Serialises drain vs PurgeAll / ApplyAnonymousDowngrade. Without it, a
        // TryDequeue'd event could hit disk after DeleteAll already cleared it.
        private readonly object _drainLock = new object();

        private volatile bool _disposed;

        // store: destination for drained events.
        // flushIntervalSeconds: how often to drain to disk regardless of batch size.
        // flushSize: drain to disk immediately when this many events are queued.
        internal EventQueue(DiskStore store, int flushIntervalSeconds, int flushSize)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _flushIntervalMs = Math.Max(1, flushIntervalSeconds) * 1000;
            _flushSize = Math.Max(1, flushSize);

            _drainThread = new Thread(DrainLoop)
            {
                IsBackground = true,
                Name = "imtbl-audience-drain"
            };
            _drainThread.Start();
        }

        // Enqueues a message dictionary. Lock-free; safe from any thread.
        // The dictionary is not copied -- callers must not mutate it after
        // enqueue. Serialisation happens on the drain thread so Track() stays
        // allocation-light.
        internal void Enqueue(Dictionary<string, object>? msg)
        {
            if (_disposed || msg == null) return;

            _memory.Enqueue(msg);

            // Signal the drain thread early if we've hit the flush-size threshold
            if (_memory.Count >= _flushSize)
                _flushGate.Set();
        }

        // Queues the message under the drain lock. The caller supplies a
        // transform that runs while the lock is held — it can edit the
        // message or return null to drop it. Running under the lock means
        // PurgeAll and ApplyAnonymousDowngrade can't slip in mid-decision, so
        // a Track that races a consent downgrade gets its userId stripped or
        // the message dropped before it reaches the queue.
        internal void EnqueueChecked(
            Dictionary<string, object>? msg,
            Func<Dictionary<string, object>, Dictionary<string, object>?>? transform)
        {
            if (_disposed || msg == null) return;

            lock (_drainLock)
            {
                if (transform != null)
                {
                    var transformed = transform(msg);
                    if (transformed == null) return;
                    msg = transformed;
                }
                _memory.Enqueue(msg);
            }

            if (_memory.Count >= _flushSize)
                _flushGate.Set();
        }

        // Drains the in-memory queue and persists all events to disk
        // immediately. Blocks until the drain is complete.
        internal void FlushSync()
        {
            DrainMemoryToDisk();
        }

        // Discards every pending event, in-memory and on disk. Used on
        // consent revocation.
        internal void PurgeAll()
        {
            // Hold _drainLock so the background drain can't sneak a TryDequeue'd
            // event onto disk after our DeleteAll. See _drainLock declaration for
            // the full race description.
            lock (_drainLock)
            {
                while (_memory.TryDequeue(out _)) { }
                _store.DeleteAll();
            }
        }

        // Synchronous: a Task.Run offload would race HttpTransport, which
        // does not take _drainLock, opening a window where userId-bearing
        // track files could ship during the rewrite.
        internal void ApplyAnonymousDowngrade()
        {
            lock (_drainLock)
            {
                // Drain any pending in-memory events first so they hit disk and
                // get the same filtering as everything already persisted.
                while (_memory.TryDequeue(out var msg))
                {
                    try { _store.Write(Json.Serialize(msg)); }
                    catch (IOException) { /* best-effort */ }
                    catch (UnauthorizedAccessException) { /* best-effort */ }
                }

                _store.ApplyAnonymousDowngrade();
            }
        }

        // Flushes all pending events to disk and stops the drain thread.
        // Safe to call multiple times.
        internal void Shutdown()
        {
            if (_disposed) return;

            // Stop accepting new events first — closes the race window where
            // events enqueued between Cancel and final drain would be lost.
            _disposed = true;

            // Signal the drain thread to exit, then wait for it.
            _cts.Cancel();
            _flushGate.Set(); // Wake drain thread so it exits promptly
            _drainThread.Join(TimeSpan.FromSeconds(5));

            // Final drain: anything enqueued before _disposed was set.
            DrainMemoryToDisk();
        }

        // -----------------------------------------------------------------
        // Background drain loop
        // -----------------------------------------------------------------

        private void DrainLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                // Wait for flush gate or interval timeout
                _flushGate.Wait(_flushIntervalMs);
                _flushGate.Reset();

                if (_cts.IsCancellationRequested)
                    break;

                DrainMemoryToDisk();
            }
        }

        private void DrainMemoryToDisk()
        {
            // Take _drainLock so PurgeAll can't run between our TryDequeue and Write
            // and leave the just-written event orphaned on disk after the wipe.
            lock (_drainLock)
            {
                while (_memory.TryDequeue(out var msg))
                {
                    try
                    {
                        // Serialise on the drain thread, not on the caller thread —
                        // keeps Track() lock-free and allocation-light.
                        _store.Write(Json.Serialize(msg));
                    }
                    catch (IOException)
                    {
                        // Best-effort: if we can't write, discard rather than block the drain
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // IDisposable
        // -----------------------------------------------------------------

        public void Dispose()
        {
            Shutdown();
            _cts.Dispose();
            _flushGate.Dispose();
        }
    }
}
