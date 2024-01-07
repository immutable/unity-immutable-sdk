using System;

namespace Immutable.Passport.Model
{
    public enum PassportErrorType
    {
        INITALISATION_ERROR,
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
        OPERATION_NOT_SUPPORTED_ERROR,
        NOT_LOGGED_IN_ERROR
    }

    public class PassportException : Exception
    {
        public Nullable<PassportErrorType> Type;

        public PassportException(string message, Nullable<PassportErrorType> type = null) : base(message)
        {
            this.Type = type;
        }

        /**
        * The error message for api requests via axios that fail due to network connectivity is "Network Error".
        * This isn't the most reliable way to determine connectivity but it is currently the best we have. 
        */
        public bool IsNetworkError()
        {
            return Message.EndsWith("Network Error");
        }
    }
}