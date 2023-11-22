namespace Immutable.Passport
{
    public static class PassportFunction
    {
        public const string INIT = "init";
        public const string CONNECT = "connect";
        public const string CONNECT_PKCE = "connectPKCE";
        public const string GET_PKCE_AUTH_URL = "getPKCEAuthUrl";
        public const string CONFIRM_CODE = "confirmCode";
        public const string CONNECT_WITH_CREDENTIALS = "connectWithCredentials";
        public const string CHECK_STORED_CREDENTIALS = "checkStoredCredentials";
        public const string GET_ADDRESS = "getAddress";
        public const string LOGOUT = "logout";
        public const string GET_EMAIL = "getEmail";

        public static class IMX
        {
            public const string IS_REGISTERED_OFFCHAIN = "isRegisteredOffchain";
            public const string REGISTER_OFFCHAIN = "registerOffchain";
            public const string TRANSFER = "imxTransfer";
            public const string BATCH_NFT_TRANSFER = "imxBatchNftTransfer";
        }

        public static class ZK_EVM
        {
            public const string CONNECT_EVM = "connectEvm";
            public const string SEND_TRANSACTION = "zkEvmSendTransaction";
            public const string REQUEST_ACCOUNTS = "zkEvmRequestAccounts";
            public const string GET_BALANCE = "zkEvmGetBalance";
        }
    }
}
