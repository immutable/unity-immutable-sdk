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

#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

// NOTE: we need extern without "C" before unity 4.5
extern "C" UIViewController *UnityGetGLViewController();
extern "C" void UnitySendMessage(const char *, const char *, const char *);
extern "C" typedef void (*DelegateCallbackFunction)(const char * key, const char * message);

DelegateCallbackFunction delegateCallback = NULL;

// cf. https://stackoverflow.com/questions/26383031/wkwebview-causes-my-view-controller-to-leak/33365424#33365424
@interface WeakScriptMessageDelegate : NSObject<WKScriptMessageHandler>

@property (nonatomic, weak) id<WKScriptMessageHandler> scriptDelegate;

- (instancetype)initWithDelegate:(id<WKScriptMessageHandler>)scriptDelegate;

@end

@implementation WeakScriptMessageDelegate

- (instancetype)initWithDelegate:(id<WKScriptMessageHandler>)scriptDelegate
{
    self = [super init];
    if (self) {
        _scriptDelegate = scriptDelegate;
    }
    return self;
}

- (void)userContentController:(WKUserContentController *)userContentController didReceiveScriptMessage:(WKScriptMessage *)message
{
    [self.scriptDelegate userContentController:userContentController didReceiveScriptMessage:message];
}

@end

@protocol WebViewProtocol <NSObject>
@property (nullable, nonatomic, weak) id <WKNavigationDelegate> navigationDelegate;
@property (nullable, nonatomic, weak) id <WKUIDelegate> UIDelegate;
@property (nullable, nonatomic, readonly, copy) NSURL *URL;
- (void)stopLoading;
- (void)load:(NSURLRequest *)request;
- (void)evaluateJavaScript:(NSString *)javaScriptString completionHandler:(void (^ __nullable)(__nullable id, NSError * __nullable error))completionHandler;
@end

@interface WKWebView(WebViewProtocolConformed) <WebViewProtocol>
@end

@implementation WKWebView(WebViewProtocolConformed)

- (void)load:(NSURLRequest *)request
{
    WKWebView *webView = (WKWebView *)self;
    NSURL *url = [request URL];
    if ([url.absoluteString hasPrefix:@"file:"]) {
        // LoadFileURL is not loading the JS content due to thinking it's out of the sandbox
        // NSURL *top = [NSURL URLWithString:[[url absoluteString] stringByDeletingLastPathComponent]];
        // [webView loadFileURL:url allowingReadAccessToURL:top];
        NSString *htmlContent = [NSString stringWithContentsOfFile:url.path encoding:NSUTF8StringEncoding error:nil];
        [webView loadHTMLString:htmlContent baseURL:url];
    } else {
        [webView loadRequest:request];
    }
}
@end

@interface CWebViewPlugin : NSObject<WKUIDelegate, WKNavigationDelegate, WKScriptMessageHandler, ASWebAuthenticationPresentationContextProviding>
{
    WKWebView *webView;
}
@end

@implementation CWebViewPlugin

static WKProcessPool *_sharedProcessPool;
static NSMutableArray *_instances = [[NSMutableArray alloc] init];
static CWebViewPlugin *__delegate = nil;
static ASWebAuthenticationSession *_authSession;

