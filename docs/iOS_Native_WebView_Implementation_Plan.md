# iOS Native WebView Implementation Plan

## Epic Overview
Implement native iOS WKWebView integration to replace Vuplex WebView dependency for iOS platform, while maintaining compatibility with existing PassportUI architecture.

## Goals
- Replace Vuplex WebView with native iOS WKWebView
- Maintain `IPassportWebView` interface compatibility
- Support Passport authentication flow with JavaScript bridge
- Handle OAuth callbacks natively without external browser
- Eliminate third-party WebView dependency for iOS

## Technical Architecture

### High-Level Components
```
Unity C# Layer (PassportUI)
    ↕ (P/Invoke calls)
iOS Native Plugin (Objective-C)
    ↕ (WKWebView delegate methods)
WKWebView (Native iOS WebView)
    ↕ (JavaScript execution & message handling)
Passport Web Authentication Page
```

### Important Architectural Decisions

#### 1. WebView as Message Relay Only
The native WebView acts as a **simple message relay** between the Passport web page and Unity. It does NOT directly communicate with Passport's JavaScript bridge.

**Message Flow**:
```
Passport Web Page 
  → window.webkit.messageHandlers.unity.postMessage()
    → Native WebView (WKScriptMessageHandler)
      → Unity SDK (via P/Invoke callback)
        → PassportUI processes message
          → Forwards to Passport Bridge if needed
```

**Why?**
- Keeps WebView implementation simple and reusable
- Unity SDK maintains control over Passport bridge communication
- Easier to debug and maintain separation of concerns

#### 2. Configurable URL Schemes
Each game defines its own deep link URL scheme (e.g., `mygame://`, `awesomegame://`), not hardcoded to `immutablerunner://`.

**Configuration Flow**:
```csharp
// Game developer configures their URL scheme
var config = new PassportWebViewConfig {
    CustomURLScheme = "mygame" // Without "://"
};

// Unity SDK passes to native WebView
PassportWebView_SetCustomURLScheme(webViewPtr, "mygame");

// Native WebView registers handler for "mygame://" URLs
```

**Why?**
- Each game needs unique URL scheme for App Store requirements
- Allows multiple Immutable games on same device
- Follows iOS best practices for custom URL schemes

### Key Integration Points
1. **Unity ↔ Objective-C Bridge**: P/Invoke calls for WebView control
2. **JavaScript Bridge**: WKWebView message handlers for authentication events
3. **OAuth Callback Handling**: Custom URL scheme interception
4. **Memory Management**: Proper cleanup between Unity and native code

## Detailed Implementation Plan

### Phase 1: Project Setup & Native Plugin Foundation (2 days)

**Goal**: Create a minimal working WebView that can be shown, hidden, and load URLs. You'll be able to see the WebView displaying web pages, but authentication callbacks won't work yet.

**What Works After Phase 1**:
- ✅ Create and destroy WebView
- ✅ Show/hide WebView on screen
- ✅ Load any URL (including Passport auth page)
- ✅ Navigate and interact with web pages
- ✅ Set WebView position and size
- ❌ JavaScript callbacks (Phase 4)
- ❌ OAuth deep link handling (Phase 4)
- ❌ Authentication flow completion (Phase 4)

#### 1.1 Create iOS Plugin Structure (0.5 days)
```
src/Packages/Passport/Runtime/Plugins/iOS/
├── PassportWebView.h           # Header file with C interface
├── PassportWebView.h.meta      # Unity meta file for iOS platform
├── PassportWebView.mm          # Objective-C++ implementation
└── PassportWebView.mm.meta     # Unity meta file with framework dependencies
```

**Note**: iOS plugins must be inside the Unity package structure (`src/Packages/Passport/Runtime/Plugins/iOS/`) for Unity to recognize and include them in iOS builds. The `.meta` files configure platform-specific settings and framework dependencies (WebKit, UIKit).

