# Implementing Immutable Unity SDK for WebGL

This guide provides focused instructions for implementing the Unity-Immutable-SDK in WebGL projects.

For general information on the SDK, please refer to
the [Immutable Unity SDK documentation](https://docs.immutable.com/sdks/zkEVM/unity).

Live example can be found at https://immutable.github.io/unity-immutable-sdk/sample/webgl

## WebGL Template Setup

WebGL template is a configuration setting that lets you control the appearance of the HTML page, so that you can: test, demonstrate, and preview your WebGL application in a browser.

A WebGL Template is always needed for creating a WebGL application and it will always be stored within **Assets > WebGL Templates** to be used. You can refer to [Unity: WebGL Templates](https://docs.unity3d.com/Manual/webgl-templates.html) for more information.

A Custom WebGL Template is required to implement the Immutable Unity SDK in WebGL projects. The easiest way to create a new custom WebGL template is to make a copy of the built-in Default or Minimal templates, which are stored in corresponding subfolders under <Unity Installation> > PlaybackEngines > WebGLSupport > BuildTools > WebGLTemplates.

Every Unity Project includes these templates by default. Copy a template and place it in your own **Assets > WebGLTemplates** folder, and rename it to something meaningful so you can identify your template later.

Once you have created your own template, copy the following files from Passport package into the **Assets > WebGLTemplates** folder:
   - `Packages/Immutable Passport/Runtime/Assets/WebGLTemplates/unity-webview.js`
   - `Packages/Immutable Passport/Runtime/Assets/WebGLTemplates/callback.html`
   - `Packages/Immutable Passport/Runtime/Assets/WebGLTemplates/logout.html`
   - `Packages/Immutable Passport/Runtime/Assets/WebGLTemplates/index.html` > `Passport/index.html`

3. Add the following script tag to the `index.html` in WebGL Templates:
   ```html
   <script src="unity-webview.js"></script>
   ```

## PKCE Login and Logout Implementation
> [!IMPORTANT]  
> WebGL supports only PKCE flow. Follow these steps for implementation:

> [!NOTE]
> You can rename `callback.html` and `logout.html` to suit your project needs.
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