using System.Collections;
using System.Collections.Generic;
using Immutable.Passport.Auth;

namespace Immutable.Passport
{
    public class Request
    {
        public string fxName;
        public string requestId;
        public string? data;

        public Request(string fxName, string requestId, string? data)
        {
            this.fxName = fxName;
            this.requestId = requestId;
            this.data = data;
        }
    }

    internal class ConfirmCodeRequest
    {
        public string deviceCode;
        public int interval;
        public long? timeoutMs;
    }
}

