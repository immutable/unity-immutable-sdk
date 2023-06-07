using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Passport
{
    internal class Response {
        public string? responseFor;
        public string? requestId;
    }

    internal class AddressResponse : Response {
        public string? address;
    }

    internal class GetImxProviderResponse : Response {
        public bool success;
    }

    internal class SignMessageResponse : Response {
        public string? result;
    }
}
