# iOS Native WebView - Phase 1 Verification Checklist

Use this checklist to verify Phase 1 implementation is complete and working correctly.

## Pre-Build Verification

### Code Files
- [x] `plugins/iOS/PassportWebView.h` exists
- [x] `plugins/iOS/PassportWebView.mm` exists
- [x] `plugins/iOS/README.md` exists
- [x] `src/Packages/Passport/Runtime/Scripts/Private/UI/WebViews/iOSNativePassportWebView.cs` exists
- [x] `sample/Assets/Scripts/Passport/WebViewTesting/TestPhase1WebView.cs` exists
- [x] `PassportUI.cs` updated with iOS native WebView detection

### Compilation
- [ ] No C# compilation errors in Unity
- [ ] No warnings related to iOS WebView code
- [ ] Conditional compilation directives correct (`#if UNITY_IOS && !UNITY_EDITOR`)

## Build Verification

### Unity Build Settings
- [ ] iOS platform selected
- [ ] Bundle Identifier configured
- [ ] Minimum iOS Version set to 12.0
- [ ] Architecture set to ARM64
- [ ] Test scene includes TestPhase1WebView script

### Xcode Project
- [ ] Xcode project opens without errors
- [ ] Native plugin files visible in `Libraries/Plugins/iOS/`
- [ ] `PassportWebView.h` present
- [ ] `PassportWebView.mm` present
- [ ] WebKit framework linked (automatic)
- [ ] No Xcode compilation errors

## Deployment Verification

### iOS Simulator
- [ ] App launches successfully
- [ ] No crash on startup
- [ ] Test UI displays
- [ ] Console shows initialization logs

### iOS Device
- [ ] App installs successfully
- [ ] Signing configured correctly
- [ ] App launches without crash
- [ ] Test UI displays
- [ ] Console shows initialization logs

## Functional Testing

### WebView Initialization
- [ ] "Initialize WebView" button works
- [ ] Status shows "WebView initialized!"
- [ ] Console log: `[PassportWebView] WKWebView created and added to Unity view`
- [ ] No errors in Xcode console
- [ ] No Unity errors

### Show/Hide Functionality
- [ ] "Show WebView" makes WebView visible
- [ ] WebView appears as overlay on screen
- [ ] Console log: `[PassportWebView] WebView shown`
- [ ] "Hide WebView" makes WebView invisible
- [ ] Console log: `[PassportWebView] WebView hidden`
- [ ] Can toggle show/hide multiple times

### URL Loading - Google
- [ ] "Load Google" button works
- [ ] Google search page loads
- [ ] Page renders correctly
- [ ] Can scroll the page
- [ ] Can click links
- [ ] Can type in search box
- [ ] Console log: `[PassportWebView] Loading URL: https://www.google.com`

### URL Loading - Passport
- [ ] "Load Passport" button works
- [ ] Passport auth page loads
- [ ] Login options display (Email, Google, Apple, Facebook)
- [ ] Page renders correctly
- [ ] Can scroll the page
- [ ] Console log: `[PassportWebView] Loading URL: https://auth.immutable.com...`
- [ ] **Expected**: Clicking login buttons won't complete (Phase 4 feature)

### URL Loading - Unity.com
- [ ] "Load Unity.com" button works
- [ ] Unity website loads
- [ ] Page renders correctly
- [ ] Can navigate the site
- [ ] Images load correctly

### Multiple URL Loads
- [ ] Can load Google, then Passport, then Unity sequentially
- [ ] Each page loads correctly
- [ ] No memory issues
- [ ] WebView remains functional

## Performance Testing

### Memory
- [ ] Initial memory usage reasonable (<100 MB)
- [ ] WebView adds ~20-50 MB (acceptable)
- [ ] No memory leaks after show/hide cycles
- [ ] Memory stable after loading multiple pages

### CPU
- [ ] Idle CPU usage low (<5%)
- [ ] Page load CPU spike acceptable (20-40%)
- [ ] Returns to idle after load complete

### Frame Rate
- [ ] Maintains 60 FPS when WebView hidden
- [ ] Maintains 55-60 FPS when WebView visible
- [ ] Smooth scrolling in WebView
- [ ] No stuttering or lag

## Error Handling

### Edge Cases
- [ ] Can initialize WebView multiple times (should warn and skip)
- [ ] Can show already-visible WebView (should be idempotent)
- [ ] Can hide already-hidden WebView (should be idempotent)
- [ ] Can load URL while another is loading (should work)
- [ ] Proper cleanup on app quit

