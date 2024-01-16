using Immutable.Passport.Core;

namespace Immutable.Passport.Model
{
    public class DeviceConnectResponse : BrowserResponse
    {
        public string code;
        public string deviceCode;
        public string url;
        public int interval;
    }
}