#### 1.2 Define C Interface (0.5 days)
Create `PassportWebView.h`:
```c
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
```

**Key Changes**:
- Added `PassportWebView_SetCustomURLScheme()` to allow games to configure their own deep link URL scheme (e.g., "mygame", "awesomegame") instead of hardcoding "immutablerunner"

#### 1.3 Basic Objective-C Implementation (1 day)

**Goal**: Implement enough to create a visible WebView and load URLs.

Create `PassportWebView.mm`:
```objc
#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <UIKit/UIKit.h>
#import "PassportWebView.h"
#import "PassportWebViewDelegate.h"

@interface PassportWebViewWrapper : NSObject
@property (strong, nonatomic) WKWebView* webView;
@property (strong, nonatomic) PassportWebViewDelegate* delegate;
@property (strong, nonatomic) UIView* containerView;
@property (strong, nonatomic) NSString* customURLScheme;
@end

@implementation PassportWebViewWrapper
// Implementation details in Phase 2
@end

// C interface implementation for setting custom URL scheme
void PassportWebView_SetCustomURLScheme(void* webViewPtr, const char* urlScheme) {
    PassportWebViewWrapper* wrapper = (__bridge PassportWebViewWrapper*)webViewPtr;
    wrapper.customURLScheme = [NSString stringWithUTF8String:urlScheme];
    // Re-configure WKWebView with new URL scheme if needed
}

// C interface implementations
void* PassportWebView_Create(const char* gameObjectName) {
    PassportWebViewWrapper* wrapper = [[PassportWebViewWrapper alloc] init];
    // Initialize WKWebView and delegate
    return (__bridge_retained void*)wrapper;
}

void PassportWebView_Destroy(void* webViewPtr) {
    PassportWebViewWrapper* wrapper = (__bridge_transfer PassportWebViewWrapper*)webViewPtr;
    // Cleanup
    wrapper = nil;
}

// Additional C functions...
```

#### 1.4 Phase 1 Testing - Verify Basic WebView Works (0.5 days)

After implementing Phase 1, you should be able to test basic WebView functionality:

**Test Script** (`TestPhase1WebView.cs`):
```csharp
using UnityEngine;
using Immutable.Passport;

public class TestPhase1WebView : MonoBehaviour
{
    private iOSNativePassportWebView webView;
    
    void Start()
    {
        Debug.Log("Phase 1 Test: Initializing iOS Native WebView...");
        
        var config = new PassportWebViewConfig
        {
            Width = 400,
            Height = 600,
            InitialUrl = "https://www.google.com" // Simple test URL
        };
        
        webView = new iOSNativePassportWebView();
        webView.Initialize(config);
        
        Debug.Log("Phase 1 Test: WebView initialized");
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 300));
        
        if (GUILayout.Button("Show WebView"))
        {
            Debug.Log("Phase 1 Test: Showing WebView");
            webView.Show();
        }
        
        if (GUILayout.Button("Hide WebView"))
        {
            Debug.Log("Phase 1 Test: Hiding WebView");
            webView.Hide();
        }
        
        if (GUILayout.Button("Load Google"))
        {
            Debug.Log("Phase 1 Test: Loading Google");
            webView.LoadUrl("https://www.google.com");
        }
        
        if (GUILayout.Button("Load Passport"))
        {
            Debug.Log("Phase 1 Test: Loading Passport (won't complete auth yet)");
            webView.LoadUrl("https://auth.immutable.com/im-embedded-login-prompt");
        }
        
        GUILayout.EndArea();
    }
    
    void OnDestroy()
    {
        webView?.Dispose();
    }
}
```

**Expected Results After Phase 1**:
1. ✅ WebView appears on screen when "Show WebView" is pressed
2. ✅ WebView disappears when "Hide WebView" is pressed
3. ✅ Google loads and displays correctly
4. ✅ Passport auth page loads and displays login options
5. ✅ You can interact with web pages (scroll, click links)
6. ❌ Clicking "Login" on Passport page won't complete (no callbacks yet)
7. ❌ OAuth redirects won't be captured (no URL scheme handling yet)

