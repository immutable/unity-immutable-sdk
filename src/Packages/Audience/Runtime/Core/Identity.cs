#nullable enable

using System;
using System.IO;

namespace Immutable.Audience
{
    // Manages the anonymous ID and device ID for this device.
    // Both are UUIDs persisted to {"anonymousId":"<uuid>","deviceId":"<uuid>"}.
    // deviceId survives RotateAnonymousId (logout); both are wiped by Reset (opt-out).
    // If disk read/write fails, both fall back to in-memory-only ids for the rest
    // of this session rather than leaving anonymousId unset (see LoadOrGenerate).
    //
    // Static caches persist across play sessions in the Unity Editor with domain reload
    // disabled. ImmutableAudience.Init() calls ClearCache() via ResetState() to handle that.
    internal sealed class Identity
    {
        private static volatile string? _cachedAnonId;
        private static volatile string? _cachedDeviceId;
        private static readonly object _sync = new object();

        // Returns the existing anonymous ID without creating one.
        internal static string? Get(string persistentDataPath)
        {
            if (_cachedAnonId != null) return _cachedAnonId;

            lock (_sync)
            {
                if (_cachedAnonId != null) return _cachedAnonId;

                try
                {
                    var filePath = AudiencePaths.IdentityFile(persistentDataPath);
                    if (!File.Exists(filePath)) return null;

                    var content = File.ReadAllText(filePath).Trim();
                    ParseFile(content, out var anonId, out _);
                    _cachedAnonId = anonId;
                    return _cachedAnonId;
                }
                catch (IOException) { return null; }
                catch (UnauthorizedAccessException) { return null; }
            }
        }

        // Clears both in-memory caches without touching disk.
        // Called on Shutdown/ResetState so a subsequent Init with a different
        // persistentDataPath re-reads the file from the new location.
        internal static void ClearCache()
        {
            lock (_sync)
            {
                _cachedAnonId = null;
                _cachedDeviceId = null;
            }
        }

        // Returns the anonymous ID, generating and persisting both IDs on first call.
        // Returns null when consent is None.
        internal static string? GetOrCreate(string persistentDataPath, ConsentLevel consent)
        {
            if (!consent.CanTrack()) return null;

            if (_cachedAnonId != null) return _cachedAnonId;

            lock (_sync)
            {
                if (_cachedAnonId != null) return _cachedAnonId;

                LoadOrGenerate(persistentDataPath);
                return _cachedAnonId;
            }
        }

        // Returns the device ID at Anonymous+ consent, null at None.
        internal static string? GetOrCreateDeviceId(string persistentDataPath, ConsentLevel consent)
        {
            if (!consent.CanTrack()) return null;

            if (_cachedDeviceId != null) return _cachedDeviceId;

            lock (_sync)
            {
                if (_cachedDeviceId != null) return _cachedDeviceId;

                LoadOrGenerate(persistentDataPath);
                return _cachedDeviceId;
            }
        }

        // Logout: rotates anon_id and rewrites the file, preserving device_id.
        internal static void RotateAnonymousId(string persistentDataPath)
        {
            lock (_sync)
            {
                var deviceId = _cachedDeviceId;
                if (deviceId == null)
                {
                    try
                    {
                        var fp = AudiencePaths.IdentityFile(persistentDataPath);
                        if (File.Exists(fp))
                        {
                            ParseFile(File.ReadAllText(fp).Trim(), out _, out deviceId);
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
                }

                _cachedAnonId = null;

                if (string.IsNullOrEmpty(deviceId))
                {
                    // Nothing to preserve: delete so the next GetOrCreate regenerates both fresh.
                    try { File.Delete(AudiencePaths.IdentityFile(persistentDataPath)); }
                    catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException) { }
                    return;
                }

                // Rewrite with a new anon_id, same device_id.
                var newAnonId = Guid.NewGuid().ToString();
                try
                {
                    WriteFile(AudiencePaths.IdentityFile(persistentDataPath), newAnonId, deviceId);
                    _cachedAnonId = newAnonId;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Log.Warn(AudienceLogs.IdentityRotateFailed(ex));
                }
            }
        }

        // Full wipe: clears both IDs and deletes the file. Called on SetConsent(None).
        internal static void Reset(string persistentDataPath)
        {
            lock (_sync)
            {
                _cachedAnonId = null;
                _cachedDeviceId = null;

                var filePath = AudiencePaths.IdentityFile(persistentDataPath);
                try { File.Delete(filePath); }
                catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException) { }
            }
        }

        // Slow path: read from disk or generate fresh IDs. Must be called under _sync.
        private static void LoadOrGenerate(string persistentDataPath)
        {
            var filePath = AudiencePaths.IdentityFile(persistentDataPath);
            string anonId;
            string deviceId;

            try
            {
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath).Trim();
                    ParseFile(content, out var existingAnonId, out var existingDeviceId);

                    if (!string.IsNullOrEmpty(existingAnonId) && !string.IsNullOrEmpty(existingDeviceId))
                    {
                        _cachedAnonId = existingAnonId;
                        _cachedDeviceId = existingDeviceId;
                        return;
                    }

                    // Partial or old plain-string format: keep anon_id, generate device_id, migrate file.
                    anonId = string.IsNullOrEmpty(existingAnonId) ? Guid.NewGuid().ToString() : existingAnonId;
                    deviceId = Guid.NewGuid().ToString();
                }
                else
                {
                    anonId = Guid.NewGuid().ToString();
                    deviceId = Guid.NewGuid().ToString();
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Reading failed, so there's nothing pre-existing to reuse.
                Log.Warn(AudienceLogs.IdentityLoadOrGenerateFailed(ex));
                _cachedAnonId ??= Guid.NewGuid().ToString();
                _cachedDeviceId ??= Guid.NewGuid().ToString();
                return;
            }

            // Cache in memory before attempting to persist: a write failure below must
            // not lose an anonId that was already legitimately read from disk (e.g. a
            // legacy-format file being migrated), and must still guarantee some id for
            // the rest of this session even when nothing existed to begin with.
            _cachedAnonId = anonId;
            _cachedDeviceId = deviceId;

            try
            {
                Directory.CreateDirectory(AudiencePaths.AudienceDir(persistentDataPath));
                WriteFile(filePath, anonId, deviceId);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Warn(AudienceLogs.IdentityLoadOrGenerateFailed(ex));
            }
        }

        // Handles two formats: JSON {"anonymousId":...,"deviceId":...} and the legacy plain UUID string.
        private static void ParseFile(string content, out string? anonId, out string? deviceId)
        {
            anonId = null;
            deviceId = null;

            if (content.StartsWith("{"))
            {
                try
                {
                    var obj = JsonReader.DeserializeObject(content);
                    obj.TryGetValue("anonymousId", out var a);
                    obj.TryGetValue("deviceId", out var d);
                    anonId = a as string;
                    deviceId = d as string;
                    return;
                }
                catch (Exception) { }
            }

            // Legacy plain-UUID format.
            if (!string.IsNullOrEmpty(content))
                anonId = content;
        }

        private static void WriteFile(string filePath, string anonId, string deviceId)
        {
            var json = Json.Serialize(new System.Collections.Generic.Dictionary<string, object>
            {
                ["anonymousId"] = anonId,
                ["deviceId"] = deviceId,
            });
            var tmpPath = filePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            if (File.Exists(filePath))
                File.Replace(tmpPath, filePath, null); // atomic overwrite; no window where the file is absent
            else
                File.Move(tmpPath, filePath);
        }
    }
}
