namespace Immutable.Browser.Core
{
    public interface IWebBrowserClient
    {
        event OnUnityPostMessageDelegate OnUnityPostMessage;

        void ExecuteJs(string js);
    }
}