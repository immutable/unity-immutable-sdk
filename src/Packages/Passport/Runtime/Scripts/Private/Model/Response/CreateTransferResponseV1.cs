using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class CreateTransferResponseV1
    {
        public string SentSignature { get; }

        public string Receiver { get; }

        public string Status { get; }

        public long Time { get; }

        public long TransferId { get; }
    }
}
