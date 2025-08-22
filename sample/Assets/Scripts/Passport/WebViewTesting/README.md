# 🧪 WebView Testing Framework

This framework demonstrates and validates the **Volt Unity Web Browser (UWB)** implementation for Immutable Passport login integration.

## 🎯 Purpose

After comprehensive evaluation, **Volt Unity Web Browser has been selected** as the optimal solution:

- ✅ **Production-ready authentication** - Handles complex OAuth flows (Google, Okta, Yubikey)
- ✅ **MIT Licensed** - Free for commercial SDK distribution
- ✅ **Already integrated** - No new dependencies required
- ✅ **Multi-platform support** - Windows and Mac compatibility
- ✅ **Modern web standards** - Based on Chromium Embedded Framework (CEF)

## 🖥️ Platform Requirements

**Supported Platforms:**

- ✅ **Windows** - Full support with CEF engine
- ✅ **macOS** - Full support with CEF engine
- ❌ **Mobile** (iOS/Android) - Not supported by Volt Unity Web Browser
- ❌ **Linux** - Not supported by Immutable SDK

**Note:** The framework will display a warning message on unsupported platforms.

## 📁 Structure

```text
WebViewTesting/
├── WebViewTestManager.cs          # Main test controller with navigation & debug tools
├── IWebViewAdapter.cs             # Common interface for WebView implementations
├── VoltUnityWebBrowserAdapter.cs  # Complete UWB implementation (PRODUCTION READY)
├── WebViewTestSceneSetup.cs       # Editor utility to create test scene
├── test-message-page.html         # Test page for JavaScript ↔ Unity messaging
└── README.md                      # This file
```

## 🚀 Quick Start

### 1. Create Test Scene

```csharp
// In Unity Editor:
Immutable → WebView Testing → Create WebView Test Scene
```

### 2. Test UWB Implementation

The Volt Unity Web Browser is already integrated! No additional packages needed.

### 3. Run Tests

1. **Test Login Page** - Load Passport sample app (`https://passport.immutable.com/sdk-sample-app`)
2. **Navigate** - Use URL bar to test different sites
3. **Test Input** - Inject input debugging JavaScript
4. **Test Popup** - Verify popup handling (opens in external browser)
5. **Find WebView** - Locate WebView GameObject in hierarchy for inspector access
6. **Test Messaging** - Test JavaScript ↔ Unity communication

## 🧪 Test Scenarios

### Login Page Test

- ✅ **Load Passport login page** (`https://auth.immutable.com`)
- ✅ **Render CSS3 features** (rounded corners, gradients, fonts)
- ✅ **Interactive elements** (buttons, forms, dropdowns)
- ✅ **OAuth flow handling** (redirects, callbacks)
- ✅ **Performance monitoring** (FPS, memory usage)

### Message Passing Test

- ✅ **Unity → JavaScript** communication
- ✅ **JavaScript → Unity** communication  
- ✅ **OAuth callback simulation**
- ✅ **Performance data exchange**
- ✅ **Error handling**

## 📊 Evaluation Criteria

Rate each WebView package (1-5 stars):

| Criteria | Weight | Description |
|----------|--------|-------------|
| **Rendering Quality** | High | CSS3 support, font clarity, animations |
| **Performance** | High | FPS, memory usage, startup time |
| **Input Handling** | Medium | Mouse, keyboard responsiveness |
| **Message Passing** | High | JavaScript ↔ Unity communication |
| **Setup Complexity** | Medium | Import → working in minutes |
| **Documentation** | Low | Examples, API docs quality |
| **Licensing Cost** | High | SDK distribution feasibility |
| **Platform Support** | Medium | Windows, macOS, Linux |

## 🔧 Implementation Guide

### Adding a New WebView Package

1. **Create Adapter Class**:

```csharp
public class MyWebViewAdapter : IWebViewAdapter
{
    // Implement all interface methods
}
```

1. **Update WebViewTestManager**:

```csharp
// Add to WebViewPackage enum
public enum WebViewPackage
{
    MyWebView // Add here
}

// Add to CreateWebViewAdapter method
case WebViewPackage.MyWebView:
    return new MyWebViewAdapter();
```

1. **Test Implementation**:

- Run login page test
- Verify message passing
- Check performance metrics

### Message Passing Implementation

**JavaScript → Unity**:

```javascript
// In WebView
window.unityInstance.SendMessage('WebViewTestManager', 'OnTestMessage', 'Hello Unity!');
```

**Unity → JavaScript**:

```csharp
// In Unity
webView.ExecuteJavaScript("window.receiveUnityMessage('Hello WebView!');");
```

## 📝 Test Results Template

```markdown
## WebView Package: [Package Name]

### Setup
- **Import Time**: X minutes
- **Complexity**: Easy/Medium/Hard
- **Dependencies**: List any required dependencies

### Login Page Test
- **Rendering Quality**: ⭐⭐⭐⭐⭐ (5/5)
- **Load Time**: X seconds
- **Interactive Elements**: Working/Broken
- **CSS3 Support**: Full/Partial/None

### Performance
- **Average FPS**: X fps
- **Memory Usage**: X MB
- **Startup Time**: X seconds

### Message Passing
- **Unity → JS**: Working/Broken
- **JS → Unity**: Working/Broken
- **OAuth Simulation**: Working/Broken

### Licensing
- **Cost**: $X or Free
- **SDK Distribution**: Allowed/Restricted
- **Commercial Use**: Allowed/Restricted

### Overall Rating: ⭐⭐⭐⭐⭐ (X/5)
**Recommendation**: Use/Don't Use for SDK
**Notes**: Additional observations...
```

## 🔗 Test Resources

- **Login Page**: `https://auth.immutable.com`
- **Message Test Page**: `StreamingAssets/test-message-page.html`
- **OAuth Callback**: `immutablerunner://callback?code=test&state=test`

## 🎯 Success Criteria

A WebView package is suitable for SDK distribution if:

1. ✅ **Renders login page correctly** (all elements visible and interactive)
2. ✅ **Maintains 30+ FPS** during normal operation  
3. ✅ **Supports JavaScript ↔ Unity** message passing
4. ✅ **Licensing allows SDK distribution** (free or reasonable cost)
5. ✅ **Easy integration** (< 1 hour setup for developers)
6. ✅ **Handles OAuth callbacks** properly

## 🚨 Known Issues

- **Template adapters** need actual WebView API implementation
- **Message passing** methods vary between packages
- **Performance metrics** may need package-specific implementation
- **IL2CPP compatibility** should be tested for each package

## 📞 Support

For questions about this testing framework:

1. Check the adapter implementation for your WebView package
2. Review the test HTML page for message passing examples
3. Consult the WebView package documentation
4. Test with the actual Passport login page

---

**Happy Testing!** 🧪✨
