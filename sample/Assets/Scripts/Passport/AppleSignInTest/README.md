# Apple Sign In Test Scene

This folder contains a dedicated test script for Apple Sign In functionality.

## Files
- `AppleSignInTestScript.cs` - Test script for Apple Sign In button

## How to Create the Test Scene

Since Unity scenes can't be created via scripts, follow these steps:

### 1. Create New Scene
1. Open Unity
2. File → New Scene
3. Choose "2D" or "UI" template
4. Save as `AppleSignInTestScene` in `Assets/Scenes/Passport/`

### 2. Set Up UI
1. Right-click in Hierarchy → UI → Canvas (if not already present)
2. Add components:
   - **Canvas** (should exist)
   - **EventSystem** (should exist)

### 3. Create UI Elements

#### Status Text (Top)
1. Right-click Canvas → UI → Text - TextMeshPro (or Legacy Text)
2. Name: "StatusText"
3. Position: Top center
4. Text: "Apple Sign In Test"
5. Font Size: 36
6. Alignment: Center
7. RectTransform:
   - Anchor: Top-stretch
   - Pos Y: -50
   - Height: 80

#### Apple Sign In Button
1. Right-click Canvas → UI → Button - TextMeshPro (or Legacy Button)
2. Name: "AppleSignInButton"
3. Position: Center
4. Size: 400 x 80
5. Child Text: "🍎 Sign in with Apple"
6. Font Size: 28
7. Colors:
   - Normal: Black (#000000)
   - Text: White (#FFFFFF)

#### Log Text (Scrollable)
1. Right-click Canvas → UI → Scroll View
2. Name: "LogScrollView"
3. Position: Bottom half of screen
4. Content → Child: Text (rename to "LogText")
5. LogText settings:
   - Alignment: Top-Left
   - Font Size: 14
   - Text: "" (empty)
   - Enable Rich Text: Yes

#### Back Button
1. Right-click Canvas → UI → Button
2. Name: "BackButton"
3. Position: Bottom-left corner
4. Size: 150 x 50
5. Text: "← Back"

### 4. Add Script
1. Create empty GameObject in scene: "AppleSignInTestManager"
2. Add Component → AppleSignInTestScript
3. Assign references in Inspector:
   - Apple Sign In Button: → AppleSignInButton
   - Status Text: → StatusText
   - Log Text: → LogText
   - Back Button: → BackButton

### 5. Test the Scene
1. File → Build Settings → Add Open Scenes (add AppleSignInTestScene)
2. Save scene
3. Build for iOS
4. Run in simulator
5. Test the button!

## What the Test Script Does

### On Start:
- ✅ Logs platform information
- ✅ Checks for Passport instance
- ✅ Sets up button listeners
- ✅ Shows UNITY_IOS flag status

### When Button Clicked:
- ✅ Logs detailed progress
- ✅ Creates DirectLoginOptions with DirectLoginMethod.Apple
- ✅ Calls Passport.Login()
- ✅ Shows success/failure
- ✅ Navigates to AuthenticatedScene on success

### Log Output:
All actions are logged to:
- Console (Unity Debug.Log)
- On-screen log text (scrollable)
- With timestamps

## Expected Behavior

### In iOS Simulator (without native plugin yet):
```
[00:00:01] AppleSignInTestScript started
[00:00:01] ✅ Passport instance found
[00:00:01] ✅ Apple Sign In button listener added
[00:00:01] Platform: IPhonePlayer
[00:00:01] iOS: True
[00:00:01] Editor: False
[00:00:01] ✅ UNITY_IOS flag is defined
[STATUS] Ready to test Apple Sign In

[User clicks button]

[00:00:05] 🍎 Apple Sign In button clicked!
[00:00:05] Creating DirectLoginOptions for Apple...
[00:00:05] DirectLoginMethod: Apple
[00:00:05] Calling Passport.Login()...
[00:00:05] [PassportImpl] Login with DirectLoginMethod: Apple
[00:00:05] [PassportUI] Loading WebView...
...
[00:00:10] ❌ Login failed (expected - no native implementation yet)
[STATUS] Login Failed
```

### After Native Plugin is Implemented:
```
[00:00:01] AppleSignInTestScript started
[00:00:01] ✅ Passport instance found
[00:00:01] ✅ Apple Sign In button listener added
[00:00:01] ✅ UNITY_IOS flag is defined
[STATUS] Ready to test Apple Sign In

[User clicks button]

[00:00:05] 🍎 Apple Sign In button clicked!
[00:00:05] Creating DirectLoginOptions for Apple...
[00:00:05] DirectLoginMethod: Apple
[00:00:05] Calling Passport.Login()...
[00:00:05] [AppleSignInAuth] Starting Apple Sign In...
[00:00:06] [iOS Native] Presenting ASAuthorizationController
[00:00:08] [User authenticates with Face ID]
[00:00:10] [AppleSignInAuth] Success! Identity token received
[00:00:11] ✅ Login successful!
[STATUS] Login Successful!
[00:00:13] [Loading AuthenticatedScene]
```

## Files to Create

You need to create in Unity Editor:
1. Scene: `Assets/Scenes/Passport/AppleSignInTestScene.unity`
2. Canvas with UI elements (follow steps above)
3. AppleSignInTestManager GameObject with script attached

## Quick Setup (Copy-Paste Ready)

If you want to skip manual UI creation, you can:

1. Duplicate `UnauthenticatedScene.unity`
2. Rename to `AppleSignInTestScene.unity`
3. Delete all UI except Canvas and EventSystem
4. Follow "Create UI Elements" steps above
5. Add AppleSignInTestScript

---

**Status**: Script ready, scene needs to be created in Unity Editor