**What You'll See**:
- A WebView overlay displaying web content
- Ability to navigate to any website
- Passport login page displaying correctly
- Web interactions working (scrolling, clicking)

**What Won't Work Yet**:
- Authentication completion
- JavaScript messages from web page to Unity
- Deep link callback handling

This validates the core WebView infrastructure before adding authentication features.

### Phase 2: WKWebView Integration (2 days)

#### 2.1 WKWebView Configuration (1 day)
Implement WKWebView setup with proper configuration:
```objc
- (void)setupWebView {
    WKWebViewConfiguration* config = [[WKWebViewConfiguration alloc] init];
    
    // Enable JavaScript
    config.preferences.javaScriptEnabled = YES;
    
    // Configure message handlers for JavaScript bridge
    // Note: We use "unity" as the message handler name, NOT "PassportBridge"
    // The WebView should NOT directly communicate with Passport's JS bridge
    // Unity SDK will handle all Passport bridge communication
    WKUserContentController* contentController = [[WKUserContentController alloc] init];
    [contentController addScriptMessageHandler:self.delegate name:@"unity"];
    config.userContentController = contentController;
    
    // Configure URL scheme handling (use custom scheme from config)
    NSString* urlScheme = self.customURLScheme ?: @"immutablerunner"; // Default fallback
    [config setURLSchemeHandler:self.delegate forURLScheme:urlScheme];
    
    // Create WebView
    self.webView = [[WKWebView alloc] initWithFrame:CGRectZero configuration:config];
    self.webView.navigationDelegate = self.delegate;
    self.webView.UIDelegate = self.delegate;
    
    // Add to Unity view hierarchy
    UIViewController* unityViewController = UnityGetGLViewController();
    [unityViewController.view addSubview:self.webView];
}
```

#### 2.2 WKWebView Delegate Implementation (1 day)
Create `PassportWebViewDelegate.mm`:
```objc
@implementation PassportWebViewDelegate

- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation {
    NSString* url = webView.URL.absoluteString;
    if (onLoadFinishedCallback) {
        onLoadFinishedCallback([url UTF8String]);
    }
}

- (void)userContentController:(WKUserContentController *)userContentController 
      didReceiveScriptMessage:(WKScriptMessage *)message {
    // Handle messages from the WebView's JavaScript
    // This is for WebView → Unity communication, NOT Passport bridge communication
    // The Unity SDK will handle forwarding to Passport's bridge if needed
    if ([message.name isEqualToString:@"unity"]) {
        NSDictionary* messageData = message.body;
        NSString* method = messageData[@"method"];
        NSString* data = messageData[@"data"];
        
        if (onJavaScriptMessageCallback) {
            onJavaScriptMessageCallback([method UTF8String], [data UTF8String]);
        }
    }
}

- (void)webView:(WKWebView *)webView 
startURLSchemeTask:(id<WKURLSchemeTask>)urlSchemeTask {
    NSURL* url = urlSchemeTask.request.URL;
    
    // Check if this matches the configured custom URL scheme
    // Note: Each game will have their own scheme (e.g., "mygame://", "awesomegame://")
    NSString* expectedScheme = self.wrapper.customURLScheme ?: @"immutablerunner";
    
    if ([url.scheme isEqualToString:expectedScheme]) {
        // Handle OAuth callback - notify Unity
        if (onURLChangedCallback) {
            onURLChangedCallback([url.absoluteString UTF8String]);
        }
        
        // Complete the task to prevent hanging
        NSHTTPURLResponse* response = [[NSHTTPURLResponse alloc] 
            initWithURL:url statusCode:200 HTTPVersion:@"HTTP/1.1" headerFields:nil];
        [urlSchemeTask didReceiveResponse:response];
        [urlSchemeTask didFinish];
    }
}

@end
```

