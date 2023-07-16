using System;

namespace Immutable.Passport.Model
{
    public enum PassportErrorType
    {
        AUTHENTICATION_ERROR,
        WALLET_CONNECTION_ERROR,
        USER_REGISTRATION_ERROR,
        REFRESH_TOKEN_ERROR,
        TRANSFER_ERROR,
        CREATE_ORDER_ERROR,
        CANCEL_ORDER_ERROR,
        CREATE_TRADE_ERROR,
        BATCH_TRANSFER_ERROR,
        EXCHANGE_TRANSFER_ERROR,
        OPERATION_NOT_SUPPORTED_ERROR
    }

    public class PassportException : Exception
    {
        public PassportErrorType? Type;

        public PassportException(string message, PassportErrorType? type = null) : base(message)
        {
            this.Type = type;
        }
    }
}