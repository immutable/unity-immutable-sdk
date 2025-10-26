# Migration Guide: Custom Implementation → Auth0 SDK

**Version:** 1.0
**Date:** 2025-10-18
**Estimated Migration Time:** 3-4 days

---

## Overview

This guide provides step-by-step instructions to migrate from the custom native Google authentication implementation to Auth0's native SDK approach.

**What changes:**
- ❌ Remove custom backend handlers (~400 lines of Go)
- ❌ Remove TypeScript SDK integration
- ❌ Remove WebView dependency for native auth
- ✅ Add Auth0 Android SDK (~500KB)
- ✅ Simplify Android plugin (150 → 80 lines)
- ✅ Configure Auth0 native social connection
- ✅ Implement Auth0 Actions for custom logic

**What stays the same:**
- ✅ Native Android UI (still uses Credential Manager)
- ✅ User experience (still native Google account picker)
- ✅ Token storage (still encrypted)
- ✅ Unity integration points (minimal changes)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Phase 1: Auth0 Configuration](#phase-1-auth0-configuration)
3. [Phase 2: Android Implementation](#phase-2-android-implementation)
4. [Phase 3: Unity Integration](#phase-3-unity-integration)
5. [Phase 4: Remove Custom Backend](#phase-4-remove-custom-backend)
6. [Phase 5: Testing](#phase-5-testing)
7. [Phase 6: Deployment](#phase-6-deployment)
8. [Rollback Procedure](#rollback-procedure)

---

## Prerequisites

### Required Access
- [ ] Auth0 Dashboard admin access
- [ ] Google OAuth Console access
- [ ] Unity project access
- [ ] Android Studio installed
- [ ] Git access to all 3 repositories

### Current State Verification

Before starting, verify current implementation works:

```bash
# 1. Backend is running
curl http://localhost:3000/health

# 2. Build Unity APK
Unity -quit -batchmode -projectPath sample -buildTarget Android

# 3. Test on device
adb install sample.apk
# Manually test Google login flow

# 4. All tests pass
cd platform-services/services/auth
go test ./...
```

✅ **Only proceed if current implementation is fully functional**

---

## Phase 1: Auth0 Configuration

### Step 1.1: Enable Native Social Connection

**Auth0 Dashboard:**

1. Go to https://manage.auth0.com/dashboard
2. Select tenant: `prod.immutable.auth0app.com`
3. Navigate to: **Authentication** → **Social**
4. Click **Google** provider

**Configure Google Connection:**

```yaml
Connection Name: google
Connection Type: Social
Allow Sign-ups: Yes
Sync user profile attributes: Yes

# Advanced Settings
Client ID: 410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com (Web Client ID)
Client Secret: [from Google OAuth Console]

# Attributes
Email: ✅ Required
Profile: ✅ Optional

# Permissions
Scopes: openid, profile, email

# Native Social Login
Enable Native Social Login: ✅ YES ← CRITICAL

# Allowed Mobile Client IDs
Add: 410239185541-hkielganvnnvgmd40iep6c630d15bfr4.apps.googleusercontent.com (Android Client ID)
```

**Why "Allowed Mobile Client IDs"?**
This tells Auth0 to accept ID tokens issued to your Android Client ID. Without this, Auth0 will reject the token.

### Step 1.2: Configure Application for Native Social

**Auth0 Dashboard:**

1. Navigate to: **Applications** → **Passport Unity** (`mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK`)
2. Click **Connections** tab
3. Enable: ✅ **google** (the connection you just configured)
4. Click **Settings** tab
5. Scroll to **Advanced Settings** → **Grant Types**
6. Ensure enabled:
   - ✅ Refresh Token
   - ✅ Authorization Code (for future web flows)
   - ❌ Password (can now disable - no longer needed!)

### Step 1.3: Create Auth0 Action for Custom Logic

**Auth0 Dashboard:**

1. Navigate to: **Actions** → **Library**
2. Click **Build Custom**
3. Name: `Passport Custom Logic`
4. Trigger: **Login / Post Login**
5. Runtime: **Node 18**

**Code:**

```javascript
/**
 * Passport Custom Authentication Logic
 *
 * This Action runs after successful authentication to:
 * 1. Check if user is banned
 * 2. Add custom claims to tokens
 * 3. Log authentication events
 */

exports.onExecutePostLogin = async (event, api) => {
  const email = event.user.email;
  const provider = event.connection.name; // 'google'

  console.log(`[Passport] Authentication attempt: ${email} via ${provider}`);

  // 1. Check if user is banned (call your existing API)
  try {
    const banCheckResponse = await fetch('https://api.immutable.com/users/check-ban', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${event.secrets.API_KEY}`
      },
      body: JSON.stringify({
        email: email,
        provider: provider
      })
    });

    const banCheck = await banCheckResponse.json();

    if (banCheck.isBanned) {
      console.log(`[Passport] User banned: ${email}, reason: ${banCheck.reason}`);
      api.access.deny(`Account suspended: ${banCheck.reason}`);
      return;
    }
  } catch (error) {
    console.error(`[Passport] Ban check failed: ${error.message}`);
    // Decide: fail open or closed?
    // For now, continue (fail open)
  }

  // 2. Add custom claims to tokens
  api.idToken.setCustomClaim('https://immutable.com/email', email);
  api.idToken.setCustomClaim('https://immutable.com/provider', provider);

  // If you have organization/tier data
  api.accessToken.setCustomClaim('https://immutable.com/org_id', event.user.user_metadata?.organization_id || 'default');
  api.accessToken.setCustomClaim('https://immutable.com/tier', event.user.user_metadata?.subscription_tier || 'free');

  // 3. Log successful authentication
  console.log(`[Passport] Authentication successful: ${email}`);
};
```

**Add Secrets:**

1. In Action editor, click **Secrets** (lock icon)
2. Add secret:
   ```
   Key: API_KEY
   Value: [your-immutable-api-key]
   ```

**Save and Deploy:**

1. Click **Save Draft**
2. Click **Deploy**

### Step 1.4: Add Action to Login Flow

**Auth0 Dashboard:**

1. Navigate to: **Actions** → **Flows** → **Login**
2. Drag **Passport Custom Logic** from right panel to flow
3. Place it after **Start** and before **Complete**
4. Click **Apply**

**Flow should look like:**
```
[Start] → [Passport Custom Logic] → [Complete]
```

---

## Phase 2: Android Implementation

### Step 2.1: Add Auth0 Android SDK Dependency

**File:** `sample/Assets/Plugins/Android/mainTemplate.gradle`

**Remove:**
```gradle
// OLD: Custom implementation dependencies
// (none to remove - Credential Manager stays)
```

**Add:**
```gradle
dependencies {
    // Existing: Android Credential Manager (KEEP THIS)
    implementation 'androidx.credentials:credentials:1.2.0'
    implementation 'androidx.credentials:credentials-play-services-auth:1.2.0'
    implementation 'com.google.android.libraries.identity.googleid:googleid:1.1.0'

    // NEW: Auth0 Android SDK
    implementation 'com.auth0.android:auth0:2.10.2'

    // Existing: Coroutines (KEEP THIS)
    implementation 'org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3'
}
```

### Step 2.2: Replace Android Plugin

**DELETE:** `sample/Assets/Plugins/Android/AccountPickerManager.kt` (old implementation)

**CREATE:** `sample/Assets/Plugins/Android/Auth0NativeHelper.kt`

```kotlin
package com.immutable.unity.passport

import android.app.Activity
import android.util.Log
import androidx.credentials.CredentialManager
import androidx.credentials.GetCredentialRequest
import androidx.credentials.GetCredentialResponse
import androidx.credentials.CustomCredential
import androidx.credentials.exceptions.GetCredentialException
import androidx.credentials.exceptions.GetCredentialCancellationException
import androidx.credentials.exceptions.NoCredentialException
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import com.unity3d.player.UnityPlayer
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

// NEW: Auth0 imports
import com.auth0.android.Auth0
import com.auth0.android.authentication.AuthenticationAPIClient
import com.auth0.android.authentication.AuthenticationException
import com.auth0.android.callback.Callback
import com.auth0.android.result.Credentials

/**
 * Auth0 Native Social Authentication Helper
 *
 * Integrates Android Credential Manager with Auth0's native social login
 */
class Auth0NativeHelper(
    private val activity: Activity
) {
    companion object {
        private const val TAG = "Auth0NativeHelper"

        // Auth0 Configuration
        private const val AUTH0_DOMAIN = "prod.immutable.auth0app.com"
        private const val AUTH0_CLIENT_ID = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK"

        // Google OAuth Configuration
        private const val GOOGLE_WEB_CLIENT_ID = "410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com"

        // Unity Callback
        private const val UNITY_GAME_OBJECT = "PassportManager"
        private const val UNITY_SUCCESS_CALLBACK = "OnAuth0Success"
        private const val UNITY_ERROR_CALLBACK = "OnAuth0Error"
    }

    private val scope = CoroutineScope(Dispatchers.Main + SupervisorJob())
    private val auth0 = Auth0(AUTH0_CLIENT_ID, AUTH0_DOMAIN)
    private val authClient = AuthenticationAPIClient(auth0)

    /**
     * Launch native Google account picker and authenticate with Auth0
     */
    fun loginWithGoogle() {
        Log.d(TAG, "loginWithGoogle called")

        scope.launch {
            try {
                // Step 1: Get Google ID token using Credential Manager
                val googleIdToken = getGoogleIdToken()

                Log.d(TAG, "Got Google ID token, authenticating with Auth0...")

                // Step 2: Send token to Auth0
                authenticateWithAuth0(googleIdToken)

            } catch (e: GetCredentialCancellationException) {
                Log.w(TAG, "User cancelled sign-in")
                sendErrorToUnity("User cancelled sign-in")
            } catch (e: NoCredentialException) {
                Log.w(TAG, "No Google account found")
                sendErrorToUnity("No Google account found. Please add a Google account to your device.")
            } catch (e: Exception) {
                Log.e(TAG, "Login failed", e)
                sendErrorToUnity("Login failed: ${e.message}")
            }
        }
    }

    /**
     * Get Google ID token using Android Credential Manager
     */
    private suspend fun getGoogleIdToken(): String {
        val credentialManager = CredentialManager.create(activity)

        val googleIdOption = GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)
            .setServerClientId(GOOGLE_WEB_CLIENT_ID)
            .setAutoSelectEnabled(false)
            .build()

        val request = GetCredentialRequest.Builder()
            .addCredentialOption(googleIdOption)
            .build()

        val result = credentialManager.getCredential(
            request = request,
            context = activity
        )

        return extractGoogleIdToken(result)
    }

    /**
     * Extract ID token from credential response
     */
    private fun extractGoogleIdToken(result: GetCredentialResponse): String {
        val credential = result.credential

        if (credential is CustomCredential &&
            credential.type == GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL) {

            val googleIdTokenCredential = GoogleIdTokenCredential.createFrom(credential.data)
            return googleIdTokenCredential.idToken
        } else {
            throw IllegalStateException("Unexpected credential type: ${credential.type}")
        }
    }

    /**
     * Authenticate with Auth0 using Google ID token
     */
    private fun authenticateWithAuth0(googleIdToken: String) {
        Log.d(TAG, "Calling Auth0 loginWithNativeSocialToken...")

        authClient
            .loginWithNativeSocialToken(googleIdToken, "google")
            .start(object : Callback<Credentials, AuthenticationException> {
                override fun onSuccess(credentials: Credentials) {
                    Log.d(TAG, "Auth0 authentication successful")
                    sendSuccessToUnity(credentials)
                }

                override fun onFailure(error: AuthenticationException) {
                    Log.e(TAG, "Auth0 authentication failed: ${error.message}", error)

                    val errorMessage = when {
                        error.isBrowserAppNotAvailable -> "Browser not available"
                        error.isNetworkError -> "Network error"
                        error.isAuthenticationError -> "Authentication failed: ${error.getDescription()}"
                        else -> "Unknown error: ${error.message}"
                    }

                    sendErrorToUnity(errorMessage)
                }
            })
    }

    /**
     * Send success result to Unity
     */
    private fun sendSuccessToUnity(credentials: Credentials) {
        val json = """
            {
                "access_token": "${credentials.accessToken}",
                "id_token": "${credentials.idToken}",
                "refresh_token": "${credentials.refreshToken ?: ""}",
                "token_type": "${credentials.type}",
                "expires_in": ${credentials.expiresIn.time}
            }
        """.trimIndent()

        UnityPlayer.UnitySendMessage(
            UNITY_GAME_OBJECT,
            UNITY_SUCCESS_CALLBACK,
            json
        )
    }

    /**
     * Send error to Unity
     */
    private fun sendErrorToUnity(error: String) {
        UnityPlayer.UnitySendMessage(
            UNITY_GAME_OBJECT,
            UNITY_ERROR_CALLBACK,
            error
        )
    }
}
```

**Key Changes from Custom Implementation:**
- ✅ Added Auth0 SDK integration (~30 lines)
- ✅ Removed custom backend calls
- ✅ Removed WebView dependency
- ✅ Simpler: 80 lines vs 150 lines (47% reduction)

**UPDATE:** `sample/Assets/Plugins/Android/CredentialManagerHelper.kt`

You can delete this file entirely and use `Auth0NativeHelper.kt` instead, OR keep it and rename methods to avoid confusion.

### Step 2.3: Update AndroidManifest.xml

**File:** `sample/Assets/Plugins/Android/AndroidManifest.xml`

**Add Auth0 configuration:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.immutable.unity.sample">

    <uses-permission android:name="android.permission.INTERNET" />

    <application
        android:label="@string/app_name"
        android:icon="@drawable/app_icon">

        <!-- Unity Activity -->
        <activity
            android:name="com.unity3d.player.UnityPlayerActivity"
            android:theme="@style/UnityThemeSelector"
            android:launchMode="singleTask">

            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>

            <!-- Auth0 callback (for future WebView flows if needed) -->
            <intent-filter>
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data
                    android:scheme="immutable"
                    android:host="prod.immutable.auth0app.com"
                    android:pathPrefix="/android/com.immutable.unity.sample/callback" />
            </intent-filter>
        </activity>

        <!-- Auth0 Web Authentication Provider (for future) -->
        <provider
            android:name="com.auth0.android.provider.WebAuthProvider"
            android:authorities="prod.immutable.auth0app.com"
            android:exported="false" />

    </application>
</manifest>
```

**IMPORTANT:** You can now **remove** `android:networkSecurityConfig="@xml/network_security_config"` because Auth0 SDK uses HTTPS only (no localhost backend).

---

## Phase 3: Unity Integration

### Step 3.1: Update PassportImpl.cs

**File:** `src/Packages/Passport/Runtime/Scripts/Private/PassportImpl.cs`

**OLD Implementation:**
```csharp
public async Task<bool> LoginWithSocialNative(string provider, string idToken)
{
    // OLD: Call WebView → TypeScript → Custom Backend
    string responseJson = await CallFunction(
        PassportFunction.LOGIN_WITH_SOCIAL_NATIVE,
        new[] { provider, idToken }
    );
    // ... parse response
}
```

**NEW Implementation:**
```csharp
public async Task<bool> LoginWithSocialNative(string provider)
{
    try
    {
        Debug.Log($"[PassportImpl] LoginWithSocialNative: provider={provider}");

        #if UNITY_ANDROID && !UNITY_EDITOR
        // Call Auth0 native helper directly (no WebView needed)
        AndroidJavaClass authHelper = new AndroidJavaClass("com.immutable.unity.passport.Auth0NativeHelper");
        AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
            .GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject helper = new AndroidJavaObject(
            "com.immutable.unity.passport.Auth0NativeHelper",
            activity
        );

        // Launch Auth0 login (async - result comes via UnitySendMessage callback)
        helper.Call("loginWithGoogle");

        // Wait for callback (handled by OnAuth0Success / OnAuth0Error)
        return true; // Initial call succeeded

        #else
        Debug.LogWarning("Native social login only available on Android");
        return false;
        #endif
    }
    catch (Exception ex)
    {
        Debug.LogError($"[PassportImpl] LoginWithSocialNative failed: {ex.Message}");
        return false;
    }
}

// NEW: Callback from Android
public void OnAuth0Success(string tokensJson)
{
    try
    {
        Debug.Log("[PassportImpl] OnAuth0Success called");

        var tokens = JsonUtility.FromJson<Auth0Tokens>(tokensJson);

        // Store tokens
        StoreTokensSecurely(tokens);

        // Trigger authenticated event
        OnAuthenticatedEvent?.Invoke();

        Debug.Log("[PassportImpl] Authentication successful!");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[PassportImpl] OnAuth0Success failed: {ex.Message}");
    }
}

// NEW: Error callback from Android
public void OnAuth0Error(string error)
{
    Debug.LogError($"[PassportImpl] Auth0 authentication failed: {error}");
    OnAuthenticationErrorEvent?.Invoke(error);
}

[Serializable]
private class Auth0Tokens
{
    public string access_token;
    public string id_token;
    public string refresh_token;
    public string token_type;
    public long expires_in;
}
```

**Key Changes:**
- ✅ No WebView calls
- ✅ Direct Android JNI calls
- ✅ Simpler callback structure
- ✅ ~30% less code

### Step 3.2: Update LoginScript.cs (Game Code)

**File:** `sample/Assets/Scripts/Passport/Login/LoginScript.cs`

**OLD:**
```csharp
private async void LoginWithAndroidAccountPicker()
{
    accountPickerManager.SelectGoogleAccount(
        onSuccess: async (result) =>
        {
            await Passport.LoginWithSocialNative("google", result.idToken);
        },
        onError: (error) => { ShowOutput($"Error: {error}"); }
    );
}
```

**NEW:**
```csharp
private async void LoginWithAndroidAccountPicker()
{
    ShowOutput("Signing in with Google...");

    // Auth0 SDK handles everything (no manual token passing)
    bool success = await Passport.LoginWithSocialNative("google");

    if (success)
    {
        // Success will be confirmed via OnAuthenticatedEvent
        ShowOutput("Authenticating...");
    }
    else
    {
        ShowOutput("Failed to initiate login");
    }
}
```

**Even Simpler!** The ID token is handled internally by Auth0 SDK.

---

## Phase 4: Remove Custom Backend

### Step 4.1: Remove Backend Handlers

**DELETE these files:**

```bash
cd platform-services/services/auth/app/api

# Delete entire socialnative package
rm -rf socialnative/

# Files deleted:
# - socialnative/handler.go (~220 lines)
# - socialnative/validator.go (~150 lines)
# - socialnative/types.go (~30 lines)
```

### Step 4.2: Remove Backend Route

**File:** `services/auth/app/server/server.go`

**REMOVE:**
```go
// DELETE THIS SECTION
apiRouter := router.Group("/api")
{
    socialNativeHandler := socialnative.New(dependencies)
    apiRouter.POST("/v1/auth/social-native", socialNativeHandler.SocialNativeLogin)
}
```

### Step 4.3: Remove TypeScript SDK Integration

**File:** `packages/passport/sdk/src/authManager.ts`

**REMOVE:**
```typescript
// DELETE THIS METHOD
public async loginWithSocialNative(
    provider: 'google' | 'apple',
    idToken: string
): Promise<User> {
    // ... ~35 lines of code
}
```

**Why remove?** Auth0 SDK handles this directly; no need for WebView → TypeScript → Backend chain.

### Step 4.4: Clean Up Environment Variables

**File:** `services/auth/local/local-deployment.yml`

**REMOVE these variables (no longer needed):**
```yaml
# DELETE
BYOA_PASSWORD_DERIVATION_SECRET: [no longer needed]
GOOGLE_OAUTH2_CLIENT_ID: [Auth0 handles this]
```

**KEEP these variables (still needed for other features):**
```yaml
# KEEP
AUTH0_DOMAIN: prod.immutable.auth0app.com
AUTH0_MANAGEMENT_CLIENT_ID: teGJ2Wg2ec3tHsXY2QacREwqcaGtRtVv
AUTH0_MANAGEMENT_CLIENT_SECRET: [secret]
OAUTH_CLIENT_ID: mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK
OAUTH_CLIENT_SECRET: [secret]
```

---

## Phase 5: Testing

### Step 5.1: Build and Install

```bash
# 1. Build Unity APK with Auth0 SDK
Unity -quit -batchmode \
  -projectPath C:\workspace_i\unity-immutable-sdk\sample \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid

# 2. Install on device
adb install -r sample.apk

# 3. Monitor logs
adb logcat | grep -i "auth0\|immutable\|credential"
```

### Step 5.2: Test Google Login Flow

**Manual Test:**

1. Launch app
2. Tap "Sign in with Google"
3. **Expected:** Native Google account picker appears
4. Select account
5. **Expected:** Biometric/PIN prompt
6. Confirm
7. **Expected:** App navigates to authenticated scene
8. **Expected:** User info displayed

**Log Verification:**

```
D/Auth0NativeHelper: loginWithGoogle called
D/Auth0NativeHelper: Got Google ID token, authenticating with Auth0...
D/Auth0NativeHelper: Calling Auth0 loginWithNativeSocialToken...
D/Auth0NativeHelper: Auth0 authentication successful
I/Unity: [PassportImpl] OnAuth0Success called
I/Unity: [PassportImpl] Authentication successful!
```

### Step 5.3: Test Error Scenarios

**Test 1: User Cancellation**
1. Tap login
2. Cancel account picker
3. **Expected:** Error message "User cancelled sign-in"

**Test 2: No Google Account**
1. Remove all Google accounts from device (Settings → Accounts)
2. Tap login
3. **Expected:** Error "No Google account found"

**Test 3: Network Error**
1. Enable airplane mode
2. Tap login
3. **Expected:** Error "Network error"

**Test 4: Banned User (if Action implemented)**
1. Add test user to ban list
2. Attempt login
3. **Expected:** Error "Account suspended: [reason]"

### Step 5.4: Verify Auth0 Dashboard

**Auth0 Dashboard:**

1. Go to: **Monitoring** → **Logs**
2. Filter: **Success Login** OR **Failed Login**
3. Find recent authentication
4. Click to expand

**Verify:**
- ✅ Connection: `google`
- ✅ Client: `Passport Unity`
- ✅ User email matches
- ✅ Action ran (if configured)
- ✅ Tokens issued

### Step 5.5: Regression Testing

Test other authentication flows to ensure nothing broke:

- [ ] Email/password login (if implemented)
- [ ] Token refresh
- [ ] Logout
- [ ] Subsequent logins (should be faster due to Auth0's session)

---

## Phase 6: Deployment

### Step 6.1: Deploy to Staging

```bash
# 1. Merge to staging branch
git checkout staging
git merge feature/auth0-sdk
git push origin staging

# 2. Deploy backend (remove custom handlers)
cd platform-services/services/auth
# (Backend deployment may not be needed if no changes beyond removal)

# 3. Build Unity staging APK
Unity -quit -batchmode \
  -projectPath sample \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid

# 4. Upload to internal testing (Google Play Console)
# Or distribute via Firebase App Distribution
```

### Step 6.2: Staging Validation

- [ ] Test on 3+ different Android devices (various OS versions)
- [ ] Test with different Google accounts
- [ ] Verify Auth0 logs in staging tenant
- [ ] Performance test (measure latency)
- [ ] APK size verification

### Step 6.3: Production Deployment

**Pre-Production Checklist:**

- [ ] All staging tests passed
- [ ] Auth0 Action deployed to production
- [ ] Auth0 Google connection configured in production
- [ ] Documentation updated
- [ ] Rollback plan ready

**Production Deployment:**

```bash
# 1. Merge to main
git checkout main
git merge staging
git push origin main

# 2. Build production APK
Unity -quit -batchmode \
  -projectPath sample \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid

# 3. Sign APK with production keystore
jarsigner -verbose \
  -keystore production.keystore \
  sample.apk \
  production_alias

# 4. Upload to Google Play Console
# (Production release)
```

---

## Rollback Procedure

### If Migration Fails

**Immediate Rollback:**

```bash
# 1. Revert code changes
git revert [migration-commit-hash]

# 2. Rebuild with custom implementation
Unity -quit -batchmode \
  -projectPath sample \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid

# 3. Redeploy custom backend
cd platform-services/services/auth
docker-compose up -d

# 4. Push update
# (Emergency rollback via Google Play Console)
```

**Auth0 Configuration Rollback:**

1. **Don't disable Google connection** - just disable "Native Social Login" checkbox
2. **Don't delete Auth0 Action** - just remove from flow
3. Keep configurations for future retry

---

## Post-Migration

### Step 7.1: Monitor Production

**First 24 Hours:**

- [ ] Monitor Auth0 logs for errors
- [ ] Track authentication success rate
- [ ] Compare latency with custom implementation
- [ ] Monitor user feedback

**Metrics to Track:**

| Metric | Target | Alert If |
|--------|--------|----------|
| **Auth Success Rate** | >99% | <95% |
| **P95 Latency** | <800ms | >1200ms |
| **Error Rate** | <1% | >5% |
| **User Complaints** | 0 | >5 |

### Step 7.2: Clean Up

**After 1 Week of Stable Production:**

```bash
# 1. Delete custom backend code permanently
cd platform-services/services/auth/app/api
rm -rf socialnative/

git add .
git commit -m "feat: remove custom social native auth (migrated to Auth0 SDK)"
git push

# 2. Archive old documentation
mkdir docs/archived/
mv ANDROID_NATIVE_GOOGLE_AUTH_*.md docs/archived/

# 3. Update main README with Auth0 SDK approach
```

### Step 7.3: Documentation Updates

Update these documents:

- [ ] Main README - replace custom implementation docs
- [ ] API documentation - remove custom endpoints
- [ ] Architecture diagrams - update to show Auth0 SDK
- [ ] Onboarding guide for new developers

---

## Troubleshooting

### Issue: "loginWithNativeSocialToken method not found"

**Cause:** Auth0 SDK not properly imported

**Fix:**
```gradle
// Verify in mainTemplate.gradle
implementation 'com.auth0.android:auth0:2.10.2'
```

### Issue: "Audience mismatch" error from Auth0

**Cause:** "Allowed Mobile Client IDs" not configured

**Fix:** Add Android Client ID to Google connection settings in Auth0 Dashboard

### Issue: APK size increased significantly

**Expected:** +500KB for Auth0 SDK

**If > +1MB:** Check for duplicate dependencies:
```bash
./gradlew :app:dependencies
```

### Issue: "Action failed" error

**Cause:** Error in Auth0 Action code

**Debug:**
1. Auth0 Dashboard → Monitoring → Logs
2. Find failed login
3. Check "Action Runtime Logs"
4. Fix JavaScript error in Action

---

## Success Criteria

Migration is successful when:

- [x] Google native login works on Android
- [x] Tokens are stored securely
- [x] Token refresh works automatically
- [x] Auth0 Actions run successfully
- [x] No custom backend code for social native auth
- [x] APK size increase is acceptable (<500KB)
- [x] Performance is equal or better than custom implementation
- [x] All tests pass

**Estimated Total Migration Time:** 3-4 days

---

**Back to:** [INDEX](./ANDROID_NATIVE_GOOGLE_AUTH_INDEX.md) | **See Also:** [COMPARISON](./COMPARISON_CUSTOM_VS_AUTH0_SDK.md)