### Phase 3: Unity C# Integration (1.5 days)

#### 3.1 Create iOS Native WebView Class (1 day)
Create `src/Packages/Passport/Runtime/Scripts/Private/UI/WebViews/iOSNativePassportWebView.cs`:
```csharp
#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Immutable.Passport.Core.Logging;

namespace Immutable.Passport
{
    public class iOSNativePassportWebView : IPassportWebView
    {
        private const string TAG = "[iOSNativePassportWebView]";
        
        // P/Invoke declarations
        [DllImport("__Internal")]
        private static extern IntPtr PassportWebView_Create(string gameObjectName);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_Destroy(IntPtr webViewPtr);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_LoadURL(IntPtr webViewPtr, string url);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_Show(IntPtr webViewPtr);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_Hide(IntPtr webViewPtr);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_SetFrame(IntPtr webViewPtr, float x, float y, float width, float height);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_ExecuteJavaScript(IntPtr webViewPtr, string script);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_SetCustomURLScheme(IntPtr webViewPtr, string urlScheme);
        
        // Callback delegates
        private delegate void OnLoadFinishedDelegate(string url);
        private delegate void OnJavaScriptMessageDelegate(string method, string data);
        private delegate void OnURLChangedDelegate(string url);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnLoadFinishedCallback(OnLoadFinishedDelegate callback);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnJavaScriptMessageCallback(OnJavaScriptMessageDelegate callback);
        
        [DllImport("__Internal")]
        private static extern void PassportWebView_SetOnURLChangedCallback(OnURLChangedDelegate callback);
        
        // Instance variables
        private IntPtr webViewPtr;
        private PassportWebViewConfig config;
        private bool isInitialized = false;
        private bool isVisible = false;
        
        // Events
        public event Action<string> OnJavaScriptMessage;
        public event Action OnLoadFinished;
        public event Action OnLoadStarted;
        
        // Properties
        public bool IsVisible => isVisible;
        public string CurrentUrl { get; private set; }
        
        public void Initialize(PassportWebViewConfig config)
        {
            if (isInitialized) return;
            
            this.config = config ?? new PassportWebViewConfig();
            
            try
            {
                PassportLogger.Info($"{TAG} Initializing iOS native WebView...");
                
                // Create native WebView
                webViewPtr = PassportWebView_Create("PassportWebView");
                
                // Configure custom URL scheme from PassportWebViewConfig
                if (!string.IsNullOrEmpty(config.CustomURLScheme))
                {
                    PassportWebView_SetCustomURLScheme(webViewPtr, config.CustomURLScheme);
                    PassportLogger.Info($"{TAG} Custom URL scheme set: {config.CustomURLScheme}");
                }
                
                // Set up callbacks
                PassportWebView_SetOnLoadFinishedCallback(OnLoadFinishedCallback);
                PassportWebView_SetOnJavaScriptMessageCallback(OnJavaScriptMessageCallback);
                PassportWebView_SetOnURLChangedCallback(OnURLChangedCallback);
                
                isInitialized = true;
                PassportLogger.Info($"{TAG} iOS native WebView initialized successfully");
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"{TAG} Failed to initialize: {ex.Message}");
                throw;
            }
        }
        
        public void LoadUrl(string url)
        {
            if (!isInitialized) return;
            
            CurrentUrl = url;
            PassportWebView_LoadURL(webViewPtr, url);
            PassportLogger.Info($"{TAG} Loading URL: {url}");
        }
        
        public void Show()
        {
            if (!isInitialized) return;
            
            // Set frame based on config
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float x = (screenWidth - config.Width) / 2f;
            float y = (screenHeight - config.Height) / 2f;
            
            PassportWebView_SetFrame(webViewPtr, x, y, config.Width, config.Height);
            PassportWebView_Show(webViewPtr);
            
            isVisible = true;
            PassportLogger.Info($"{TAG} WebView shown");
        }
        
        public void Hide()
        {
            if (!isInitialized) return;
            
            PassportWebView_Hide(webViewPtr);
            isVisible = false;
            PassportLogger.Info($"{TAG} WebView hidden");
        }
        
        public void ExecuteJavaScript(string js)
        {
            if (!isInitialized) return;
            
            PassportWebView_ExecuteJavaScript(webViewPtr, js);
        }
        
        public void RegisterJavaScriptMethod(string methodName, Action<string> handler)
        {
            // iOS uses WKScriptMessageHandler, so we register handlers differently
            // The native side will call OnJavaScriptMessageCallback with method name
            OnJavaScriptMessage += (message) => {
                // Parse message format: "methodName:data"
                var parts = message.Split(new[] { ':' }, 2);
                if (parts.Length == 2 && parts[0] == methodName)
                {
                    handler?.Invoke(parts[1]);
                }
            };
        }
        
        public void Dispose()
        {
            if (webViewPtr != IntPtr.Zero)
            {
                PassportWebView_Destroy(webViewPtr);
                webViewPtr = IntPtr.Zero;
            }
            
            isInitialized = false;
            isVisible = false;
            PassportLogger.Info($"{TAG} iOS native WebView disposed");
        }
        
        // Callback implementations
        [AOT.MonoPInvokeCallback(typeof(OnLoadFinishedDelegate))]
        private static void OnLoadFinishedCallback(string url)
        {
            // Find the active instance and call its event
            // Note: In production, you'd maintain a registry of instances
            PassportLogger.Info($"[iOSNativePassportWebView] Load finished: {url}");
        }
        
        [AOT.MonoPInvokeCallback(typeof(OnJavaScriptMessageDelegate))]
        private static void OnJavaScriptMessageCallback(string method, string data)
        {
            PassportLogger.Info($"[iOSNativePassportWebView] JS Message - Method: {method}, Data: {data}");
            // Handle JavaScript messages from Passport authentication page
        }
        
        [AOT.MonoPInvokeCallback(typeof(OnURLChangedDelegate))]
        private static void OnURLChangedCallback(string url)
        {
            PassportLogger.Info($"[iOSNativePassportWebView] URL changed: {url}");
            // Handle OAuth callback URLs
        }
    }
}
#endif
```