- (id)initWithUa:(const char *)ua
{
    self = [super init];

    if (_sharedProcessPool == NULL) {
        _sharedProcessPool = [[WKProcessPool alloc] init];
    }

    WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
    WKUserContentController *controller = [[WKUserContentController alloc] init];
    [controller addScriptMessageHandler:[[WeakScriptMessageDelegate alloc] initWithDelegate:self] name:@"unityControl"];
    [controller addScriptMessageHandler:[[WeakScriptMessageDelegate alloc] initWithDelegate:self] name:@"saveDataURL"];
    [controller addScriptMessageHandler:[[WeakScriptMessageDelegate alloc] initWithDelegate:self] name:@"logHandler"];
    NSString *str = @"\
        window.Unity = { \
        call: function(msg) { \
            window.webkit.messageHandlers.unityControl.postMessage(msg); \
        }, \
        saveDataURL: function(fileName, dataURL) { \
            window.webkit.messageHandlers.saveDataURL.postMessage(fileName + '\t' + dataURL); \
        } \
        }; \
        function captureLog(msg) { window.webkit.messageHandlers.logHandler.postMessage(msg); } window.console.log = captureLog; \
        ";

    WKUserScript *script
        = [[WKUserScript alloc] initWithSource:str injectionTime:WKUserScriptInjectionTimeAtDocumentStart forMainFrameOnly:YES];
    [controller addUserScript:script];
    configuration.userContentController = controller;
    configuration.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeNone;
    configuration.websiteDataStore = [WKWebsiteDataStore defaultDataStore];
    configuration.processPool = _sharedProcessPool;

    WKWebView *wkwebView = [[WKWebView alloc] initWithFrame:CGRectZero configuration:configuration];
#if UNITYWEBVIEW_DEVELOPMENT
    // enable Safari debugging if exists
    if ([wkwebView respondsToSelector:@selector(setInspectable:)]) {
        [wkwebView performSelector:@selector(setInspectable:) withObject:@true];
    }
#endif
    webView = wkwebView;
    webView.UIDelegate = self;
    webView.navigationDelegate = self;

    if (ua != NULL && strcmp(ua, "") != 0) {
        ((WKWebView *)webView).customUserAgent = [[NSString alloc] initWithUTF8String:ua];
    }

    webView.hidden = YES;

    // cf. https://rick38yip.medium.com/wkwebview-weird-spacing-issue-in-ios-13-54a4fc686f72
    // cf. https://stackoverflow.com/questions/44390971/automaticallyadjustsscrollviewinsets-was-deprecated-in-ios-11-0
    ((WKWebView *)webView).scrollView.contentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentNever;

    webView.backgroundColor = [UIColor clearColor];
    webView.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;

    UIView *view = UnityGetGLViewController().view;
    [view addSubview:webView];

    return self;
}

- (void)dispose
{
    if (webView != nil) {
        WKWebView *webView0 = webView;
        webView = nil;
        if ([webView0 isKindOfClass:[WKWebView class]]) {
            webView0.UIDelegate = nil;
            webView0.navigationDelegate = nil;
            [((WKWebView *)webView0).configuration.userContentController removeScriptMessageHandlerForName:@"saveDataURL"];
            [((WKWebView *)webView0).configuration.userContentController removeScriptMessageHandlerForName:@"unityControl"];
            [((WKWebView *)webView0).configuration.userContentController removeScriptMessageHandlerForName:@"logHandler"];            
        }
        [webView0 stopLoading];
        [webView0 removeFromSuperview];
    }
    delegateCallback = nil;
}

+ (void)resetSharedProcessPool
{
    // cf. https://stackoverflow.com/questions/33156567/getting-all-cookies-from-wkwebview/49744695#49744695
    _sharedProcessPool = [[WKProcessPool alloc] init];
    [_instances enumerateObjectsUsingBlock:^(CWebViewPlugin *obj, NSUInteger idx, BOOL *stop) {
        if ([obj->webView isKindOfClass:[WKWebView class]]) {
            WKWebView *webView = (WKWebView *)obj->webView;
            webView.configuration.processPool = _sharedProcessPool;
        }
    }];
}

- (void)userContentController:(WKUserContentController *)userContentController
      didReceiveScriptMessage:(WKScriptMessage *)message
{
    if ([message.name isEqualToString:@"logHandler"]) {
        [self sendUnityCallback:"CallOnLog" message:[[NSString stringWithFormat:@"%@", message.body] UTF8String]];
    } else if ([message.name isEqualToString:@"unityControl"]) {
        [self sendUnityCallback:"CallFromJS" message:[[NSString stringWithFormat:@"%@", message.body] UTF8String]];
    } else if ([message.name isEqualToString:@"saveDataURL"]) {
        NSRange range = [message.body rangeOfString:@"\t"];
        if (range.location == NSNotFound) {
            return;
        }
        NSString *fileName = [[message.body substringWithRange:NSMakeRange(0, range.location)] lastPathComponent];
        NSString *dataURL = [message.body substringFromIndex:(range.location + 1)];
        range = [dataURL rangeOfString:@"data:"];
        if (range.location != 0) {
            return;
        }
        NSString *tmp = [dataURL substringFromIndex:[@"data:" length]];
        range = [tmp rangeOfString:@";"];
        if (range.location == NSNotFound) {
            return;
        }
        NSString *base64data = [tmp substringFromIndex:(range.location + 1 + [@"base64," length])];
        NSData *data = [[NSData alloc] initWithBase64EncodedString:base64data options:0];
        NSString *path = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES)[0];
        path = [path stringByAppendingString:@"/Downloads"];
        BOOL isDir;
        NSError *err = nil;
        if ([[NSFileManager defaultManager] fileExistsAtPath:path isDirectory:&isDir]) {
            if (!isDir) {
                return;
            }
        } else {
            [[NSFileManager defaultManager] createDirectoryAtPath:path withIntermediateDirectories:YES attributes:nil error:&err];
            if (err != nil) {
                return;
            }
        }
        NSString *prefix  = [path stringByAppendingString:@"/"];
        path = [prefix stringByAppendingString:fileName];
        int count = 0;
        while ([[NSFileManager defaultManager] fileExistsAtPath:path]) {
            count++;
            NSString *name = [fileName stringByDeletingPathExtension];
            NSString *ext = [fileName pathExtension];
            if (ext.length == 0) {
                path = [NSString stringWithFormat:@"%@%@ (%d)", prefix, name, count];
            } else {
                path = [NSString stringWithFormat:@"%@%@ (%d).%@", prefix, name, count, ext];
            }
        }
        [data writeToFile:path atomically:YES];
    }
}

