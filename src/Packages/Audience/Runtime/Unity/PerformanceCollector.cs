#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Immutable.Audience.Unity
{
    // fps + memory for session_heartbeat. RecordFrame: main thread, SnapshotAndReset: thread-safe.
    internal sealed class PerformanceCollector
    {
        private readonly object _lock = new object();

        private int _frameCount;
        private float _elapsed;
        private float _fpsMin = float.MaxValue;
        private long _memUsedMb;
        private long _memReservedMb;

        private bool _memorySampleRequested = true;

        internal void RecordFrame()
        {
            lock (_lock)
            {
                var dt = Time.unscaledDeltaTime;
                _frameCount++;
                _elapsed += dt;

                if (dt > 0f)
                {
                    var instantFps = 1f / dt;
                    if (instantFps < _fpsMin) _fpsMin = instantFps;
                }

                if (_memorySampleRequested)
                {
                    _memUsedMb = Profiler.GetTotalAllocatedMemoryLong() / (1024L * 1024L);
                    _memReservedMb = Profiler.GetTotalReservedMemoryLong() / (1024L * 1024L);
                    _memorySampleRequested = false;
                }
            }
        }

        internal Dictionary<string, object> SnapshotAndReset()
        {
            int frames;
            float elapsed;
            float fpsMin;
            long memUsed;
            long memReserved;

            lock (_lock)
            {
                frames = _frameCount;
                elapsed = _elapsed;
                fpsMin = _fpsMin;
                memUsed = _memUsedMb;
                memReserved = _memReservedMb;

                _frameCount = 0;
                _elapsed = 0f;
                _fpsMin = float.MaxValue;
                _memorySampleRequested = true;
            }

            // Memory is a point-in-time reading — always meaningful. fps
            // fields only ship when at least one frame was recorded, so the
            // backend can tell "no sample" (fields absent) apart from
            // "framerate dropped to zero" (fields present with value 0).
            var result = new Dictionary<string, object>
            {
                ["memoryUsedMb"] = memUsed,
                ["memoryReservedMb"] = memReserved,
            };
            if (frames > 0 && elapsed > 0f)
                result["fpsAvg"] = Math.Round(frames / elapsed, 1);
            if (fpsMin != float.MaxValue)
                result["fpsMin"] = Math.Round(fpsMin, 1);
            return result;
        }
    }
}
