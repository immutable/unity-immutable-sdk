<div class="display-none">

# Login

</div>

Passport Login enables users to authenticate with the Immutable Passport service. This feature demonstrates how to implement user authentication in your Unity game using two different authentication methods: PKCE (Proof Key for Code Exchange) and Device Code Auth.

<div class="button-component">

[View feature on Github](https://github.com/immutable/unity-immutable-sdk/tree/main/sample/Assets/Scripts/Passport/Login) <span class="button-component-arrow">→</span>

</div>

## Feature Overview
This example demonstrates how to implement user authentication using the Immutable Passport SDK. It covers two authentication methods:
1. **PKCE (Proof Key for Code Exchange)** - A more secure OAuth flow for native and mobile applications
2. **Device Code Auth** - An authentication flow designed for devices with limited input capabilities

## SDK Integration Details

### PKCE Login
PKCE login is recommended for platforms with a web browser, including mobile devices and desktop applications. It provides a secure authentication flow without exposing sensitive data.

```csharp title="PKCE Login" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Login/LoginScript.cs"
// First, initialize Passport with the appropriate redirect URIs for PKCE
SampleAppManager.UsePKCE = true;
InitialisePassport(redirectUri: "immutablerunner://callback", logoutRedirectUri: "immutablerunner://logout");

// Then, to perform the login:
await Passport.LoginPKCE();
```

The PKCE login flow works as follows:
1. The SDK generates a code verifier and code challenge
2. The user is redirected to the Immutable Passport login page in a browser
3. After successful authentication, the browser redirects back to your application with an authorization code
4. The SDK exchanges this code for access and refresh tokens using the code verifier
5. The user is now authenticated and can interact with Immutable services

### Device Code Auth Login
Device Code Auth is useful for devices with limited input capabilities or where opening a browser is not optimal. The user authenticates on a separate device by visiting a URL and entering a code.

```csharp title="Device Code Auth Login" manualLink="https://github.com/immutable/unity-immutable-sdk/blob/main/sample/Assets/Scripts/Passport/Login/LoginScript.cs"
// First, initialize Passport for Device Code Auth
SampleAppManager.UsePKCE = false;
InitialisePassport(logoutRedirectUri: "https://www.immutable.com");

// Then, to perform the login with an optional timeout:
var timeoutMs = GetDeviceCodeTimeoutMs(); // Optional timeout in milliseconds
await Passport.Login(timeoutMs: timeoutMs);
```

The Device Code Auth flow works as follows:
1. The SDK requests a device code from the authorization server
2. A URL and user code are presented to the user
3. The user must visit the URL on another device and enter the code
4. The SDK polls the server until the user completes authentication
5. Upon successful authentication, the user is logged in
6. An optional timeout can be specified to limit how long the SDK will wait for authentication

## Running the Feature Example

### Prerequisites
- Set up your development environment by following the instructions on [Immutable Hub](https://hub.immutable.com)
- Ensure you have the Immutable Passport SDK installed in your Unity project

### Steps to Run the Login Feature
1. Open the sample project in Unity Editor
2. Navigate to the SelectAuthMethod scene
3. Enter Play mode
4. Choose either "Use PKCE" or "Use Device Code Auth" based on your preferred authentication method
5. After initialization completes, you'll be taken to the login screen
6. Click "Login" to start the authentication process
   - For PKCE: A browser window will open where you can enter your credentials
   - For Device Code Auth: A URL and code will be displayed; visit the URL on another device and enter the code

## Summary
The Login feature demonstrates how to implement user authentication in your Unity game using the Immutable Passport SDK. It provides two authentication methods: PKCE for browser-capable devices and Device Code Auth for devices with limited input capabilities. By authenticating users, you enable them to access Immutable's blockchain services securely. 