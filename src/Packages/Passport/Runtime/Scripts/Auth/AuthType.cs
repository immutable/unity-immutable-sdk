namespace Immutable.Passport.Auth
{
#pragma warning disable CS8618
#pragma warning disable IDE1006
    public class ConnectResponse
    {
        public string code;
        public string url;
    }

    public class TokenResponse
    {
        public string? accessToken;
        public string? refreshToken;
        public string? idToken;
        public string? tokenType;
        public int? expiresIn;
    }
#pragma warning restore CS8618
#pragma warning restore IDE1006
}