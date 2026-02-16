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
        /// The direct login options for authentication.
        /// </summary>
        public DirectLoginOptions directLoginOptions;

        /// <summary>
        /// Creates a new GetPKCEAuthUrlRequest with DirectLoginOptions.
        /// </summary>
        /// <param name="directLoginOptions">The direct login options to use</param>
        public GetPKCEAuthUrlRequest(DirectLoginOptions directLoginOptions)
        {
            this.directLoginOptions = directLoginOptions;
        }
    }
}