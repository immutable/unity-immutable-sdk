/*
 * Copyright (C) 2011 Keijiro Takahashi
 * Copyright (C) 2012 GREE, Inc.
 *
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

package net.gree.unitywebview;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.view.View;
import android.webkit.CookieManager;
import android.webkit.GeolocationPermissions.Callback;
import android.webkit.JavascriptInterface;
import android.webkit.JsPromptResult;
import android.webkit.JsResult;
import android.webkit.PermissionRequest;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import com.unity3d.player.UnityPlayer;

import java.net.URLEncoder;
import java.util.ArrayDeque;
import java.util.Hashtable;
import java.util.Objects;
import java.util.Queue;
import java.util.concurrent.Callable;
import java.util.concurrent.FutureTask;

class CWebViewPluginNoUiInterface {
    private CWebViewPluginNoUi mPlugin;
    private String mGameObject;

    public CWebViewPluginNoUiInterface(CWebViewPluginNoUi plugin, String gameObject) {
        mPlugin = plugin;
        mGameObject = gameObject;
    }

    @JavascriptInterface
    public void call(final String message) {
        call("CallFromJS", message);
    }

    public void call(final String method, final String message) {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        if (mPlugin.IsInitialized()) {
            mPlugin.MyUnitySendMessage(mGameObject, method, message);
        }
//        a.runOnUiThread(new Runnable() {
//            public void run() {
//                if (mPlugin.IsInitialized()) {
//                    mPlugin.MyUnitySendMessage(mGameObject, method, message);
//                }
//            }
//        });
    }
}

public class CWebViewPluginNoUi {
    private WebViewCallback callback;
    private WebView mWebView;
    private CWebViewPluginNoUiInterface mWebViewPlugin;
    private boolean mAlertDialogEnabled;
    private Handler unityHandler;

    public static boolean isDestroyed(final Activity a) {
        if (a == null) {
            return true;
        } else if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN_MR1) {
            return a.isDestroyed();
        } else {
            return false;
        }
    }

    public CWebViewPluginNoUi() {
        unityHandler = new Handler(Looper.myLooper());
    }

    public void setCallback(WebViewCallback callback) {
        this.callback = callback;
    }

    public void MyUnitySendMessage(String gameObject, final String method, final String message) {
        if (callback != null && Objects.equals(method, "CallFromJS"))
            unityHandler.post(new Runnable() {
                @Override
                public void run() {
                    callback.callFromJs(message);
                }
            });

    }

    public boolean IsInitialized() {
        return mWebView != null;
    }

    public void Init(final String gameObject, final String ua) {
        final CWebViewPluginNoUi self = this;
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPluginNoUi.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView != null) {
                    return;
                }
                mAlertDialogEnabled = true;

                final WebView webView = new WebView(a.getApplication());
                try {
                    ApplicationInfo ai = a.getPackageManager().getApplicationInfo(a.getPackageName(), 0);
                    if ((ai.flags & ApplicationInfo.FLAG_DEBUGGABLE) != 0) {
                        WebView.setWebContentsDebuggingEnabled(true);
                    }
                } catch (Exception ignored) {
                }
                webView.setVisibility(View.INVISIBLE);
//            webView.setFocusable(true);
//            webView.setFocusableInTouchMode(true);

                // webView.setWebChromeClient(new WebChromeClient() {
                //     public boolean onConsoleMessage(android.webkit.ConsoleMessage cm) {
                //         Log.d("Webview", cm.message());
                //         return true;
                //     }
                // });
                webView.setWebChromeClient(new WebChromeClient() {
                    @Override
                    public boolean onJsAlert(WebView view, String url, String message, JsResult result) {
                        if (!mAlertDialogEnabled) {
                            result.cancel();
                            return true;
                        }
                        return super.onJsAlert(view, url, message, result);
                    }

                    @Override
                    public boolean onJsConfirm(WebView view, String url, String message, JsResult result) {
                        if (!mAlertDialogEnabled) {
                            result.cancel();
                            return true;
                        }
                        return super.onJsConfirm(view, url, message, result);
                    }

                    @Override
                    public boolean onJsPrompt(WebView view, String url, String message, String defaultValue, JsPromptResult result) {
                        if (!mAlertDialogEnabled) {
                            result.cancel();
                            return true;
                        }
                        return super.onJsPrompt(view, url, message, defaultValue, result);
                    }

                    @Override
                    public void onGeolocationPermissionsShowPrompt(String origin, Callback callback) {
                        callback.invoke(origin, true, false);
                    }
                });

                mWebViewPlugin = new CWebViewPluginNoUiInterface(self, gameObject);

                webView.setWebViewClient(new WebViewClient() {

                    @Override
                    public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                        System.out.println("<<Gree>> onReceivedError: " + description);
                        webView.loadUrl("about:blank");
                        mWebViewPlugin.call("CallOnError", errorCode + "\t" + description + "\t" + failingUrl);
                    }

                    @Override
                    public void onReceivedHttpError(WebView view, WebResourceRequest request, WebResourceResponse errorResponse) {
                        System.out.println("<<Gree>> onReceivedHttpError: " + errorResponse.getReasonPhrase());
                        mWebViewPlugin.call("CallOnHttpError", Integer.toString(errorResponse.getStatusCode()));
                    }

                    @Override
                    public void onPageStarted(WebView view, String url, Bitmap favicon) {
                        System.out.println("<<Gree>> onPageStarted: " + url);
                        mWebViewPlugin.call("CallOnStarted", url);
                    }

                    @Override
                    public void onPageFinished(WebView view, String url) {
                        System.out.println("<<Gree>> onPageFinished: " + url);
                        mWebViewPlugin.call("CallOnLoaded", url);
                    }

                    @Override
                    public void onLoadResource(WebView view, String url) {
                    }

                    @Override
                    public boolean shouldOverrideUrlLoading(WebView view, String url) {
                        System.out.println("<<Gree>> shouldOverrideUrlLoading: " + url);
                        boolean pass = true;
                        if (url.startsWith("unity:")) {
                            String message = url.substring(6);
                            mWebViewPlugin.call("CallFromJS", message);
                            return true;
                        } else if (!url.toLowerCase().endsWith(".pdf")
                            && !url.startsWith("https://maps.app.goo.gl")
                            && (url.startsWith("http://")
                            || url.startsWith("https://")
                            || url.startsWith("file://")
                            || url.startsWith("javascript:"))) {
                            mWebViewPlugin.call("CallOnStarted", url);
                            // Let webview handle the URL
                            return false;
                        }
                        Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
                        PackageManager pm = a.getPackageManager();
                        // List<ResolveInfo> apps = pm.queryIntentActivities(intent, 0);
                        // if (apps.size() > 0) {
                        //     view.getContext().startActivity(intent);
                        // }
                        try {
                            view.getContext().startActivity(intent);
                        } catch (ActivityNotFoundException ex) {
                        }
                        return true;
                    }
                });
                webView.addJavascriptInterface(mWebViewPlugin, "Unity");

                WebSettings webSettings = webView.getSettings();
                if (ua != null && ua.length() > 0) {
                    webSettings.setUserAgentString(ua);
                }
//            mWebViewUA = webSettings.getUserAgentString();
                webSettings.setJavaScriptEnabled(true);
                webSettings.setGeolocationEnabled(true);
                webSettings.setAllowUniversalAccessFromFileURLs(true);
                webSettings.setMediaPlaybackRequiresUserGesture(false);
                webSettings.setDatabaseEnabled(true);
                webSettings.setDomStorageEnabled(true);
                webSettings.setAllowFileAccess(true);  // cf. https://github.com/gree/unity-webview/issues/625
                CookieManager.getInstance().setAcceptThirdPartyCookies(webView, true);
                mWebView = webView;
            }
        });
    }

    public void LoadURL(final String url) {
        System.out.println("<<Gree>> Load URL called: " + url + " webview is null" + (mWebView == null));
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                System.out.println("<<Gree>> Load URL in block: " + url);
                mWebView.loadUrl(url);
            }
        });
    }

    public void EvaluateJS(final String js) {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                mWebView.loadUrl("javascript:" + URLEncoder.encode(js));
            }
        });
    }

    public void SetNetworkAvailable(final boolean networkUp) {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                mWebView.setNetworkAvailable(networkUp);
            }
        });
    }

    // as the following explicitly pause/resume, pauseTimers()/resumeTimers() are always
    // called. this differs from OnApplicationPause().
    public void Pause() {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                mWebView.onPause();
                mWebView.pauseTimers();
            }
        });
    }

    public void Resume() {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                mWebView.onResume();
                mWebView.resumeTimers();
            }
        });
    }

    // cf. https://stackoverflow.com/questions/31788748/webview-youtube-videos-playing-in-background-on-rotation-and-minimise/31789193#31789193
    public void OnApplicationPause(final boolean paused) {
        final Activity a = UnityPlayer.currentActivity;
        if (CWebViewPlugin.isDestroyed(a)) {
            return;
        }
        a.runOnUiThread(new Runnable() {
            public void run() {
                if (mWebView == null) {
                    return;
                }
                if (paused) {
                    mWebView.onPause();
                    if (mWebView.getVisibility() == View.VISIBLE) {
                        // cf. https://qiita.com/nbhd/items/d31711faa8852143f3a4
                        mWebView.pauseTimers();
                    }
                } else {
                    mWebView.onResume();
                    mWebView.resumeTimers();
                }
            }
        });
    }
}
