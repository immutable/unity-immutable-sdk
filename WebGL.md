# Implementing Immutable Unity SDK for WebGL

This guide provides focused instructions for implementing the Unity-Immutable-SDK in WebGL projects.

For general information on the SDK, please refer to
the [Immutable Unity SDK documentation](https://docs.immutable.com/sdks/zkEVM/unity).

Live example can be found at https://immutable.github.io/unity-immutable-sdk

## WebGL Template Setup

WebGL template is a configuration setting that lets you control the appearance of the HTML page, so that you can: test, demonstrate, and preview your WebGL application in a browser.

1. Create a custom WebGL template:
   - Navigate to **Assets > WebGLTemplates** in your Unity project.
   - Copy one of the built-in templates (Default or Minimal) from **[Unity Installation] > PlaybackEngines > WebGLSupport > BuildTools > WebGLTemplates**.
   - Rename the copied template to something meaningful for your project.

2. Copy the following files from the Passport package into your **Assets > WebGLTemplates** folder:
   - `Packages/Immutable Passport/WebGLTemplates~/unity-webview.js`
   - `Packages/Immutable Passport/WebGLTemplates~/callback.html`
   - `Packages/Immutable Passport/WebGLTemplates~/logout.html`
   - `Packages/Immutable Passport/Runtime/Resources/index.html` > `Passport/index.html`

3. Add the following script tag to the `index.html` in WebGL Templates:
   ```html
   <script src="unity-webview.js"></script>
   ```

## PKCE Login and Logout Implementation
> [!IMPORTANT]  
> WebGL supports only PKCE flow.

Follow these steps for implementation:
> [!NOTE]
> You can rename `callback.html` and `logout.html` to suit your project needs.
> For local testing with WebGL builds, note the random port number assigned (e.g., http://localhost:60750). You may need to update the Hub Passport config each time you start a new local WebGL build, as the port number may change.
1. Define a deep link scheme for your game:
   - Redirect URL: https://game.domain.com/callback.html
   - Logout URL: https://game.domain.com/logout.html

2. Configure Immutable Hub:
   - Login to [Immutable Hub](https://hub.immutable.com)
   - Add these deep links to your client's Redirect URLs and Logout URLs

3. Update Passport initialisation:
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