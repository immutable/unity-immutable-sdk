using System;

namespace Immutable.Passport.Core
{
    [Serializable]
    public class BrowserRequest
    {
        public string fxName;
        public string requestId;
        public string data;
    }
}

