using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Structure representing direct login options for authentication.
    /// Can be used for social login (google, apple, facebook) or email login.
    /// </summary>
    [Serializable]
    public class DirectLoginOptions
    {
        /// <summary>
        /// Authentication method.
        /// </summary>
        public DirectLoginMethod directLoginMethod = DirectLoginMethod.Email;

        /// <summary>
        /// Email address for email-based authentication (only used when directLoginMethod is Email).
        /// </summary>
        public string email;

        /// <summary>
        /// Marketing consent status. Defaults to OptedIn. Required by Auth endpoints.
        /// </summary>
        public MarketingConsentStatus? marketingConsentStatus;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DirectLoginOptions()
        {
            directLoginMethod = DirectLoginMethod.Email;
            email = null;
            marketingConsentStatus = MarketingConsentStatus.OptedIn;
        }

        /// <summary>
        /// Constructor with method and email.
        /// </summary>
        /// <param name="loginMethod">The direct login method</param>
        /// <param name="emailAddress">The email address (optional)</param>
        /// <param name="marketingConsentStatus">Marketing consent status. Defaults to OptedIn if not specified. Required.</param>
        public DirectLoginOptions(DirectLoginMethod loginMethod, string emailAddress = null, MarketingConsentStatus? marketingConsentStatus = null)
        {
            directLoginMethod = loginMethod;
            email = emailAddress;
            this.marketingConsentStatus = marketingConsentStatus ?? MarketingConsentStatus.OptedIn;
        }

        /// <summary>
        /// Checks if the email is valid and should be included in requests.
        /// </summary>
        /// <returns>True if email is valid for email method</returns>
        public bool IsEmailValid()
        {
            return directLoginMethod == DirectLoginMethod.Email && !string.IsNullOrEmpty(email);
        }
    }
}