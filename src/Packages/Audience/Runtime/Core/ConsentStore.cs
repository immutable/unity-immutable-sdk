using System;
using System.IO;

namespace Immutable.Audience
{
    internal static class ConsentStore
    {
        internal static void Save(string persistentDataPath, ConsentLevel level)
        {
            Directory.CreateDirectory(AudiencePaths.AudienceDir(persistentDataPath));

            var filePath = AudiencePaths.ConsentFile(persistentDataPath);
            var tmpPath = filePath + ".tmp";

            File.WriteAllText(tmpPath, ((int)level).ToString());

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

        // Returns null on missing/malformed/unreadable file; caller falls back to config default.
        internal static ConsentLevel? Load(string persistentDataPath)
        {
            try
            {
                var filePath = AudiencePaths.ConsentFile(persistentDataPath);
                if (!File.Exists(filePath)) return null;

                var text = File.ReadAllText(filePath).Trim();
                if (int.TryParse(text, out var raw) && Enum.IsDefined(typeof(ConsentLevel), raw))
                    return (ConsentLevel)raw;
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
