using System.Collections;
using System.Collections.Generic;

namespace Immutable.Passport.Auth
{
    /// <summary>
    /// The specification about the response is described in "3.2. Device Authorization Response".
    /// See https://datatracker.ietf.org/doc/html/rfc8628#section-3.2
    /// </summary>
    internal class DeviceCodeResponse
    {
        public string device_code { get; set; }
        public string user_code { get; set; }
        public string verification_uri { get; set; }
        public int expires_in { get; set; }
        public int interval { get; set; }
        public string verification_uri_complete { get; set; }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }

    internal class AccessTokenPayload
    {
        public long? exp { get; set; }
    }

    internal class IdTokenPayload
    {
        public IdTokenPassport? passport { get; set; }
        public string? email { get; set; }
        public string? nickname { get; set; }
        public string? aud { get; set; }
        public string? sub { get; set; }
    }

    internal class IdTokenPassport
    {
        public string? ether_key { get; set; }
        public string? stark_key { get; set; }
        public string? user_admin_key { get; set; }
    }

    internal class ErrorResponse
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
}