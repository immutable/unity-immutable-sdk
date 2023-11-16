namespace Immutable.Browser.Core
{
    public interface IWebBrowserClient
    {
        event OnUnityPostMessageDelegate OnUnityPostMessage;
        event OnUnityPostMessageDelegate OnAuthPostMessage;
        event OnUnityPostMessageErrorDelegate OnPostMessageError;

        void ExecuteJs(string js);

        void LaunchAuthURL(string url, string redirectUri);
    }
}