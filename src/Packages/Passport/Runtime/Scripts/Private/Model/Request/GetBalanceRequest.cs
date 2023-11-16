using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class GetBalanceRequest
    {
        public string address;
        public string blockNumberOrTag;
    }
}