using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class VersionInfo
    {
        public string engine;
        public string engineVersion;
        public string platform;
        public string platformVersion;
    }
}
