# 🧪 WebView Testing Framework

This framework provides a standardized way to test different WebView packages for Unity integration with Immutable Passport.

## 🎯 Purpose

- **Compare WebView packages** (Volt Unity Web Browser, Alacrity, UWebView2, ZenFulcrum, Vuplex 3D WebView)
- **Test login page rendering** with actual Passport authentication
- **Evaluate message passing** between JavaScript and Unity
- **Measure performance** (FPS, memory usage, rendering quality)
- **Validate SDK integration** for distribution

## 📁 Structure

```
WebViewTesting/
├── WebViewTestManager.cs          # Main test controller
├── IWebViewAdapter.cs             # Common interface for all WebView packages
├── AlacrityWebViewAdapter.cs      # Alacrity WebView implementation
├── UWebView2Adapter.cs            # UWebView2 implementation  
├── ZenFulcrumWebViewAdapter.cs    # ZenFulcrum implementation
├── Vuplex3DWebViewAdapter.cs      # Vuplex 3D WebView implementation
├── WebViewTestSceneSetup.cs       # Editor utility to create test scene
└── README.md                      # This file
```

## 🚀 Quick Start

### 1. Create Test Scene

```csharp
// In Unity Editor:
Immutable → WebView Testing → Create WebView Test Scene
```

### 2. Import WebView Package

Download and import one of the WebView packages:

- **Volt Unity Web Browser (UWB)**: `https://projects.voltstro.dev/UnityWebBrowser/latest/` ⭐ **RECOMMENDED** (MIT License, Multi-Platform)
- **Alacrity**: `https://alacrity.kevinbedi.com/`
- **UWebView2**: `https://uwebview.com/`
- **ZenFulcrum**: `https://zenfulcrum.com/browser`
- **Vuplex 3D WebView**: `https://store.vuplex.com/webview/windows-mac`

### 3. Update Adapter Implementation

Replace the template code in the corresponding adapter (e.g., `AlacrityWebViewAdapter.cs`) with actual WebView API calls.

### 4. Run Tests

1. Open the WebView Test scene
2. 
3. Select the WebView package in the dropdown
4. Click "Test Login Page" to test with `https://auth.immutable.com`
5. Click "Test Messaging" to test JavaScript ↔ Unity communication

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
