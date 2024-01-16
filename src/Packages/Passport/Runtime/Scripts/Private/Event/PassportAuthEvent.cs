namespace Immutable.Passport.Event
{
    public delegate void OnAuthEventDelegate(PassportAuthEvent authEvent);

    public enum PassportAuthEvent
    {
        #region Device Code Authorisation

        /// <summary>
        /// Started the login process using Device Code Authorisation flow
        /// </summary>
        LoggingIn,
        /// <summary>
        /// Failed to log in using Device Code Authorisation flow
        /// </summary>
        LoginFailed,
        /// <summary>
        /// Successfully logged in using Device Code Authorisation flow
        /// </summary>
        LoginSuccess,
        /// <summary>
        /// Opening Passport login in an external browser for login via Device Code Authorisation flow
        /// </summary>
        LoginOpeningBrowser,
        /// <summary>
        /// Waiting for user to complete Passport login in an external browser
        /// </summary>
        PendingBrowserLogin,

        /// <summary>
        /// Started the login and set up IMX provider process using Device Code Authorisation flow
        /// </summary>
        ConnectingImx,
        /// <summary>
        /// Failed to login and set up IMX provider using Device Code Authorisation flow
        /// </summary>
        ConnectImxFailed,
        /// <summary>
        /// Successfully logged in and set up IMX provider using Device Code Authorisation flow
        /// </summary>
        ConnectImxSuccess,
        /// <summary>
        /// Opening Passport login in an external browser for connect IMX via Device Code Authorisation flow
        /// </summary>
        ConnectImxOpeningBrowser,
        /// <summary>
        /// Waiting for user to complete Passport login in the browser and the IMX provider to be set up
        /// </summary>
        PendingBrowserLoginAndProviderSetup,

        /// <summary>
        /// Started the log out process using an external browser
        /// </summary>
        LoggingOut,
        /// <summary>
        /// Failed to log out using an external browser
        /// </summary>
        LogoutFailed,
        /// <summary>
        /// Successfully logged out using an external browser
        /// </summary>
        LogoutSuccess,

        #endregion

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
        /// Started the login and set up IMX provider process using PKCE flow
        /// </summary>
        ConnectingImxPKCE,
        /// <summary>
        /// Launching Passport login in Chrome Custom Tabs for connect IMX via PKCE flow
        /// </summary>
        ConnectImxPKCELaunchingCustomTabs,
        /// <summary>
        /// Opening Passport login in a webview (ASWebAuthenticationSession) for connect IMX via PKCE flow
        /// </summary>
        ConnectImxPKCEOpeningWebView,
        /// <summary>
        /// Failed to login and set up IMX provider using PKCE flow
        /// </summary>
        ConnectImxPKCEFailed,
        /// <summary>
        /// Successfully logged in and set up IMX provider using PKCE flow
        /// </summary>
        ConnectImxPKCESuccess,
        /// <summary>
        /// Chrome Custom Tabs/Webview redirected the user back to the game via deeplink 
        /// and is now trying to complete the PKCE login process and set up IMX provider
        /// </summary>
        CompletingConnectImxPKCE,


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

        /// <summary>
        /// Started the reconnect (login and set up IMX provider) process using saved credentials
        /// </summary>
        Reconnecting,
        /// <summary>
        /// Failed to reconnect (login and set up IMX provider) using saved credentials
        /// </summary>
        ReconnectFailed,
        /// <summary>
        /// Successfully reconnected (login and set up IMX provider) in using saved credentials
        /// </summary>
        ReconnectSuccess,

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