- (void)webViewWebContentProcessDidTerminate:(WKWebView *)webView
{
    [self sendUnityCallback:"CallOnError" message:"webViewWebContentProcessDidTerminate"];
}

- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(WKNavigation *)navigation withError:(NSError *)error
{
    [self sendUnityCallback:"CallOnError" message:[[error description] UTF8String]];
}

- (void)webView:(WKWebView *)webView didFailNavigation:(WKNavigation *)navigation withError:(NSError *)error
{
    [self sendUnityCallback:"CallOnError" message:[[error description] UTF8String]];
}

- (WKWebView *)webView:(WKWebView *)wkWebView createWebViewWithConfiguration:(WKWebViewConfiguration *)configuration forNavigationAction:(WKNavigationAction *)navigationAction windowFeatures:(WKWindowFeatures *)windowFeatures
{
    // cf. for target="_blank", cf. http://qiita.com/ShingoFukuyama/items/b3a1441025a36ab7659c
    if (!navigationAction.targetFrame.isMainFrame) {
        [wkWebView loadRequest:navigationAction.request];
    }
    return nil;
}

- (void)webView:(WKWebView *)wkWebView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    if (webView == nil) {
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }
    NSURL *nsurl = [navigationAction.request URL];
    NSString *url = [nsurl absoluteString];

    if ([url rangeOfString:@"//itunes.apple.com/"].location != NSNotFound) {
        [[UIApplication sharedApplication] openURL:nsurl];
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    } else if ([url hasPrefix:@"unity:"]) {
        [self sendUnityCallback:"CallFromJS" message:[[url substringFromIndex:6] UTF8String]];
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    } else if (![url hasPrefix:@"about:blank"]  // for loadHTML(), cf. #365
               && ![url hasPrefix:@"file:"]
               && ![url hasPrefix:@"http:"]
               && ![url hasPrefix:@"https:"]) {
        if([[UIApplication sharedApplication] canOpenURL:nsurl]) {
            [[UIApplication sharedApplication] openURL:nsurl];
        }
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    } else if (navigationAction.navigationType == WKNavigationTypeLinkActivated
               && (!navigationAction.targetFrame || !navigationAction.targetFrame.isMainFrame)) {
        // cf. for target="_blank", cf. http://qiita.com/ShingoFukuyama/items/b3a1441025a36ab7659c
        [webView load:navigationAction.request];
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }
    [self sendUnityCallback:"CallOnStarted" message:[url UTF8String]];
    decisionHandler(WKNavigationActionPolicyAllow);
}

- (void)webView:(WKWebView *)webView decidePolicyForNavigationResponse:(WKNavigationResponse *)navigationResponse decisionHandler:(void (^)(WKNavigationResponsePolicy))decisionHandler {

    if ([navigationResponse.response isKindOfClass:[NSHTTPURLResponse class]]) {

        NSHTTPURLResponse * response = (NSHTTPURLResponse *)navigationResponse.response;
        if (response.statusCode >= 400) {
            [self sendUnityCallback:"CallOnHttpError" message:[[NSString stringWithFormat:@"%ld", (long)response.statusCode] UTF8String]];
        }

    }
    decisionHandler(WKNavigationResponsePolicyAllow);
}

- (void)sendUnityCallback:(const char *)key message:(const char *)message {
    if (delegateCallback != nil) {
        delegateCallback(key, message);
    } else {
        NSLog(@"delegateCallback is nil, message not sent.");
    }
}

