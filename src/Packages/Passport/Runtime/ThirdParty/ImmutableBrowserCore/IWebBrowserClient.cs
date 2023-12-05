namespace Immutable.Browser.Core
{
    public interface IWebBrowserClient
    {
        event OnUnityPostMessageDelegate OnUnityPostMessage;
        event OnUnityPostMessageDelegate OnAuthPostMessage;
        event OnUnityPostMessageErrorDelegate OnPostMessageError;

        void ExecuteJs(string js);

        void LaunchAuthURL(string url, string redirectUri);
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        void ClearCache(bool includeDiskFiles);
        void ClearStorage();
#endif
    }
}