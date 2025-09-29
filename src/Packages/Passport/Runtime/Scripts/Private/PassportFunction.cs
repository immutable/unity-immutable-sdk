namespace Immutable.Passport
{
    public static class PassportFunction
    {
        public const string INIT = "init";
        public const string RELOGIN = "relogin";
        public const string RECONNECT = "reconnect";
        public const string LOGIN_PKCE = "loginPKCE";
        public const string CONNECT_PKCE = "connectPKCE";
        public const string GET_PKCE_AUTH_URL = "getPKCEAuthUrl";
        public const string GET_ACCESS_TOKEN = "getAccessToken";
        public const string GET_ID_TOKEN = "getIdToken";
        public const string LOGOUT = "logout";
        public const string GET_EMAIL = "getEmail";
        public const string GET_PASSPORT_ID = "getPassportId";
        public const string GET_LINKED_ADDRESSES = "getLinkedAddresses";
        public const string STORE_TOKENS = "storeTokens";
        public static class IMX
        {
            public const string GET_ADDRESS = "getAddress";
            public const string IS_REGISTERED_OFFCHAIN = "isRegisteredOffchain";
            public const string REGISTER_OFFCHAIN = "registerOffchain";
            public const string TRANSFER = "imxTransfer";
            public const string BATCH_NFT_TRANSFER = "imxBatchNftTransfer";
        }

        public static class ZK_EVM
        {
            public const string CONNECT_EVM = "connectEvm";
            public const string SEND_TRANSACTION = "zkEvmSendTransaction";
            public const string SEND_TRANSACTION_WITH_CONFIRMATION = "zkEvmSendTransactionWithConfirmation";
            public const string GET_TRANSACTION_RECEIPT = "zkEvmGetTransactionReceipt";
            public const string SIGN_TYPED_DATA_V4 = "zkEvmSignTypedDataV4";
            public const string REQUEST_ACCOUNTS = "zkEvmRequestAccounts";
            public const string GET_BALANCE = "zkEvmGetBalance";
        }
    }
}
