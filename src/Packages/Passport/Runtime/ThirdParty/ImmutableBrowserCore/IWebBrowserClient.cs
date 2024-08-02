namespace Immutable.Browser.Core
{
    public interface IWebBrowserClient
    {
        event OnUnityPostMessageDelegate OnUnityPostMessage;

        // Required for Gree browser only
#if (UNITY_ANDROID && !UNITY_EDITOR_WIN) || (UNITY_IPHONE && !UNITY_EDITOR_WIN) || UNITY_STANDALONE_OSX
        event OnUnityPostMessageDelegate OnAuthPostMessage;
        event OnUnityPostMessageErrorDelegate OnPostMessageError;
#endif
        void ExecuteJs(string js);
        void LaunchAuthURL(string url, string redirectUri);

        // Required for Windows browser only
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        void Dispose();
#endif

        // Only available for mobile devices
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        void ClearCache(bool includeDiskFiles);
        void ClearStorage();
#endif
    }
}