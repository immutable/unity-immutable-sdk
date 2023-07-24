using Newtonsoft.Json;

namespace Immutable.Passport.Model
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
        [JsonProperty(Required = Required.Always)]
        public string accessToken;
        public string? refreshToken;
        [JsonProperty(Required = Required.Always)]
        public string idToken;
        [JsonProperty(Required = Required.Always)]
        public string tokenType;
        [JsonProperty(Required = Required.Always)]
        public int expiresIn;
    }
#pragma warning restore CS8618
#pragma warning restore IDE1006
}