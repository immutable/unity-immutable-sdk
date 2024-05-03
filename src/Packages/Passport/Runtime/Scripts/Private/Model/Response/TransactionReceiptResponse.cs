namespace Immutable.Passport.Model
{
    // GetTransactionReceipt docs
    // https://docs.immutable.com/docs/zkEVM/products/passport/wallet/rpc-methods/eth_getTransactionReceipt#example

    // GetTransactionReceipt response example
    // {
    //   "blockHash": "0xc72ec8cd9bc04238ada35cc13b59b4aee19a08fb8a79611e11c92fdc5bda184b",
    //   "blockNumber": "0x78091c",
    //   "contractAddress": null,
    //   "cumulativeGasUsed": "0x6ce6",
    //   "effectiveGasPrice": "0x2540be431",
    //   "from": "0x9aa485da96f0a3c3a4b223b81c4f93702b6ad515",
    //   "gasUsed": "0x6ce6",
    //   "logs": [
    //     {
    //       "address": "0x1ccca691501174b4a623ceda58cc8f1a76dc3439",
    //       "topics": [
    //         "0xe1fffcc4923d04b559f4d29a8bfc6cda04eb5b0d3c460751c2402c5c5cc9109c",
    //         "0x0000000000000000000000009aa485da96f0a3c3a4b223b81c4f93702b6ad515"
    //       ],
    //       "data": "0x00000000000000000000000000000000000000000000000006f05b59d3b20000",
    //       "blockNumber": "0x78091c",
    //       "transactionHash": "0x3b1afb55feda7a6831931acf527c86edc99e8227272b5f6283eef1ea87780560",
    //       "transactionIndex": "0x0",
    //       "blockHash": "0xc72ec8cd9bc04238ada35cc13b59b4aee19a08fb8a79611e11c92fdc5bda184b",
    //       "logIndex": "0x0",
    //       "removed": false
    //     }
    //   ],
    //   "logsBloom": "0x00000000000000400000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000002000000000000000000000000000000000000000000000000000000000000000000008000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002000000000000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000040000000000000000000000000000000000000000400000000000000000",
    //   "status": "0x1", // 0x1 means success, 0x0 means failure
    //   "to": "0x1ccca691501174b4a623ceda58cc8f1a76dc3439",
    //   "transactionHash": "0x3b1afb55feda7a6831931acf527c86edc99e8227272b5f6283eef1ea87780560",
    //   "transactionIndex": "0x0",
    //   "type": "0x2"
    // }    
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
