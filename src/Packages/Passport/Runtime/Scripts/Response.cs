using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Passport
{
    public class Response {
        public string? responseFor;
        public string? requestId;
    }

    public class AddressResponse : Response {
        public string? address;
    }

    public class GetImxProviderResponse : Response {
        public bool success;
    }

    public class SignMessageResponse : Response {
        public string? result;
    }
}
