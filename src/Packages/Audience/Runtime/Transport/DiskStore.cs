using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Immutable.Audience
{
    /// <summary>
    /// File-per-event persistent store. Each event is written as an atomic
    /// <c>{ticks}_{uuid}.json</c> file inside <c>imtbl_audience/queue/</c>.
    /// </summary>
    internal sealed class DiskStore
    {
        private readonly string _queueDir;

        internal DiskStore(string persistentDataPath)
        {
            _queueDir = Path.Combine(persistentDataPath, "imtbl_audience", "queue");
            Directory.CreateDirectory(_queueDir);
        }

        /// <summary>Atomically writes <paramref name="json"/> as a new event file.</summary>
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
                // Destination already exists (unlikely but safe to handle)
                File.Delete(finalPath);
                File.Move(tmpPath, finalPath);
            }
        }

        /// <summary>
        /// Returns up to <paramref name="maxSize"/> file paths, oldest first.
        /// Files older than <see cref="Constants.StaleEventDays"/> days are deleted and excluded.
        /// </summary>
        internal IReadOnlyList<string> ReadBatch(int maxSize)
        {
            if (maxSize <= 0)
                return Array.Empty<string>();

            maxSize = Math.Min(maxSize, Constants.MaxBatchSize);

            var cutoff = DateTime.UtcNow.AddDays(-Constants.StaleEventDays);

            var result = new List<string>();

            // Sort by filename (ticks prefix) → oldest first
            var files = Directory.GetFiles(_queueDir, "*.json")
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal);

            foreach (var path in files)
            {
                if (result.Count >= maxSize)
                    break;

                // Stale check: parse ticks from filename prefix
                var name = Path.GetFileNameWithoutExtension(path);
                var underscoreIdx = name.IndexOf('_');
                if (underscoreIdx > 0 && long.TryParse(name.Substring(0, underscoreIdx), out var ticks))
                {
                    var fileTime = new DateTime(ticks, DateTimeKind.Utc);
                    if (fileTime < cutoff)
                    {
                        TryDelete(path);
                        continue;
                    }
                }

                result.Add(path);
            }

            return result;
        }

        /// <summary>Deletes the event files at <paramref name="paths"/>.</summary>
        internal void Delete(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDelete(path);
        }

        /// <summary>Returns the total number of event files currently on disk.</summary>
        internal int Count() => Directory.GetFiles(_queueDir, "*.json").Length;

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
