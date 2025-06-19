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

        /// <summary>
        /// Possible reponses:
        /// <list type="bullet">
        /// <item>
        /// <description>Success: 1 or 0x1</description>
        /// </item>
        /// <item>
        /// <description>Failure: 0 or 0x0</description>
        /// </item>
        /// </summary>
        public string status;

        public string to;

        public string hash;

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
