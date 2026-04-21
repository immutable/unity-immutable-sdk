using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentQueue<string> _memory = new ConcurrentQueue<string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Thread _drainThread;
        private readonly ManualResetEventSlim _flushGate = new ManualResetEventSlim(false);

        // Volatile so all threads see the shutdown signal immediately.
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

        // Enqueues a JSON-serialised event. Lock-free; safe from any thread.
        internal void Enqueue(string json)
        {
            if (_disposed) return;

            _memory.Enqueue(json);

            // Signal the drain thread early if we've hit the flush-size threshold
            if (_memory.Count >= _flushSize)
                _flushGate.Set();
        }

        // Drains the in-memory queue and persists all events to disk
        // immediately. Blocks until the drain is complete.
        internal void FlushSync()
        {
            DrainMemoryToDisk();
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
            _flushGate.Set();
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
            while (_memory.TryDequeue(out var json))
            {
                try
                {
                    _store.Write(json);
                }
                catch (Exception)
                {
                    // Best-effort: if we can't write, discard rather than block the drain
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
