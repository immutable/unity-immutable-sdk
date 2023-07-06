namespace Immutable.Passport.Auth
{
#pragma warning disable CS8618
#pragma warning disable IDE1006
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

    public class ConnectResponse
    {
        public string code { get; set; }
        public string url { get; set; }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }

    internal class TokenPayload
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
        public string? imx_eth_address { get; set; }
        public string? imx_stark_address { get; set; }
        public string? imx_user_admin_address { get; set; }
    }

    internal class ErrorResponse
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
#pragma warning restore CS8618
#pragma warning restore IDE1006
}