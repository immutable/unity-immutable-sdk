using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Immutable.Passport.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TransactionRequest
    {
        public string? To;
        public string? From;
        public string? Nonce;

        public string? GasLimit;
        public string? GasPrice;

        public string? Data;
        public string? Value;
        public int? ChainId;

        public int? Type;
        public AccessListItem[]? AccessList;

        public string? MaxPriorityFeePerGas;
        public string? MaxFeePerGas;

        public CustomData[]? CustomData;
        public bool? CcipReadEnabled;
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AccessListItem
    {
        public string Address;
        public string[] StorageKeys;
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CustomData
    {
        public string Key;
        public object Value;
    }
}