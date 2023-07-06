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

    internal class GetImxProviderRequest
    {
        public string idToken;
        public string accessToken;
        public string? refreshToken;
        public UserProfile? profile;
        public UserImx? imx;

        public GetImxProviderRequest(string idToken, string accessToken, string? refreshToken, UserProfile? profile, string? ethAddress)
        {
            this.idToken = idToken;
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;
            this.profile = profile;
            this.imx = new UserImx()
            {
                ethAddress = ethAddress
            };
        }
    }
}