#### 3.2 Update PassportUI Platform Detection (0.5 days)
Update `PassportUI.cs`:
```csharp
private IPassportWebView CreatePlatformWebView()
{
#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_EDITOR && UNITY_EDITOR_WIN))
    return new WindowsPassportWebView(rawImage, this);
#elif UNITY_IOS && !UNITY_EDITOR
    // Use native iOS WebView
    return new iOSNativePassportWebView();
#elif UNITY_IOS && VUPLEX_WEBVIEW
    // Fallback to Vuplex for iOS (Editor mode or if native fails)
    return new iOSPassportWebView(rawImage);
#elif UNITY_ANDROID && VUPLEX_WEBVIEW
    return new AndroidVuplexWebView(rawImage);
#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX) && VUPLEX_WEBVIEW
    return new MacOSPassportWebView(rawImage);
#else
    return null;
#endif
}
```

### Phase 4: JavaScript Bridge Implementation (1.5 days)

#### 4.1 Passport Web Page Integration (1 day)
The Passport authentication page needs to communicate with Unity through the native WebView. The WebView acts as a simple message relay - it does NOT directly interact with Passport's JS bridge.

**Architecture**:
```
Passport Web Page → Native WebView (via "unity" handler) → Unity SDK → Passport Bridge
```

Update the JavaScript to detect and use the native WebView handler:

