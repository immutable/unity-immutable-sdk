using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    internal class ConfirmCodeRequest
    {
        public string deviceCode;
        public int interval;
        public long? timeoutMs;
    }
}

