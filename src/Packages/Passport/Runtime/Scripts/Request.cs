using System.Collections;
using System.Collections.Generic;
using Immutable.Passport.Auth;

namespace Immutable.Passport
{
    class Request {
        public string fxName;
        public string requestId;
        public string? data;

        public Request(string fxName, string requestId, string? data) {
            this.fxName = fxName;
            this.requestId = requestId;
            this.data = data;
        }
    }

    public class GetImxProviderRequest {
        public string idToken;
        public string accessToken;
        public string? refreshToken;
        public UserProfile? profile;
        public string? etherKey;

        public GetImxProviderRequest(string idToken, string accessToken, string? refreshToken, UserProfile? profile, string? etherKey) {
            this.idToken = idToken;
            this.accessToken = accessToken;
            this.refreshToken = refreshToken;
            this.profile = profile;
            this.etherKey = etherKey;
        }
    }
}

