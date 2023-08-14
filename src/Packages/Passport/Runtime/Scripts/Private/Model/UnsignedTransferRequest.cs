using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class UnsignedTransferRequest
    {
        public Token Token { get; }

        public string Receiver { get; }

        public UnsignedTransferRequest(Token token, string receiver)
        {
            this.Token = token;
            this.Receiver = receiver;
        }
    }
}