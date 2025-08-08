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
        /// The direct login options for authentication.
        /// </summary>
        public DirectLoginOptions directLoginOptions;

        /// <summary>
        /// Creates a new GetPKCEAuthUrlRequest with DirectLoginOptions.
        /// </summary>
        /// <param name="isConnectImx">Whether this is a connect to IMX operation</param>
        /// <param name="directLoginOptions">The direct login options to use</param>
        public GetPKCEAuthUrlRequest(bool isConnectImx, DirectLoginOptions directLoginOptions)
        {
            this.isConnectImx = isConnectImx;
            this.directLoginOptions = directLoginOptions;
        }
    }
}