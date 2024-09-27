using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    ///     Represents the payload for signing an EIP-712 typed data message using ZK-EVM.
    ///     This payload consists of three components: domain, types, and message.
    ///     Each component is a JSON string that conforms to the EIP-712 standard.
    /// </summary>
    [Serializable]
    public class SignPayloadRequest
    {
        public string domain;
        public string types;
        public string message;
    }
}