```javascript
// In the Passport web page (index.html)
function sendMessageToUnity(method, data) {
    if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unity) {
        // iOS Native WKWebView - send to "unity" handler, NOT "PassportBridge"
        window.webkit.messageHandlers.unity.postMessage({
            method: method,
            data: data
        });
    } else if (window.vuplex && window.vuplex.postMessage) {
        // Vuplex fallback
        window.vuplex.postMessage({
            method: method,
            data: data
        });
    } else {
        // Fallback for other WebView implementations
        console.warn('No WebView message handler available');
    }
}

// Usage examples
function handleLoginSuccess(authData) {
    sendMessageToUnity('HandleLoginData', JSON.stringify(authData));
}

function handleLoginError(error) {
    sendMessageToUnity('HandleLoginError', JSON.stringify(error));
}

function handleClose() {
    sendMessageToUnity('HandleClose', '');
}
```

#### 4.2 OAuth Callback Handling (0.5 days)
Implement custom URL scheme handling in the native plugin:
```objc
// In PassportWebViewDelegate.mm
- (void)webView:(WKWebView *)webView 
startURLSchemeTask:(id<WKURLSchemeTask>)urlSchemeTask {
    NSURL* url = urlSchemeTask.request.URL;
    
    if ([url.scheme isEqualToString:@"immutablerunner"]) {
        PassportLogger.Info($"Handling OAuth callback: {url.absoluteString}");
        
        // Extract authentication data from URL
        NSURLComponents* components = [NSURLComponents componentsWithURL:url resolvingAgainstBaseURL:NO];
        NSString* authCode = nil;
        NSString* state = nil;
        
        for (NSURLQueryItem* item in components.queryItems) {
            if ([item.name isEqualToString:@"code"]) {
                authCode = item.value;
            } else if ([item.name isEqualToString:@"state"]) {
                state = item.value;
            }
        }
        
        if (authCode) {
            // Send success callback to Unity
            NSDictionary* authData = @{
                @"code": authCode,
                @"state": state ?: @""
            };
            NSString* jsonData = [self dictionaryToJSON:authData];
            
            if (onJavaScriptMessageCallback) {
                onJavaScriptMessageCallback("HandleLoginData", [jsonData UTF8String]);
            }
        }
        
        // Complete the URL scheme task
        NSHTTPURLResponse* response = [[NSHTTPURLResponse alloc] 
            initWithURL:url statusCode:200 HTTPVersion:@"HTTP/1.1" headerFields:nil];
        [urlSchemeTask didReceiveResponse:response];
        [urlSchemeTask didFinish];
    }
}
```

### Phase 5: Testing & Integration (1 day)

#### 5.1 Local Testing Setup
1. **iOS Device/Simulator Setup**:
   ```bash
   # Build for iOS
   Unity → File → Build Settings → iOS
   Unity → Player Settings → iOS Settings:
   - Bundle Identifier: com.immutable.passport.test
   - Target minimum iOS Version: 12.0
   - Architecture: ARM64 (for device), x86_64 (for simulator)
   ```

2. **Xcode Project Configuration**:
   ```xml
   <!-- Add to Info.plist for custom URL scheme -->
   <key>CFBundleURLTypes</key>
   <array>
       <dict>
           <key>CFBundleURLName</key>
           <string>immutablerunner</string>
           <key>CFBundleURLSchemes</key>
           <array>
               <string>immutablerunner</string>
           </array>
       </dict>
   </array>
   ```

3. **Debug Logging**:
   ```csharp
   // Add debug logging to track WebView lifecycle
   public class iOSWebViewDebugger : MonoBehaviour
   {
       void Start()
       {
           Application.logMessageReceived += HandleLog;
       }
       
       void HandleLog(string logString, string stackTrace, LogType type)
       {
           if (logString.Contains("[iOSNativePassportWebView]"))
           {
               Debug.Log($"iOS WebView: {logString}");
           }
       }
   }
   ```

#### 5.2 Test Cases
1. **WebView Lifecycle**:
   - Create WebView → Should initialize without errors
   - Show WebView → Should display on screen with correct dimensions
   - Load URL → Should navigate to Passport authentication page
   - Hide WebView → Should remove from screen
   - Dispose WebView → Should cleanup native resources

