namespace Immutable.Passport.Model
{
    public class TransactionReceiptResponse
    {
        public string blockHash;

        public string blockNumber;

        public string contractAddress;

        public string cumulativeGasUsed;

        public string effectiveGasPrice;

        public string from;

        public string gasUsed;

        public Log[] logs;

        public string logsBloom;

        public string status;

        public string to;

        public string transactionHash;

        public string transactionIndex;

        public string type;

        public class Log
        {
            public string address;

            public string[] topics;

            public string data;

            public string blockNumber;

            public string transactionHash;

            public string transactionIndex;

            public string blockHash;

            public string logIndex;

            public bool removed;
        }
    }
}
