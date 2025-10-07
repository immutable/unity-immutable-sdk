using UnityEngine;
using Immutable.Passport;

/// <summary>
/// Phase 1 Test Script for iOS Native WebView
/// Tests basic WebView functionality: create, show, hide, load URLs
///
/// IMPORTANT: This only tests Phase 1 features. Authentication callbacks will NOT work yet.
///
/// Usage:
/// 1. Add this script to a GameObject in your scene
/// 2. Build and deploy to iOS device or simulator
/// 3. Use the on-screen buttons to test WebView functionality
///
/// Expected Results:
/// - WebView appears/disappears when Show/Hide is pressed
/// - Google loads and displays correctly
/// - Passport auth page loads and displays (but login won't complete yet)
/// - You can interact with web pages (scroll, click links)
/// </summary>
public class TestPhase1WebView : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    private iOSNativePassportWebView webView;
#else
    private IPassportWebView webView;
#endif

    private bool isWebViewInitialized = false;
    private string statusMessage = "Press 'Initialize WebView' to start";

    void Start()
    {
        Debug.Log("[TestPhase1] Phase 1 WebView Test Script loaded");
        Debug.Log("[TestPhase1] Platform: " + Application.platform);
        Debug.Log("[TestPhase1] iOS Device: " + (Application.platform == RuntimePlatform.IPhonePlayer));
    }

    void OnGUI()
    {
        // Create a simple UI in the top-left corner
        GUILayout.BeginArea(new Rect(10, 10, 250, 400));
        GUILayout.Box("Phase 1 WebView Test");

        // Status display
        GUILayout.Label("Status:");
        GUILayout.TextArea(statusMessage, GUILayout.Height(60));

        GUILayout.Space(10);

        // Initialize button
        if (!isWebViewInitialized)
        {
            if (GUILayout.Button("Initialize WebView", GUILayout.Height(40)))
            {
                InitializeWebView();
            }
        }
        else
        {
            // WebView control buttons
            if (GUILayout.Button("Show WebView", GUILayout.Height(40)))
            {
                ShowWebView();
            }

            if (GUILayout.Button("Hide WebView", GUILayout.Height(40)))
            {
                HideWebView();
            }

            GUILayout.Space(10);
            GUILayout.Label("Load URLs:");

            if (GUILayout.Button("Load Google", GUILayout.Height(35)))
            {
                LoadGoogle();
            }

            if (GUILayout.Button("Load Passport (no auth yet)", GUILayout.Height(35)))
            {
                LoadPassport();
            }

            if (GUILayout.Button("Load Unity.com", GUILayout.Height(35)))
            {
                LoadUnity();
            }
        }

        GUILayout.EndArea();
    }

    private void InitializeWebView()
    {
        try
        {
            Debug.Log("[TestPhase1] Initializing WebView...");
            statusMessage = "Initializing WebView...";

#if UNITY_IOS && !UNITY_EDITOR
            var config = new PassportWebViewConfig
            {
                Width = 400,
                Height = 600,
                InitialUrl = "about:blank"
            };

            webView = new iOSNativePassportWebView();
            webView.Initialize(config);

            isWebViewInitialized = true;
            statusMessage = "WebView initialized!\nReady to test.";
            Debug.Log("[TestPhase1] iOS Native WebView initialized successfully");
#else
            statusMessage = "ERROR: iOS device build required!\nThis test only works on iOS devices.";
            Debug.LogWarning("[TestPhase1] This test requires an iOS device build");
#endif
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to initialize: {ex.Message}");
        }
    }

    private void ShowWebView()
    {
        if (webView == null) return;

        try
        {
            Debug.Log("[TestPhase1] Showing WebView");
            webView.Show();
            statusMessage = "WebView shown\nShould be visible on screen";
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR showing: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to show: {ex.Message}");
        }
    }

    private void HideWebView()
    {
        if (webView == null) return;

        try
        {
            Debug.Log("[TestPhase1] Hiding WebView");
            webView.Hide();
            statusMessage = "WebView hidden\nShould be invisible now";
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR hiding: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to hide: {ex.Message}");
        }
    }

    private void LoadGoogle()
    {
        if (webView == null) return;

        try
        {
            Debug.Log("[TestPhase1] Loading Google");
            webView.LoadUrl("https://www.google.com");
            statusMessage = "Loading Google...\nShould display search page";
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR loading: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to load Google: {ex.Message}");
        }
    }

    private void LoadPassport()
    {
        if (webView == null) return;

        try
        {
            Debug.Log("[TestPhase1] Loading Passport (auth won't complete)");
            webView.LoadUrl("https://auth.immutable.com/im-embedded-login-prompt");
            statusMessage = "Loading Passport...\nPage will display but login won't complete (Phase 4 needed)";
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR loading: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to load Passport: {ex.Message}");
        }
    }

    private void LoadUnity()
    {
        if (webView == null) return;

        try
        {
            Debug.Log("[TestPhase1] Loading Unity.com");
            webView.LoadUrl("https://unity.com");
            statusMessage = "Loading Unity.com...\nShould display Unity website";
        }
        catch (System.Exception ex)
        {
            statusMessage = $"ERROR loading: {ex.Message}";
            Debug.LogError($"[TestPhase1] Failed to load Unity.com: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        if (webView != null)
        {
            Debug.Log("[TestPhase1] Cleaning up WebView");
            webView.Dispose();
            webView = null;
        }
    }
}
