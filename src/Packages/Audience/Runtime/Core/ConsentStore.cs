#nullable enable

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
            var tmpPath = filePath + AudiencePaths.TempFileSuffix;

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
                // Range check is reflection-free (Enum.IsDefined uses reflection; under
                // Unity 6's tighter IL2CPP stripping the metadata it walks may be
                // stripped even with link.xml preserve="all" on the assembly).
                if (int.TryParse(text, out var raw)
                    && raw >= (int)ConsentLevel.None && raw <= (int)ConsentLevel.Full)
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