- (void)loadURL:(const char *)url
{
    if (webView == nil)
        return;
        
    WKWebView *_webView = (WKWebView *)webView;
    NSString *urlStr = [NSString stringWithUTF8String:url];
    NSURL *nsurl = [[NSURL alloc] initFileURLWithPath:urlStr];
    NSURLRequest *request = [NSURLRequest requestWithURL:nsurl];
    [_webView load:request];
}

- (void)evaluateJS:(const char *)js
{
    if (webView == nil)
        return;
    NSString *jsStr = [NSString stringWithUTF8String:js];
    [webView evaluateJavaScript:jsStr completionHandler:^(NSString *result, NSError *error) {}];
}

- (void)launchAuthURL:(const char *)url
{
    NSURL *URL = [[NSURL alloc] initWithString: [NSString stringWithUTF8String:url]];
    NSString *scheme = NSBundle.mainBundle.bundleIdentifier;

    _authSession = [[ASWebAuthenticationSession alloc] initWithURL:URL callbackURLScheme:scheme completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error) {
        _authSession = nil;

        if (error != nil && error.code == 1) {
            // Cancelled
            [self sendUnityCallback:"CallFromAuthCallbackError" message: ""];
        } else if (error != nil) {
            [self sendUnityCallback:"CallFromAuthCallbackError" message:error.localizedDescription.UTF8String];
        } else {
            [self sendUnityCallback:"CallFromAuthCallback" message: callbackURL.absoluteString.UTF8String];
        }
    }];

    _authSession.presentationContextProvider = self;
    [_authSession start];
}

- (ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session
{
    return UIApplication.sharedApplication.windows.firstObject;
}

- (void)clearCache:(BOOL)includeDiskFiles
{
    if (webView == nil)
        return;
    NSMutableSet *types = [NSMutableSet setWithArray:@[WKWebsiteDataTypeMemoryCache]];
    if (includeDiskFiles) {
        [types addObject:WKWebsiteDataTypeDiskCache];
    }
    NSDate *date = [NSDate dateWithTimeIntervalSince1970:0];
    [[WKWebsiteDataStore defaultDataStore] removeDataOfTypes:types modifiedSince:date completionHandler:^{}];
}

- (void)clearStorage
{
    if (webView == nil)
        return;
    NSMutableSet *types = [NSMutableSet setWithArray:@[WKWebsiteDataTypeLocalStorage, WKWebsiteDataTypeSessionStorage, WKWebsiteDataTypeWebSQLDatabases, WKWebsiteDataTypeIndexedDBDatabases]];
    NSDate *date = [NSDate dateWithTimeIntervalSince1970:0];
    [[WKWebsiteDataStore defaultDataStore] removeDataOfTypes:types modifiedSince:date completionHandler:^{}];
}
@end

extern "C" {
    void *_CWebViewPlugin_Init(const char *ua);
    void _CWebViewPlugin_Destroy(void *instance);
    void _CWebViewPlugin_LoadURL(void *instance, const char *url);
    void _CWebViewPlugin_EvaluateJS(void *instance, const char *url);
    void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback);
    void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url);
    void _CWebViewPlugin_ClearCache(void *instance, BOOL includeDiskFiles);
    void _CWebViewPlugin_ClearStorage(void *instance);
}

void _CWebViewPlugin_SetDelegate(DelegateCallbackFunction callback) {
    delegateCallback = callback;
}

void *_CWebViewPlugin_Init(const char *ua)
{
    CWebViewPlugin *webViewPlugin = [[CWebViewPlugin alloc] initWithUa:ua];
    [_instances addObject:webViewPlugin];
    return (__bridge_retained void *)webViewPlugin;
}

void _CWebViewPlugin_Destroy(void *instance)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge_transfer CWebViewPlugin *)instance;
    [_instances removeObject:webViewPlugin];
    [webViewPlugin dispose];
    webViewPlugin = nil;
}

void _CWebViewPlugin_LoadURL(void *instance, const char *url)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin loadURL:url];
}

void _CWebViewPlugin_EvaluateJS(void *instance, const char *js)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin evaluateJS:js];
}

void _CWebViewPlugin_LaunchAuthURL(void *instance, const char *url)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin launchAuthURL:url];
}

void _CWebViewPlugin_ClearCache(void *instance, BOOL includeDiskFiles)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin clearCache:includeDiskFiles];
}

void _CWebViewPlugin_ClearStorage(void *instance)
{
    if (instance == NULL)
        return;
    CWebViewPlugin *webViewPlugin = (__bridge CWebViewPlugin *)instance;
    [webViewPlugin clearStorage];
}