2. **Authentication Flow**:
   - Load Passport login page → Should display login options
   - Complete authentication → Should receive `HandleLoginData` callback
   - Handle authentication error → Should receive `HandleLoginError` callback
   - OAuth callback → Should handle `immutablerunner://callback` URLs

3. **JavaScript Bridge**:
   - Execute JavaScript → Should run in WebView context
   - Receive JavaScript messages → Should call Unity callbacks
   - Message serialization → Should properly parse JSON data

#### 5.3 Performance Testing
```csharp
// Memory usage monitoring
public class WebViewPerformanceMonitor : MonoBehaviour
{
    private float memoryBefore;
    private float memoryAfter;
    
    public void StartMonitoring()
    {
        memoryBefore = Profiler.GetTotalAllocatedMemory(false);
    }
    
    public void StopMonitoring()
    {
        memoryAfter = Profiler.GetTotalAllocatedMemory(false);
        float memoryDelta = (memoryAfter - memoryBefore) / 1024f / 1024f; // MB
        Debug.Log($"WebView memory usage: {memoryDelta:F2} MB");
    }
}
```

## Integration Points Summary

### 1. Unity → Native
- **P/Invoke calls** for WebView control (create, show, hide, load URL)
- **Configuration data** passed through C interface
- **Memory management** with proper retain/release cycles

### 2. Native → Unity
- **Callback functions** for WebView events (load finished, JavaScript messages)
- **OAuth callback handling** through custom URL schemes
- **Error reporting** through callback mechanisms

### 3. JavaScript → Native → Unity
- **WKScriptMessageHandler** for receiving JavaScript messages
- **Message parsing** and forwarding to Unity
- **Authentication data extraction** from JavaScript events

### 4. Unity → JavaScript
- **JavaScript execution** through WKWebView's `evaluateJavaScript`
- **Configuration injection** for Passport authentication parameters
- **Event handling setup** for authentication flow

## Local Testing Guide

### Prerequisites
- Unity 2021.3+ with iOS Build Support
- Xcode 14+ 
- iOS device or simulator (iOS 12+)
- Valid Apple Developer account for device testing

### Step-by-Step Testing

1. **Setup Test Project**:
   ```bash
   # Clone repository
   git clone <repository-url>
   cd unity-immutable-sdk
   
   # Open in Unity
   Unity Hub → Add → Select project folder
   ```

2. **Configure iOS Build**:
   ```
   Unity → File → Build Settings → iOS
   Player Settings → iOS Settings:
   - Bundle Identifier: com.immutable.passport.test
   - Minimum iOS Version: 12.0
   - Target Device: iPhone + iPad
   - Architecture: ARM64
   ```

3. **Build and Deploy**:
   ```bash
   # Build iOS project
   Unity → File → Build Settings → Build
   
   # Open in Xcode
   open <build-folder>/Unity-iPhone.xcodeproj
   
   # Configure signing and deploy to device/simulator
   ```

4. **Test Authentication Flow**:
   ```
   1. Launch app on device/simulator
   2. Tap "Login with Passport" button
   3. Verify WebView displays authentication page
   4. Complete authentication flow
   5. Verify OAuth callback is handled correctly
   6. Check Unity console for debug logs
   ```

5. **Debug Common Issues**:
   ```objc
   // Add to native code for debugging
   NSLog(@"WebView created: %@", self.webView);
   NSLog(@"Loading URL: %@", url);
   NSLog(@"Received message: %@ with data: %@", method, data);
   ```

