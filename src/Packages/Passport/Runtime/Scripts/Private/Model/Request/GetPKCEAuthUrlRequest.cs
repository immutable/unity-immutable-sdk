using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Request model for getting PKCE authentication URL.
    /// </summary>
    [Serializable]
    internal class GetPKCEAuthUrlRequest
    {
        /// <summary>
        /// Whether this is a connect to IMX operation (true) or just login (false).
        /// </summary>
        public bool isConnectImx;

        /// <summary>
        /// The direct login method to use for authentication.
        /// </summary>
        public string directLoginMethod;

        /// <summary>
        /// Creates a new GetPKCEAuthUrlRequest.
        /// </summary>
        /// <param name="isConnectImx">Whether this is a connect to IMX operation</param>
        /// <param name="directLoginMethod">The direct login method to use</param>
        public GetPKCEAuthUrlRequest(bool isConnectImx, DirectLoginMethod directLoginMethod)
        {
            this.isConnectImx = isConnectImx;
            this.directLoginMethod = directLoginMethod == DirectLoginMethod.None ? null : directLoginMethod.ToString().ToLower();
        }
    }
}