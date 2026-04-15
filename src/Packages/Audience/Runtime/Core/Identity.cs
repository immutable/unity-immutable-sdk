using System;
using System.IO;

namespace Immutable.Audience
{
    // Manages the anonymous ID that identifies a device across sessions.
    // The ID is a UUID generated once, written to disk, and reused on every subsequent launch.
    internal sealed class Identity
    {
        // In-memory cache — volatile so background threads always see the latest write.
        private static volatile string _cachedId;
        private static readonly object _sync = new object();

        private static string GetDirectory(string persistentDataPath) =>
            Path.Combine(persistentDataPath, "imtbl_audience");

        private static string GetFilePath(string persistentDataPath) =>
            Path.Combine(GetDirectory(persistentDataPath), "identity");

        // Returns the anonymous ID, generating and persisting it on first call.
        // Returns null without touching disk when consent is None.
        // Safe to call from any thread after Init() has run on the main thread.
        internal static string GetOrCreate(string persistentDataPath, ConsentLevel consent)
        {
            // No ID until the player grants at least anonymous consent.
            if (consent == ConsentLevel.None)
                return null;

            // Fast path — already loaded this session, no lock needed.
            if (_cachedId != null)
                return _cachedId;

            // Slow path — first call or after Reset(). Only one thread does the work.
            lock (_sync)
            {
                // Re-check after acquiring the lock in case another thread beat us here.
                if (_cachedId != null)
                    return _cachedId;

                var dir = GetDirectory(persistentDataPath);
                Directory.CreateDirectory(dir); // no-op if already exists

                var filePath = GetFilePath(persistentDataPath);

                // Returning player — read the ID we wrote on a previous launch.
                if (File.Exists(filePath))
                {
                    _cachedId = File.ReadAllText(filePath).Trim();
                    return _cachedId;
                }

                // New install — generate a UUID and persist it atomically.
                // Write to a .tmp file first so a crash mid-write leaves no corrupt file.
                var newId = Guid.NewGuid().ToString();
                var tmpPath = filePath + ".tmp";
                File.WriteAllText(tmpPath, newId);

                try
                {
                    File.Move(tmpPath, filePath);
                }
                catch (IOException)
                {
                    // Unexpected — file appeared between our Exists check and Move (shouldn't happen in practice).
                    // Delete and retry to ensure a clean state.
                    File.Delete(filePath);
                    File.Move(tmpPath, filePath);
                }

                _cachedId = newId;
                return _cachedId;
            }
        }

        // Clears the cached ID and deletes the persisted file.
        // Called on logout or when consent is downgraded to None.
        // The next GetOrCreate call will generate a fresh ID.
        internal static void Reset(string persistentDataPath)
        {
            _cachedId = null;

            var filePath = GetFilePath(persistentDataPath);
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // File was never written (e.g. consent was None) — nothing to do.
            }
        }
    }
}
