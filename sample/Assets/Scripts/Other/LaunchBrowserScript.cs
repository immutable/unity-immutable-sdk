#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;

public class LaunchBrowserScript : MonoBehaviour
{
    [SerializeField] private WebBrowserUIFull webBrowser;
    [SerializeField] private Button openDevToolsButton;

    private WebBrowserClient? webBrowserClient;

    public void Start()
    {
        webBrowser.browserClient.OnLoadFinish += OnLoadFinish;

        void OnLoadFinish(string url)
        {
            webBrowser.browserClient.OnLoadFinish -= OnLoadFinish;
            webBrowserClient = webBrowser.browserClient;
        }
    }

    /// <summary>
    /// Opens the dev tools for the browser
    /// </summary>
    public void OpenDevTools()
    {
        webBrowserClient?.OpenDevTools();
    }

    /// <summary>
    /// Navigates back to the authenticated scene.
    /// </summary>
    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }
}
#endif