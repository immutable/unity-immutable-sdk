using System;

namespace Immutable.Passport.Model
{
    [Serializable]
    public class TransactionRequest
    {
        public string to;
        public string data;
        public string value;
    }

    [Serializable]
    internal class TransactionRequestNoData
    {
        public string to;
        public string value;
    }
}