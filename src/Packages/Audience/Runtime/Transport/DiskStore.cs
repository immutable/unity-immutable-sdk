#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Immutable.Audience
{
    // File-per-event persistent store. Each event is written as an atomic
    // {ticks}_{uuid}.json file inside imtbl_audience/queue/.
    internal sealed class DiskStore
    {
        private readonly string _queueDir;

        internal DiskStore(string persistentDataPath)
        {
            _queueDir = Path.Combine(persistentDataPath, "imtbl_audience", "queue");
            Directory.CreateDirectory(_queueDir);
        }

        // Atomically writes json as a new event file.
        internal void Write(string json)
        {
            var fileName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}.json";
            var finalPath = Path.Combine(_queueDir, fileName);
            var tmpPath = finalPath + ".tmp";

            File.WriteAllText(tmpPath, json);

            try
            {
                File.Move(tmpPath, finalPath);
            }
            catch (IOException)
            {
                File.Delete(finalPath);
                File.Move(tmpPath, finalPath);
            }
        }

        // Returns up to maxSize file paths, oldest first. Files outside the
        // backend's accepted eventTimestamp window (more than StaleEventDays
        // in the past, or more than MaxClockSkewFutureHours in the future --
        // e.g. from a device with a badly-skewed system clock) are deleted
        // and excluded: the backend would reject them anyway.
        internal IReadOnlyList<string> ReadBatch(int maxSize)
        {
            if (maxSize <= 0)
                return Array.Empty<string>();

            maxSize = Math.Min(maxSize, Constants.MaxBatchSize);

            var now = DateTime.UtcNow;
            var pastCutoff = now.AddDays(-Constants.StaleEventDays);
            var futureCutoff = now.AddHours(Constants.MaxClockSkewFutureHours);

            var result = new List<string>();

            // Sort by filename (ticks prefix), oldest first.
            // Missing queue dir is empty queue. Matches DeleteAll.
            string[] paths;
            try { paths = Directory.GetFiles(_queueDir, "*.json"); }
            catch (DirectoryNotFoundException) { return Array.Empty<string>(); }

            var files = paths.OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal);

            foreach (var path in files)
            {
                if (result.Count >= maxSize)
                    break;

                // Window check: parse ticks from filename prefix
                var name = Path.GetFileNameWithoutExtension(path);
                var underscoreIdx = name.IndexOf('_');
                if (underscoreIdx > 0 && long.TryParse(name.Substring(0, underscoreIdx), out var ticks))
                {
                    var fileTime = new DateTime(ticks, DateTimeKind.Utc);
                    if (fileTime < pastCutoff || fileTime > futureCutoff)
                    {
                        TryDelete(path);
                        continue;
                    }
                }

                result.Add(path);
            }

            return result;
        }

        // Deletes the given event files.
        internal void Delete(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDelete(path);
        }

        // Total number of event files currently on disk. Reads the filesystem
        // each call so concurrent Write / Delete from any thread is reflected
        // without a separately maintained counter that could drift.
        internal int Count()
        {
            try { return Directory.GetFiles(_queueDir, "*.json").Length; }
            catch (DirectoryNotFoundException) { return 0; }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        internal void DeleteAll()
        {
            string[] paths;
            try { paths = Directory.GetFiles(_queueDir, "*.json"); }
            catch (DirectoryNotFoundException) { return; }

            foreach (var path in paths)
                TryDelete(path);
        }

    }
}
