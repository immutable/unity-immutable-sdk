#nullable enable

using System;
using System.IO;

namespace Immutable.Audience
{
    internal static class AttStatusStore
    {
        internal static void Save(string persistentDataPath, int status)
        {
            var dir = AudiencePaths.AudienceDir(persistentDataPath);
            Directory.CreateDirectory(dir);
            var filePath = AudiencePaths.AttStatusFile(persistentDataPath);
            var tmpPath = filePath + ".tmp";
            File.WriteAllText(tmpPath, status.ToString());
            try
            {
                File.Move(tmpPath, filePath);
            }
            catch (IOException)
            {
                File.Delete(filePath);
                File.Move(tmpPath, filePath);
            }
        }

        // Returns null on missing/malformed/unreadable file.
        internal static int? Load(string persistentDataPath)
        {
            try
            {
                var filePath = AudiencePaths.AttStatusFile(persistentDataPath);
                if (!File.Exists(filePath)) return null;
                var text = File.ReadAllText(filePath).Trim();
                if (int.TryParse(text, out var raw) && raw >= 0 && raw <= 3)
                    return raw;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return null;
        }
    }
}
