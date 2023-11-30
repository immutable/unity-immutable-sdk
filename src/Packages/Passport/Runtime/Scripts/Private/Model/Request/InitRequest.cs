using System;
using Immutable.Passport.Model;

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

    [Serializable]
    internal class InitRequest
    {
        public string clientId;
        public string environment;
        public VersionInfo engineVersion;
    }
}
