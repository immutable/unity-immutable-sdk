using System.Collections.Generic;
using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    internal class TrackData
    {
        public string moduleName;
        public string eventName;
        public string? properties;
    }
}

