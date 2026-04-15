using System;
using System.IO;

namespace Immutable.Audience
{
    internal sealed class Identity
    {
        private static volatile string _cachedId;

        private static string GetDirectory(string persistentDataPath) =>
            Path.Combine(persistentDataPath, "imtbl_audience");

        private static string GetFilePath(string persistentDataPath) =>
            Path.Combine(GetDirectory(persistentDataPath), "identity");

        internal static string GetOrCreate(string persistentDataPath, ConsentLevel consent)
        {
            if (consent == ConsentLevel.None)
                return null;

            if (_cachedId != null)
                return _cachedId;

            var dir = GetDirectory(persistentDataPath);
            Directory.CreateDirectory(dir);

            var filePath = GetFilePath(persistentDataPath);

            if (File.Exists(filePath))
            {
                var existing = File.ReadAllText(filePath).Trim();
                _cachedId = existing;
                return _cachedId;
            }

            var newId = Guid.NewGuid().ToString();
            var tmpPath = filePath + ".tmp";
            File.WriteAllText(tmpPath, newId);

            try
            {
                File.Move(tmpPath, filePath);
            }
            catch (IOException)
            {
                // Destination already exists (race condition) — delete it and retry
                File.Delete(filePath);
                File.Move(tmpPath, filePath);
            }

            _cachedId = newId;
            return _cachedId;
        }

        internal static void Reset(string persistentDataPath)
        {
            _cachedId = null;

            var filePath = GetFilePath(persistentDataPath);
            try
            {
                File.Delete(filePath);
            }
            catch (FileNotFoundException)
            {
                // Nothing to delete — this is fine
            }
        }
    }
}
