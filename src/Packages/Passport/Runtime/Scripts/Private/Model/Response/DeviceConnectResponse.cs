using Immutable.Passport.Core;

namespace Immutable.Passport.Model
{
    public class DeviceConnectResponse : BrowserResponse
    {
        public string code;
        public string deviceCode;
        public int interval;
        public string url;
    }
}