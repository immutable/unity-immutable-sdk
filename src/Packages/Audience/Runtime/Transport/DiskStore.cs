#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                // Destination already exists (unlikely but safe to handle)
                File.Delete(finalPath);
                File.Move(tmpPath, finalPath);
            }
        }

        // Returns up to maxSize file paths, oldest first. Stale files
        // (older than Constants.StaleEventDays) are deleted and excluded.
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

        // Deletes the given event files.
        internal void Delete(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDelete(path);
        }

        // Total number of event files currently on disk.
        internal int Count() => Directory.GetFiles(_queueDir, "*.json").Length;

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
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

        // Drops queued identify/alias files, strips userId from track files.
        // Unparseable files are deleted to fail closed.
        internal void ApplyAnonymousDowngrade()
        {
            string[] paths;
            try { paths = Directory.GetFiles(_queueDir, "*.json"); }
            catch (DirectoryNotFoundException) { return; }

            foreach (var path in paths)
                ApplyAnonymousDowngradeToFile(path);
        }

        private void ApplyAnonymousDowngradeToFile(string path)
        {
            if (!TryReadMessage(path, out var msg) ||
                !msg.TryGetValue(MessageFields.Type, out var typeObj) ||
                !(typeObj is string type))
            {
                TryDelete(path);
                return;
            }

            if (IsIdentityMessage(type))
            {
                TryDelete(path);
                return;
            }

            if (type == MessageTypes.Track && msg.ContainsKey(MessageFields.UserId))
                RewriteTrackWithoutUserId(path, msg);
        }

        private static bool IsIdentityMessage(string type) =>
            type == MessageTypes.Identify || type == MessageTypes.Alias;

        private static bool TryReadMessage(string path, [NotNullWhen(true)] out Dictionary<string, object>? msg)
        {
            msg = null;
            string json;
            try { json = File.ReadAllText(path); }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }

            try { msg = JsonReader.DeserializeObject(json); }
            catch (FormatException) { return false; }

            return true;
        }

        private void RewriteTrackWithoutUserId(string path, Dictionary<string, object> msg)
        {
            msg.Remove(MessageFields.UserId);

            try
            {
                var rewritten = Json.Serialize(msg);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, rewritten);
                try { File.Move(tmp, path); }
                catch (IOException)
                {
                    File.Delete(path);
                    File.Move(tmp, path);
                }
            }
            catch (IOException)
            {
                // Delete rather than leave the old userId-bearing payload.
                TryDelete(path);
            }
            catch (UnauthorizedAccessException)
            {
                TryDelete(path);
            }
        }

    }
}
