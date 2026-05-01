using System.IO;

namespace Immutable.Audience
{
    internal static class AudiencePaths
    {
        private const string RootDirName = "imtbl_audience";
        private const string IdentityFileName = "identity";
        private const string ConsentFileName = "consent";
        private const string QueueDirName = "queue";

        // Queue files are named <ticks>_<uuid>.json.
        internal const string QueueFileExtension = ".json";
        internal const string QueueGlob = "*" + QueueFileExtension;

        // Files ending in this suffix are mid-write and must not be read.
        internal const string TempFileSuffix = ".tmp";

        internal static string AudienceDir(string persistentDataPath) =>
            Path.Combine(persistentDataPath, RootDirName);

        internal static string IdentityFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), IdentityFileName);

        internal static string ConsentFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), ConsentFileName);

        internal static string QueueDir(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), QueueDirName);
    }
}
