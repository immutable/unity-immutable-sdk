namespace Immutable.Passport.Event
{
    public delegate void OnAuthEventDelegate(PassportAuthEvent authEvent);

    public enum PassportAuthEvent
    {
        #region PKCE

        /// <summary>
        /// Started the login process using PKCE flow
        /// </summary>
        LoggingInPKCE,
        /// <summary>
        /// Launching Passport login in Chrome Custom Tabs for login via PKCE flow
        /// </summary>
        LoginPKCELaunchingCustomTabs,
        /// <summary>
        /// Opening Passport login in a webview (ASWebAuthenticationSession) for login via PKCE flow
        /// </summary>
        LoginPKCEOpeningWebView,
        /// <summary>
        /// Failed to log in using PKCE flow
        /// </summary>
        LoginPKCEFailed,
        /// <summary>
        /// Successfully logged in using PKCE flow
        /// </summary>
        LoginPKCESuccess,
        /// <summary>
        /// Chrome Custom Tabs/Webview redirected the user back to the game via deeplink 
        /// and is now trying to complete the PKCE login process
        /// </summary>
        CompletingLoginPKCE,

        /// <summary>
        /// Started the log out process using Chrome Custom Tabs/ASWebAuthenticationSession
        /// </summary>
        LoggingOutPKCE,
        /// <summary>
        /// Failed to log out using Chrome Custom Tabs/ASWebAuthenticationSession
        /// </summary>
        LogoutPKCEFailed,
        /// <summary>
        /// Successfully logged out using Chrome Custom Tabs/ASWebAuthenticationSession
        /// </summary>
        LogoutPKCESuccess,

        #endregion

        #region Using saved credentials
        /// <summary>
        /// Started the re-login process using saved credentials
        /// </summary>
        ReloggingIn,
        /// <summary>
        /// Failed to re-login using saved credentials
        /// </summary>
        ReloginFailed,
        /// <summary>
        /// Successfully re-logged in using saved credentials
        /// </summary>
        ReloginSuccess,

        #endregion

        /// <summary>
        /// Started to the process of checking whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckingForSavedCredentials,
        /// <summary>
        /// Failed to check whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckForSavedCredentialsFailed,
        /// <summary>
        /// Successfully checked whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckForSavedCredentialsSuccess
    }
}