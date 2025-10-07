#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <UIKit/UIKit.h>
#import "PassportWebView.h"

// Global callback function pointers
static PassportWebView_OnLoadFinished onLoadFinishedCallback = NULL;
static PassportWebView_OnJavaScriptMessage onJavaScriptMessageCallback = NULL;
static PassportWebView_OnURLChanged onURLChangedCallback = NULL;

// Forward declaration
@interface PassportWebViewWrapper : NSObject
@property (strong, nonatomic) WKWebView* webView;
@property (strong, nonatomic) NSString* customURLScheme;
@property (assign, nonatomic) BOOL isVisible;
@end

@implementation PassportWebViewWrapper

- (instancetype)init {
    self = [super init];
    if (self) {
        _isVisible = NO;
        _customURLScheme = @"immutablerunner"; // Default
    }
    return self;
}

- (void)setupWebView {
    // Create WKWebView configuration
    WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
    
    // Enable JavaScript
    config.preferences.javaScriptEnabled = YES;
    
    // Create WebView with zero frame (will be set when shown)
    self.webView = [[WKWebView alloc] initWithFrame:CGRectZero configuration:config];
    
    // Set background to transparent
    self.webView.opaque = NO;
    self.webView.backgroundColor = [UIColor clearColor];
    
    // Add to Unity view hierarchy (but keep hidden initially)
    UIViewController* unityViewController = UnityGetGLViewController();
    [unityViewController.view addSubview:self.webView];
    self.webView.hidden = YES;
    
    NSLog(@"[PassportWebView] WKWebView created and added to Unity view");
}

- (void)loadURL:(NSString*)urlString {
    if (!self.webView) {
        NSLog(@"[PassportWebView] Error: WebView not initialized");
        return;
    }
    
    NSURL* url = [NSURL URLWithString:urlString];
    if (url) {
        NSURLRequest* request = [NSURLRequest requestWithURL:url];
        [self.webView loadRequest:request];
        NSLog(@"[PassportWebView] Loading URL: %@", urlString);
    } else {
        NSLog(@"[PassportWebView] Error: Invalid URL: %@", urlString);
    }
}

- (void)show {
    if (self.webView) {
        self.webView.hidden = NO;
        self.isVisible = YES;
        NSLog(@"[PassportWebView] WebView shown");
    }
}

- (void)hide {
    if (self.webView) {
        self.webView.hidden = YES;
        self.isVisible = NO;
        NSLog(@"[PassportWebView] WebView hidden");
    }
}

- (void)setFrame:(CGRect)frame {
    if (self.webView) {
        self.webView.frame = frame;
        NSLog(@"[PassportWebView] Frame set to: x=%.0f, y=%.0f, w=%.0f, h=%.0f", 
              frame.origin.x, frame.origin.y, frame.size.width, frame.size.height);
    }
}

- (void)executeJavaScript:(NSString*)script {
    if (self.webView) {
        [self.webView evaluateJavaScript:script completionHandler:^(id result, NSError *error) {
            if (error) {
                NSLog(@"[PassportWebView] JavaScript execution error: %@", error.localizedDescription);
            } else {
                NSLog(@"[PassportWebView] JavaScript executed successfully");
            }
        }];
    }
}

- (void)cleanup {
    if (self.webView) {
        [self.webView removeFromSuperview];
        self.webView = nil;
        NSLog(@"[PassportWebView] WebView cleaned up");
    }
}

@end

// C Interface Implementations

void* PassportWebView_Create(const char* gameObjectName) {
    NSLog(@"[PassportWebView] Creating WebView for: %s", gameObjectName);
    
    PassportWebViewWrapper* wrapper = [[PassportWebViewWrapper alloc] init];
    [wrapper setupWebView];
    
    return (__bridge_retained void*)wrapper;
}

void PassportWebView_Destroy(void* webViewPtr) {
    if (webViewPtr == NULL) return;
    
    NSLog(@"[PassportWebView] Destroying WebView");
    
    PassportWebViewWrapper* wrapper = (__bridge_transfer PassportWebViewWrapper*)webViewPtr;
    [wrapper cleanup];
    wrapper = nil;
}

void PassportWebView_LoadURL(void* webViewPtr, const char* url) {
    if (webViewPtr == NULL || url == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    NSString* urlString = [NSString stringWithUTF8String:url];
    [wrapper loadURL:urlString];
}

void PassportWebView_Show(void* webViewPtr) {
    if (webViewPtr == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    [wrapper show];
}

void PassportWebView_Hide(void* webViewPtr) {
    if (webViewPtr == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    [wrapper hide];
}

void PassportWebView_SetFrame(void* webViewPtr, float x, float y, float width, float height) {
    if (webViewPtr == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    CGRect frame = CGRectMake(x, y, width, height);
    [wrapper setFrame:frame];
}

void PassportWebView_SetCustomURLScheme(void* webViewPtr, const char* urlScheme) {
    if (webViewPtr == NULL || urlScheme == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    wrapper.customURLScheme = [NSString stringWithUTF8String:urlScheme];
    NSLog(@"[PassportWebView] Custom URL scheme set to: %@", wrapper.customURLScheme);
}

void PassportWebView_ExecuteJavaScript(void* webViewPtr, const char* script) {
    if (webViewPtr == NULL || script == NULL) return;
    
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    NSString* scriptString = [NSString stringWithUTF8String:script];
    [wrapper executeJavaScript:scriptString];
}

// Callback setters (for Phase 4, but defined now for completeness)
void PassportWebView_SetOnLoadFinishedCallback(PassportWebView_OnLoadFinished callback) {
    onLoadFinishedCallback = callback;
    NSLog(@"[PassportWebView] OnLoadFinished callback set");
}

void PassportWebView_SetOnJavaScriptMessageCallback(PassportWebView_OnJavaScriptMessage callback) {
    onJavaScriptMessageCallback = callback;
    NSLog(@"[PassportWebView] OnJavaScriptMessage callback set");
}

void PassportWebView_SetOnURLChangedCallback(PassportWebView_OnURLChanged callback) {
    onURLChangedCallback = callback;
    NSLog(@"[PassportWebView] OnURLChanged callback set");
}
