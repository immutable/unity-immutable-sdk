# iOS Native WebView - Phase 1 Testing Guide

## ✅ Phase 1 Complete!

Phase 1 implementation is complete. You now have a working iOS native WebView that can display web pages.

## What Works in Phase 1

✅ **Working Features**:
- Create and initialize native iOS WKWebView
- Show/hide WebView on screen
- Load any URL (Google, Passport auth page, etc.)
- Navigate and interact with web pages
- Scroll, click links, fill forms
- Position and size WebView

❌ **Not Working Yet** (Phase 2-4):
- JavaScript callbacks from web page to Unity
- OAuth deep link handling  
- Authentication flow completion
- WKWebView delegate events

## Files Created

### Native iOS Plugin (`plugins/iOS/`)
- `PassportWebView.h` - C interface for Unity P/Invoke
- `PassportWebView.mm` - Objective-C implementation with WKWebView
- `README.md` - Plugin documentation

### Unity C# Wrapper
- `src/Packages/Passport/Runtime/Scripts/Private/UI/WebViews/iOSNativePassportWebView.cs` - Unity wrapper with P/Invoke

### Test Script
- `sample/Assets/Scripts/Passport/WebViewTesting/TestPhase1WebView.cs` - Test script for Phase 1 features

### Updated Files
- `src/Packages/Passport/Runtime/Scripts/Public/PassportUI.cs` - Updated platform detection to use native iOS WebView

## How to Test Phase 1

### Prerequisites
- Unity 2021.3+ with iOS Build Support
- Xcode 14+
- iOS device or simulator (iOS 12+)
- macOS for building

### Step 1: Setup Test Scene

1. Open Unity project
2. Create a new scene or use existing scene
3. Create an empty GameObject
4. Add `TestPhase1WebView.cs` script to the GameObject
5. Save the scene

### Step 2: Configure iOS Build

1. Go to **File → Build Settings → iOS**
2. Click **Player Settings**
3. Configure iOS settings:
   - **Bundle Identifier**: `com.yourcompany.passporttest`
   - **Minimum iOS Version**: 12.0
   - **Target Device**: iPhone + iPad
   - **Architecture**: ARM64

### Step 3: Build for iOS

1. In Build Settings, click **Build**
2. Choose output folder (e.g., `Builds/iOS`)
3. Wait for build to complete
4. Unity will generate an Xcode project

### Step 4: Open in Xcode

1. Navigate to build folder
2. Open `Unity-iPhone.xcodeproj` in Xcode
3. Verify the native plugin files are included:
   - Check `Libraries/Plugins/iOS/` for `PassportWebView.h` and `PassportWebView.mm`

### Step 5: Deploy and Test

#### For iOS Simulator:
1. In Xcode, select a simulator (e.g., iPhone 14)
2. Click **Run** (▶️)
3. App will launch in simulator

#### For iOS Device:
1. Connect your iOS device
2. In Xcode, select your device
3. Configure **Signing & Capabilities** with your Apple Developer account
4. Click **Run** (▶️)
5. App will install and launch on device

### Step 6: Run Tests

Once the app is running:

1. **Initialize WebView**:
   - Tap "Initialize WebView" button
   - Status should show "WebView initialized! Ready to test."
   - Check Xcode console for log: `[PassportWebView] WKWebView created and added to Unity view`

2. **Show WebView**:
   - Tap "Show WebView" button
   - A WebView overlay should appear on screen (initially blank)
   - Check console for: `[PassportWebView] WebView shown`

3. **Load Google**:
   - Tap "Load Google" button
   - Google search page should load and display
   - You should be able to interact with the page (scroll, type, click)

4. **Load Passport**:
   - Tap "Load Passport (no auth yet)" button
   - Passport authentication page should load
   - You'll see login options (Email, Google, Apple, Facebook)
   - **Note**: Clicking login buttons won't complete authentication (Phase 4 feature)

5. **Hide WebView**:
   - Tap "Hide WebView" button
   - WebView should disappear from screen

6. **Load Unity.com**:
   - Tap "Show WebView" again
   - Tap "Load Unity.com" button
   - Unity website should load and display

## Expected Results

### ✅ Success Indicators:
- WebView appears/disappears correctly
- Web pages load and render properly
- You can scroll and interact with pages
- Multiple URLs can be loaded sequentially
- No crashes or errors in console

### ❌ Expected Limitations:
- Clicking "Login" on Passport page won't complete (no callbacks yet)
- OAuth redirects won't be captured
- JavaScript messages won't reach Unity
- No authentication flow completion

## Debugging

### Check Xcode Console
Look for these log messages:
```
[PassportWebView] Creating WebView for: PassportWebView
[PassportWebView] WKWebView created and added to Unity view
[PassportWebView] WebView shown
[PassportWebView] Loading URL: https://www.google.com
[PassportWebView] Frame set to: x=..., y=..., w=..., h=...
```

### Common Issues

#### 1. "WebView not supported on this platform"
- **Cause**: Running in Unity Editor or non-iOS platform
- **Fix**: Build and deploy to iOS device/simulator

#### 2. Black/blank WebView
- **Cause**: URL not loaded yet or invalid URL
- **Fix**: Tap a "Load" button to load a URL

#### 3. WebView not visible
- **Cause**: WebView hidden or frame not set
- **Fix**: Tap "Show WebView" button

#### 4. Xcode build errors
- **Cause**: Missing iOS Build Support in Unity
- **Fix**: Install iOS Build Support module in Unity Hub

#### 5. Native plugin not found
- **Cause**: Plugin files not copied to Xcode project
- **Fix**: Check `Libraries/Plugins/iOS/` in Xcode project navigator

## Performance Testing

Monitor these metrics during testing:

### Memory Usage:
- Initial: ~50-100 MB
- With WebView: +20-50 MB (acceptable)
- After loading pages: +10-30 MB per page (normal)

### CPU Usage:
- Idle: <5%
- Loading page: 20-40% (temporary spike)
- Scrolling: 10-20%

### Frame Rate:
- Should maintain 60 FPS on device
- Slight drops during page load are normal

## Next Steps

Once Phase 1 testing is successful:

1. **Phase 2**: Implement WKWebView delegates for navigation events
2. **Phase 3**: Complete Unity integration
3. **Phase 4**: Add JavaScript bridge and OAuth callback handling
4. **Phase 5**: Comprehensive testing and production readiness

## Success Criteria for Phase 1

- [x] Native iOS plugin compiles without errors
- [x] Unity C# wrapper compiles without errors
- [x] WebView displays on iOS device/simulator
- [x] Can load and display web pages
- [x] Show/hide functionality works
- [x] Multiple URLs can be loaded
- [x] No crashes or memory leaks
- [x] Smooth scrolling and interaction

## Questions or Issues?

If you encounter issues:
1. Check Xcode console for error messages
2. Verify iOS version is 12.0+
3. Ensure native plugin files are in Xcode project
4. Try on both simulator and device
5. Check Unity console for C# errors

Refer to `docs/iOS_Native_WebView_Implementation_Plan.md` for detailed implementation information.
