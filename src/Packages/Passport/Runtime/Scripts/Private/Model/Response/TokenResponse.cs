using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class TokenResponse
    {
        public string accessToken;
        public string refreshToken;
        public string idToken;
        public string tokenType;
        public int expiresIn;
    }
}