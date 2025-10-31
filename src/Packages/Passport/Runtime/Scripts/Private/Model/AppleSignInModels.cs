using System;
using Newtonsoft.Json;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Request sent from Unity to your backend with Apple Sign-in tokens
    /// Your backend will verify these tokens and exchange them with Passport
    /// </summary>
    [Serializable]
    public class AppleSignInRequest
    {
        /// <summary>
        /// JWT identity token from Apple (contains user's email, sub, etc.)
        /// Your backend MUST verify this token with Apple's public keys
        /// </summary>
        [JsonProperty("identityToken")]
        public string identityToken;

        /// <summary>
        /// Optional: Authorization code from Apple (single-use, short-lived)
        /// Can be used to get refresh tokens from Apple if needed
        /// </summary>
        [JsonProperty("authorizationCode")]
        public string authorizationCode;

        /// <summary>
        /// Apple's unique user identifier (from JWT 'sub' claim)
        /// Stable across sign-ins, use this as primary identifier
        /// </summary>
        [JsonProperty("userId")]
        public string userId;

        /// <summary>
        /// User's email (only populated on first sign-in in native callback, but always in JWT)
        /// Your backend should extract this from the JWT
        /// </summary>
        [JsonProperty("email")]
        public string email;

        /// <summary>
        /// User's full name (only populated on first sign-in)
        /// Store this in your backend on first sign-in
        /// </summary>
        [JsonProperty("fullName")]
        public string fullName;

        /// <summary>
        /// Passport client ID - backend validates this matches expected client
        /// </summary>
        [JsonProperty("clientId")]
        public string clientId;

        public AppleSignInRequest(string identityToken, string authorizationCode, string userId, string email, string fullName, string clientId)
        {
            this.identityToken = identityToken;
            this.authorizationCode = authorizationCode;
            this.userId = userId;
            this.email = email;
            this.fullName = fullName;
            this.clientId = clientId;
        }
    }

    /// <summary>
    /// Response from your backend containing Passport tokens
    /// Your backend exchanges Apple tokens with Immutable's Passport backend and returns these
    /// </summary>
    [Serializable]
    public class AppleSignInResponse
    {
        /// <summary>
        /// Passport access token (use this for API calls)
        /// </summary>
        [JsonProperty("access_token")]
        public string accessToken;

        /// <summary>
        /// Passport ID token (JWT containing user identity)
        /// </summary>
        [JsonProperty("id_token")]
        public string idToken;

        /// <summary>
        /// Passport refresh token (use to get new access tokens)
        /// </summary>
        [JsonProperty("refresh_token")]
        public string refreshToken;

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        [JsonProperty("expires_in")]
        public int expiresIn;

        /// <summary>
        /// Token type (usually "Bearer")
        /// </summary>
        [JsonProperty("token_type")]
        public string tokenType;
    }

    /// <summary>
    /// Error response from backend if Apple Sign-in fails
    /// </summary>
    [Serializable]
    public class AppleSignInErrorResponse
    {
        /// <summary>
        /// Error code (e.g., "invalid_token", "verification_failed")
        /// </summary>
        [JsonProperty("error")]
        public string error;

        /// <summary>
        /// Human-readable error message
        /// </summary>
        [JsonProperty("error_description")]
        public string errorDescription;
    }
}