### Automated Testing
```csharp
// Unit tests for iOS WebView
[TestFixture]
public class iOSNativeWebViewTests
{
    private iOSNativePassportWebView webView;
    
    [SetUp]
    public void Setup()
    {
        var config = new PassportWebViewConfig
        {
            Width = 400,
            Height = 600,
            InitialUrl = "about:blank"
        };
        
        webView = new iOSNativePassportWebView();
        webView.Initialize(config);
    }
    
    [Test]
    public void TestWebViewInitialization()
    {
        Assert.IsNotNull(webView);
        Assert.IsFalse(webView.IsVisible);
    }
    
    [Test]
    public void TestShowHideWebView()
    {
        webView.Show();
        Assert.IsTrue(webView.IsVisible);
        
        webView.Hide();
        Assert.IsFalse(webView.IsVisible);
    }
    
    [TearDown]
    public void Cleanup()
    {
        webView?.Dispose();
    }
}
```

## Risk Mitigation

### High-Risk Areas
1. **Memory Leaks**: Implement proper cleanup in Dispose()
2. **Threading Issues**: Ensure callbacks execute on main thread
3. **OAuth Callback Handling**: Test various authentication scenarios
4. **JavaScript Bridge Reliability**: Implement message queuing and retry logic

### Fallback Strategy
- Keep existing Vuplex implementation as fallback
- Add configuration option to choose WebView implementation
- Implement graceful degradation if native WebView fails

## Success Criteria
- [ ] Native iOS WebView displays Passport authentication page
- [ ] JavaScript bridge successfully handles authentication events
- [ ] OAuth callbacks are intercepted and processed correctly
- [ ] Memory usage remains stable during WebView lifecycle
- [ ] Authentication flow completes successfully on iOS device
- [ ] No crashes or memory leaks during extended testing
- [ ] Performance is equal or better than Vuplex implementation

## Estimated Timeline: 8 days
- **Phase 1: Project Setup (2 days)** - ✅ **Testable milestone**: Basic WebView visible and loading URLs
- Phase 2: WKWebView Integration (2 days)  
- Phase 3: Unity Integration (1.5 days)
- Phase 4: JavaScript Bridge (1.5 days) - ✅ **Testable milestone**: Full authentication flow working
- Phase 5: Testing & Integration (1 day)

### Incremental Testing Strategy
The implementation is designed so you can test and validate at each phase:
- **After Phase 1**: WebView displays and loads web pages (no auth yet)
- **After Phase 2**: WKWebView fully configured with delegates
- **After Phase 3**: Unity integration complete
- **After Phase 4**: Full authentication flow working end-to-end
- **After Phase 5**: Production-ready with comprehensive tests

This plan provides a comprehensive roadmap for implementing native iOS WebView support while maintaining compatibility with the existing PassportUI architecture.

---

## Answers to Key Questions

### Q1: Why `plugins/iOS/` instead of `src/Packages/Passport/Plugins/`?
**A**: Following the existing project structure where native plugins live in the root `plugins/` directory:
- `plugins/Android/` - Android native code
- `plugins/Mac/` - macOS native code  
- `plugins/iOS/` - iOS native code (new)

This keeps all platform-specific native code in one consistent location, separate from the Unity package structure.

### Q2: How do we handle different games having different deep link URLs?
**A**: The native WebView accepts a configurable URL scheme through `PassportWebView_SetCustomURLScheme()`:

```csharp
// Game developer configures their unique scheme
var config = new PassportWebViewConfig {
    CustomURLScheme = "mygame" // Each game uses their own
};
```

The native WebView then registers a handler for that specific scheme (e.g., `mygame://callback`). No hardcoded `immutablerunner://` URLs.

### Q3: Does the WebView communicate directly with Passport's JS bridge?
**A**: **No**. The WebView uses `window.webkit.messageHandlers.unity` (NOT `PassportBridge`) and acts as a simple message relay:

**Correct Flow**:
```
Passport Web Page 
  → unity handler 
    → Native WebView 
      → Unity SDK 
        → Passport Bridge (if needed)
```

**Why?**
- Separation of concerns - WebView doesn't need to know about Passport
- Unity SDK maintains full control over bridge communication
- WebView remains reusable for other purposes
- Easier to test and debug

The WebView is just a display and message transport layer. All Passport-specific logic stays in the Unity SDK layer.