### Error Scenarios
- [ ] Invalid URL shows error in console
- [ ] Empty URL shows error in console
- [ ] Network error handled gracefully
- [ ] WebView operations before init show errors

## Console Logging

### Expected Logs (Xcode Console)
- [ ] `[PassportWebView] Creating WebView for: PassportWebView`
- [ ] `[PassportWebView] WKWebView created and added to Unity view`
- [ ] `[PassportWebView] WebView shown`
- [ ] `[PassportWebView] Frame set to: x=..., y=..., w=..., h=...`
- [ ] `[PassportWebView] Loading URL: ...`
- [ ] `[PassportWebView] WebView hidden`
- [ ] `[PassportWebView] WebView cleaned up` (on quit)

### Expected Logs (Unity Console)
- [ ] `[TestPhase1] Phase 1 WebView Test Script loaded`
- [ ] `[TestPhase1] Initializing WebView...`
- [ ] `[iOSNativePassportWebView] Initializing iOS native WebView...`
- [ ] `[iOSNativePassportWebView] iOS native WebView initialized successfully`
- [ ] `[iOSNativePassportWebView] Loading URL: ...`

### No Unexpected Errors
- [ ] No "WebView not supported" errors
- [ ] No P/Invoke errors
- [ ] No null reference exceptions
- [ ] No native plugin errors
- [ ] No memory allocation errors

## Documentation

### Files Present
- [x] `docs/iOS_Native_WebView_Implementation_Plan.md`
- [x] `docs/iOS_Native_WebView_Phase1_Testing.md`
- [x] `docs/iOS_Native_WebView_Phase1_Summary.md`
- [x] `docs/iOS_Native_WebView_Phase1_Checklist.md` (this file)
- [x] `plugins/iOS/README.md`

### Documentation Accuracy
- [ ] Testing guide is accurate
- [ ] Implementation plan reflects actual code
- [ ] Summary matches implementation
- [ ] README explains plugin structure

## Known Limitations (Expected)

### Phase 1 Limitations - These are EXPECTED:
- [ ] ❌ Passport login doesn't complete (Phase 4)
- [ ] ❌ No JavaScript callbacks (Phase 4)
- [ ] ❌ No OAuth redirect handling (Phase 4)
- [ ] ❌ No page load events (Phase 2)
- [ ] ❌ No navigation events (Phase 2)

### Platform Limitations - These are EXPECTED:
- [ ] ⚠️ Only works on iOS device builds (not Editor)
- [ ] ⚠️ Editor falls back to Vuplex (if available)
- [ ] ⚠️ Requires iOS 12.0+

## Sign-Off

### Developer Verification
- [ ] All code files created
- [ ] All tests passing
- [ ] No compilation errors
- [ ] Documentation complete
- [ ] Ready for Phase 2

### QA Verification
- [ ] Tested on iOS Simulator
- [ ] Tested on iOS Device (iPhone)
- [ ] Tested on iOS Device (iPad)
- [ ] All functional tests pass
- [ ] Performance acceptable
- [ ] No critical bugs

### Stakeholder Approval
- [ ] Demo completed
- [ ] Functionality verified
- [ ] Limitations understood
- [ ] Approved to proceed to Phase 2

## Notes

### Issues Found:
_(Document any issues discovered during testing)_

### Workarounds Applied:
_(Document any workarounds needed)_

### Follow-Up Items:
_(Document any items to address in future phases)_

---

## Quick Test Script

Run these commands in order to quickly verify Phase 1:

1. **Initialize**: Tap "Initialize WebView" → Should show "WebView initialized!"
2. **Show**: Tap "Show WebView" → WebView overlay appears
3. **Load**: Tap "Load Google" → Google page displays
4. **Interact**: Scroll and click in WebView → Should work smoothly
5. **Switch**: Tap "Load Passport" → Passport page displays
6. **Hide**: Tap "Hide WebView" → WebView disappears
7. **Show Again**: Tap "Show WebView" → WebView reappears with Passport page
8. **Quit**: Close app → Should cleanup without errors

If all 8 steps work, Phase 1 is successful! ✅

---

**Date Tested**: _______________
**Tested By**: _______________
**Device/Simulator**: _______________
**iOS Version**: _______________
**Result**: ☐ Pass  ☐ Fail  ☐ Pass with Issues
