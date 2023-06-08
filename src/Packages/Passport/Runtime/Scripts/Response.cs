using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Passport
{
    internal class Response {
        public string? responseFor;
        public string? requestId;
        public bool success;
        public string? error;
    }

    internal class AddressResponse : Response {
        public string? address;
    }

    internal class GetImxProviderResponse : Response {
    }

    internal class SignMessageResponse : Response {
        public string? result;
    }
}
