#ifndef PassportWebView_h
#define PassportWebView_h

#ifdef __cplusplus
extern "C" {
#endif

// WebView lifecycle
void* PassportWebView_Create(const char* gameObjectName);
void PassportWebView_Destroy(void* webViewPtr);
void PassportWebView_LoadURL(void* webViewPtr, const char* url);
void PassportWebView_Show(void* webViewPtr);
void PassportWebView_Hide(void* webViewPtr);

// Configuration
void PassportWebView_SetFrame(void* webViewPtr, float x, float y, float width, float height);
void PassportWebView_SetCustomURLScheme(void* webViewPtr, const char* urlScheme);
void PassportWebView_ExecuteJavaScript(void* webViewPtr, const char* script);

// Callbacks (set from Unity)
typedef void (*PassportWebView_OnLoadFinished)(const char* url);
typedef void (*PassportWebView_OnJavaScriptMessage)(const char* method, const char* data);
typedef void (*PassportWebView_OnURLChanged)(const char* url);

void PassportWebView_SetOnLoadFinishedCallback(PassportWebView_OnLoadFinished callback);
void PassportWebView_SetOnJavaScriptMessageCallback(PassportWebView_OnJavaScriptMessage callback);
void PassportWebView_SetOnURLChangedCallback(PassportWebView_OnURLChanged callback);

#ifdef __cplusplus
}
#endif

#endif
