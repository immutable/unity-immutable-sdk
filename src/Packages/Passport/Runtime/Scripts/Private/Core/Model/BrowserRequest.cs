using System;

namespace Immutable.Passport.Core
{
    [Serializable]
    public class BrowserRequest
    {
        public string fxName;
        public string requestId;
        public string? data;

        public BrowserRequest(string fxName, string requestId, string? data)
        {
            this.fxName = fxName;
            this.requestId = requestId;
            this.data = data;
        }
    }
}

