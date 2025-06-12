using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    internal class InitRequestWithRedirectUri
    {
        public string clientId;
        public string environment;
        public string redirectUri;
        public string logoutRedirectUri;
        public VersionInfo engineVersion;
    }
}
