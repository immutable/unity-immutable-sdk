using System.IO;

namespace Immutable.Audience
{
    internal static class AudiencePaths
    {
        private const string RootDirName = "imtbl_audience";
        private const string IdentityFileName = "identity";
        private const string ConsentFileName = "consent";
        private const string QueueDirName = "queue";

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
