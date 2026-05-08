using System.IO;

namespace Immutable.Audience
{
    internal static class AudiencePaths
    {
        private const string RootDirName = "imtbl_audience";
        private const string IdentityFileName = "identity";
        private const string ConsentFileName = "consent";
        private const string QueueDirName = "queue";
        private const string InstallReferrerFileName = "install_referrer";
        private const string InstallReferrerSentFileName = "install_referrer_sent";
        private const string GAIDFileName = "gaid";

        internal static string AudienceDir(string persistentDataPath) =>
            Path.Combine(persistentDataPath, RootDirName);

        internal static string IdentityFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), IdentityFileName);

        internal static string ConsentFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), ConsentFileName);

        internal static string QueueDir(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), QueueDirName);

        internal static string InstallReferrerFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), InstallReferrerFileName);

        internal static string InstallReferrerSentFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), InstallReferrerSentFileName);

        internal static string GAIDFile(string persistentDataPath) =>
            Path.Combine(AudienceDir(persistentDataPath), GAIDFileName);
    }
}
