using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immutable.Passport
{
    class Request {
        public string fxName;
        public string requestId;
        public string? data;

        public Request(string fxName, string requestId, string? data) {
            this.fxName = fxName;
            this.requestId = requestId;
            this.data = data;
        }
    }
}

