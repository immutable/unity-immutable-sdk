using System;

namespace Immutable.Passport.Model
{

    [Serializable]
    internal class ConnectPKCERequest
    {
        public string authorizationCode;
        public string state;
    }
}

