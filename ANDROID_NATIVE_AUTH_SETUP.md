# Android Native Authentication Setup

This guide explains how to configure native Google authentication (no browser) for the Immutable Unity SDK.

## Overview

The Android Account Picker feature provides two authentication methods:

1. **Credential Manager (Android 9+)** - Native One Tap sign-in with ID token (NO BROWSER)
2. **AccountManager (Android 5+)** - Fallback account picker (requires browser OAuth)

## Setup Steps

### 1. Get OAuth 2.0 Client ID from Google Cloud Console

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project (or create a new one)
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth 2.0 Client ID**
5. Select **Web application** as the application type
6. Add authorized redirect URIs (your Passport redirect URIs)
7. Click **Create**
8. **Copy the Client ID** (format: `xxxxx.apps.googleusercontent.com`)

### 2. Configure the Web Client ID

Open the file:
```
Plugins/Android/ImmutableAndroid/ImmutableAndroid/src/main/java/com/immutable/unity/CredentialManagerHelper.java
```

Replace this line:
```java
private static final String WEB_CLIENT_ID = "YOUR_WEB_CLIENT_ID_HERE.apps.googleusercontent.com";
```

With your actual Web Client ID:
```java
private static final String WEB_CLIENT_ID = "123456789-abcdefg.apps.googleusercontent.com";
```

### 3. Rebuild the AAR

After changing the Client ID, rebuild the Android library:

```bash
cd Plugins/Android/ImmutableAndroid
./gradlew clean assembleRelease
```

The AAR will be automatically copied to:
```
src/Packages/Passport/Runtime/Assets/Plugins/Android/ImmutableAndroid.aar
```

### 4. Configure AndroidManifest.xml

Ensure your app's `AndroidManifest.xml` uses the custom activity:

```xml
<activity android:name="com.immutable.unity.ImmutableUnityActivity"
          android:theme="@style/UnityThemeSelector"
          android:exported="true">
    <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
    </intent-filter>
</activity>
```

## How It Works

### Native Flow (Credential Manager - Android 9+)

```
User clicks "Sign In"
   ↓
Native One Tap modal appears (no browser!)
   ↓
User selects Google account
   ↓
Google ID token retrieved (JWT)
   ↓
ID token sent to Unity
   ↓
[TODO] Send ID token to Passport backend
```

**Current Status:** The native flow successfully retrieves the ID token, but currently falls back to browser OAuth because Passport SDK doesn't yet support ID token authentication.

**ID Token is available** in `AndroidAccountResult.idToken` - ready for backend integration.

### Fallback Flow (AccountManager - Android 5-8)

```
User clicks "Sign In"
   ↓
Account picker dialog appears
   ↓
User selects Google account
   ↓
Email extracted
   ↓
Browser OAuth flow (Chrome Custom Tab)
   ↓
OAuth token exchange
   ↓
Authenticated
```

## Testing

### Test Credential Manager (Android 9+)

1. Run on Android 9+ device with Google Play Services
2. Ensure device has Google accounts added
3. Click the Android Account Picker button
4. Native One Tap modal should appear
5. Check Unity console for:
   ```
   [GoogleAccountManager] Initialized with Credential Manager (native One Tap)
   [LoginScript] Native authentication successful! Got ID token
   ```

### Test AccountManager Fallback (Android 5-8)

1. Run on Android 5-8 device
2. Click the Android Account Picker button
3. System account picker should appear
4. Check Unity console for:
   ```
   [GoogleAccountManager] Credential Manager not available, falling back to AccountManager
   [LoginScript] Fallback authentication (AccountManager, requires browser OAuth)
   ```

## Troubleshooting

### "Web Client ID not configured" Error

**Solution:** Configure `WEB_CLIENT_ID` in `CredentialManagerHelper.java` (see Step 2)

### "No Google accounts found on device"

**Solution:** Add a Google account to the Android device (Settings > Accounts)

### Credential Manager not initializing

**Possible causes:**
- Device doesn't have Google Play Services
- Android version < 9
- Missing dependencies in build.gradle

**Check Unity console for:**
```
[GoogleAccountManager] Credential Manager not available, falling back to AccountManager
```

### "Sign-in failed" errors

**Check:**
1. Web Client ID is correct (from Google Cloud Console)
2. OAuth 2.0 Client ID is for **Web application** type (not Android)
3. Redirect URIs are configured in Google Cloud Console
4. Google Play Services is up to date on device

## Future: Full Native Authentication

To enable **completely native** authentication (no browser at all):

### Option 1: Passport SDK Integration

Passport SDK needs to add ID token support:

```csharp
// In LoginScript.cs
if (!string.IsNullOrEmpty(result.idToken))
{
    // Send ID token directly to Passport
    bool success = await Passport.LoginWithIdToken(result.idToken);
    if (success)
    {
        SceneManager.LoadScene("AuthenticatedScene");
        return;
    }
}
```

### Option 2: Direct Backend Integration

Send ID token directly to Passport backend:

```csharp
// In LoginScript.cs
if (!string.IsNullOrEmpty(result.idToken))
{
    // Send to Passport backend
    UnityWebRequest request = UnityWebRequest.Post(
        "https://auth.immutable.com/v1/auth/google-id-token",
        new Dictionary<string, string> { { "idToken", result.idToken } }
    );

    await request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        // Parse response and store session
        SceneManager.LoadScene("AuthenticatedScene");
        return;
    }
}
```

## Dependencies

The native authentication requires these dependencies (automatically included):

```gradle
implementation 'androidx.credentials:credentials:1.3.0'
implementation 'androidx.credentials:credentials-play-services-auth:1.3.0'
implementation 'com.google.android.libraries.identity.googleid:googleid:1.1.1'
```

**Total size:** ~3-6 MB (includes Google Play Services integration)

## Supported Android Versions

| Android Version | Auth Method | Browser Required? | ID Token? |
|-----------------|-------------|-------------------|-----------|
| 9+ (API 28+) | Credential Manager | ❌ No | ✅ Yes |
| 5-8 (API 21-27) | AccountManager | ✅ Yes | ❌ No |
| < 5 (API < 21) | Not supported | - | - |

## References

- [Google Credential Manager Documentation](https://developer.android.com/training/sign-in/credential-manager)
- [Google ID Token Reference](https://developers.google.com/identity/gsi/web/reference/js-reference)
- [OAuth 2.0 for Android](https://developers.google.com/identity/protocols/oauth2/native-app)
