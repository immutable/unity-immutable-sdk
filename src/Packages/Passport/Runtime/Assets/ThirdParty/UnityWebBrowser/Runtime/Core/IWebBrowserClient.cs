using VoltstroStudios.UnityWebBrowser.Events;

namespace VoltstroStudios.UnityWebBrowser.Core
{
    public interface IWebBrowserClient
    {
        event OnUnityPostMessageDelegate OnUnityPostMessage;

        void ExecuteJs(string js);
    }
}