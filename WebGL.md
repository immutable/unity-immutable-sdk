# Implementing Unity-Immutable-SDK for WebGL

This guide provides focused instructions for implementing the Unity-Immutable-SDK in WebGL projects.

For general information on the SDK, please refer to
the [Unity-Immutable-SDK documentation](https://docs.immutable.com/sdks/zkEVM/unity).

Live example can be found at https://immutable.github.io/unity-immutable-sdk/sample/webgl

## WebGL Template Setup

1. Navigate to `sample/Assets/WebGLTemplates/unity-webview/`.

2. Copy the following files to your WebGLTemplates folder:
   - `unity-webview.js`
   - `callback.html`
   - `logout.html`
   - `Passport/index.html`

3. Add the following script tag to your `index.html`:
   ```html
   <script src="unity-webview.js"></script>
   ```

## PKCE Login and Logout Implementation
WebGL supports only PKCE flow. Follow these steps for implementation:

1. Define a deep link scheme for your game:
   - Login: https://game.domain.com/callback.html
   - Logout: https://game.domain.com/logout.html

2. Configure Immutable Hub:
   - Login to [Immutable Hub](https:hub.immutable.com)
   - Add these deep links to your client's Redirect URLs and Logout URLs

3. Update Passport initialization:
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Immutable.Passport;

public class InitPassport : MonoBehaviour
{
    private Passport passport;

    async void Start()
    {
        string clientId = "YOUR_IMMUTABLE_CLIENT_ID";
        string environment = Immutable.Passport.Model.Environment.SANDBOX;
        string redirectUri = "https://game.domain.com/callback.html";
        string logoutRedirectUri = "https://game.domain.com/logout.html";
        passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
    }
}
```

## Building for WebGL
1. Go to File > Build Settings
2. Select WebGL as the target platform
3. Click "Build And Run"
4. Choose a directory for the build output
Your WebGL application will open in your default web browser once the build is complete.

For a complete working example, refer to the sample application in the SDK repository.