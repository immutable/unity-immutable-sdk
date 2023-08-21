using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class CreateBatchTransferResponse
    {
        public long[] TransferIds { get; }

        public CreateBatchTransferResponse(long[] transferIds)
        {
            this.TransferIds = transferIds;
        }
    }
}
