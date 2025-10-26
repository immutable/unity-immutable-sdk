# Auth0 SDK Setup Guide - Implementation Checklist

**Version:** 1.0
**Date:** 2025-10-18
**Purpose:** Complete Auth0 SDK migration from custom implementation

---

## ✅ Pre-Implementation Checklist

Before starting code changes, complete Auth0 configuration:

---

## Step 1: Configure Auth0 Google Connection

### 1.1 Access Auth0 Dashboard

1. Go to: https://manage.auth0.com/dashboard
2. Select tenant: `prod.immutable.auth0app.com`
3. Navigate to: **Authentication** → **Social**

### 1.2 Configure Google Connection

Click on **Google** provider:

```yaml
Connection Name: google
Status: ✅ Enabled

# Basic Settings
Client ID: 410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com
Client Secret: [from Google OAuth Console]

# Attributes
Email: ✅ Required
Profile: ✅ Requested
Name: ✅ Requested

# Scopes
openid: ✅
profile: ✅
email: ✅

# Sync user profile attributes at each login
Sync user profile: ✅ Enabled
```

### 1.3 Enable Native Social Login ⚠️ CRITICAL

Scroll to **Advanced Settings** → **Native Social Login**:

```yaml
Enable Native Social Login: ✅ YES

Allowed Mobile Client IDs:
  - 410239185541-hkielganvnnvgmd40iep6c630d15bfr4.apps.googleusercontent.com
    (This is your Android Client ID)
```

**⚠️ Important:** Without adding the Android Client ID here, Auth0 will reject tokens from your Android app!

### 1.4 Save Settings

Click **Save Changes** at the bottom

---

## Step 2: Enable Connection for Application

### 2.1 Navigate to Application

1. Go to: **Applications** → **Applications**
2. Click: **Passport Unity** (`mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK`)
3. Click: **Connections** tab

### 2.2 Enable Google

Find **google** in the list and toggle it **ON** ✅

### 2.3 Verify Grant Types

Click **Settings** tab → Scroll to **Advanced Settings** → **Grant Types**:

```yaml
✅ Refresh Token (keep enabled)
✅ Authorization Code (keep enabled - for future web flows)
❌ Password (DISABLE - no longer needed with Auth0 SDK!)
```

Click **Save Changes**

---

## Step 3: Create Auth0 Action for Custom Logic

### 3.1 Create New Action

1. Navigate to: **Actions** → **Library**
2. Click: **Build Custom**
3. Fill form:
   - **Name:** `Passport Custom Logic`
   - **Trigger:** **Login / Post Login**
   - **Runtime:** **Node 18** (recommended)
4. Click **Create**

### 3.2 Implement Action Code

Replace the template code with:

```javascript
/**
 * Passport Custom Authentication Logic
 *
 * This Action runs after successful Google authentication to:
 * 1. Check if user is banned
 * 2. Add custom claims to tokens
 * 3. Log authentication events
 * 4. Handle first-time user setup
 */

exports.onExecutePostLogin = async (event, api) => {
  const { email, user_id, name, picture } = event.user;
  const provider = event.connection.name; // 'google'
  const isFirstLogin = event.stats.logins_count === 1;

  console.log(`[Passport] Auth attempt: ${email} via ${provider} (login #${event.stats.logins_count})`);

  try {
    // 1. Check if user is banned
    const banCheckResponse = await fetch(event.secrets.BAN_CHECK_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${event.secrets.API_KEY}`
      },
      body: JSON.stringify({ email, provider }),
      signal: AbortSignal.timeout(5000) // 5 second timeout
    });

    const banCheck = await banCheckResponse.json();

    if (banCheck.isBanned) {
      console.log(`[Passport] User banned: ${email}, reason: ${banCheck.reason}`);
      api.access.deny(`Account suspended: ${banCheck.reason}`);
      return; // Stop execution
    }

    // 2. Add custom claims to tokens
    api.idToken.setCustomClaim('https://immutable.com/email', email);
    api.idToken.setCustomClaim('https://immutable.com/provider', provider);

    // Fetch user profile for additional claims (if needed)
    if (event.secrets.PROFILE_API_URL) {
      try {
        const profileResponse = await fetch(`${event.secrets.PROFILE_API_URL}/${user_id}`, {
          headers: { 'Authorization': `Bearer ${event.secrets.API_KEY}` },
          signal: AbortSignal.timeout(5000)
        });

        const profile = await profileResponse.json();

        api.accessToken.setCustomClaim('https://immutable.com/org_id', profile.organizationId || 'default');
        api.accessToken.setCustomClaim('https://immutable.com/tier', profile.tier || 'free');
      } catch (error) {
        console.warn('[Passport] Profile fetch failed, using defaults:', error.message);
      }
    }

    // 3. First-time user setup
    if (isFirstLogin) {
      console.log(`[Passport] First login for ${email}, creating profile`);
      api.idToken.setCustomClaim('https://immutable.com/first_login', true);

      // Create user profile (fire and forget)
      if (event.secrets.CREATE_PROFILE_URL) {
        fetch(event.secrets.CREATE_PROFILE_URL, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${event.secrets.API_KEY}`
          },
          body: JSON.stringify({ user_id, email, name, picture, provider })
        }).catch(error => {
          console.error('[Passport] Profile creation failed:', error.message);
        });
      }
    }

    // 4. Log successful authentication (fire and forget)
    if (event.secrets.AUDIT_LOG_URL) {
      fetch(event.secrets.AUDIT_LOG_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${event.secrets.API_KEY}`
        },
        body: JSON.stringify({
          timestamp: new Date().toISOString(),
          userId: user_id,
          email,
          provider,
          ipAddress: event.request.ip,
          userAgent: event.request.userAgent,
          location: event.request.geoip
        })
      }).catch(error => {
        console.error('[Passport] Audit log failed:', error.message);
      });
    }

    console.log(`[Passport] Authentication successful: ${email}`);

  } catch (error) {
    console.error('[Passport] Action error:', error.message);
    // Fail open: allow authentication despite error
    console.warn('[Passport] Allowing auth despite error');
  }
};
```

### 3.3 Add Secrets

Click **Secrets** (lock icon) in the left sidebar and add:

```yaml
API_KEY: [your-immutable-api-key-for-authentication]
BAN_CHECK_URL: https://api.immutable.com/users/check-ban
PROFILE_API_URL: https://api.immutable.com/users
CREATE_PROFILE_URL: https://api.immutable.com/users/create-profile
AUDIT_LOG_URL: https://api.immutable.com/audit/auth-events
```

**Note:** Only add the URLs for APIs you actually have. The Action code handles missing secrets gracefully.

### 3.4 Test Action

1. Click **Test** tab
2. Modify test payload:
```json
{
  "user": {
    "email": "test@gmail.com",
    "user_id": "google-oauth2|123456789",
    "name": "Test User",
    "picture": "https://example.com/photo.jpg"
  },
  "connection": {
    "name": "google"
  },
  "stats": {
    "logins_count": 1
  },
  "request": {
    "ip": "192.168.1.1"
  }
}
```
3. Click **Run Test**
4. Verify console output shows no errors

### 3.5 Deploy Action

1. Click **Deploy** button
2. Action is now in **Deployed** state

---

## Step 4: Add Action to Login Flow

### 4.1 Navigate to Flows

1. Go to: **Actions** → **Flows**
2. Click: **Login**

### 4.2 Add Action to Flow

1. Find **Passport Custom Logic** in the right panel (under **Custom**)
2. **Drag and drop** it into the flow diagram
3. Place it between **Start** and **Complete**

**Flow should look like:**
```
[Start] → [Passport Custom Logic] → [Complete]
```

### 4.3 Apply Flow

Click **Apply** in the top right

---

## ✅ Auth0 Configuration Complete!

Verify completion:
- [x] Google connection configured
- [x] Native Social Login enabled
- [x] Android Client ID added to allowed list
- [x] Connection enabled for Passport Unity app
- [x] Password grant disabled
- [x] Auth0 Action created and deployed
- [x] Action added to Login flow

---

## Next Steps

Now proceed with code implementation:

1. **Android Plugin:** Create `Auth0NativeHelper.kt`
2. **Unity Integration:** Update `PassportImpl.cs`
3. **Remove Custom Backend:** Delete Go handlers
4. **Testing:** Build APK and test on device

See `AUTH0_SDK_IMPLEMENTATION_CODE.md` for complete code.

---

## Troubleshooting

### Issue: "Audience mismatch" error

**Cause:** Android Client ID not added to "Allowed Mobile Client IDs"

**Fix:** Go back to Step 1.3 and add your Android Client ID

### Issue: Action doesn't run

**Cause:** Action not added to Login flow

**Fix:** Go back to Step 4 and drag Action into flow

### Issue: "Password grant not allowed"

**Cause:** Password grant still enabled (from old custom implementation)

**Fix:** Go to Application Settings → Advanced → Grant Types → Disable Password

---

## Auth0 Dashboard Quick Links

- **Google Connection:** https://manage.auth0.com/dashboard/us/prod/connections/social
- **Applications:** https://manage.auth0.com/dashboard/us/prod/applications
- **Actions:** https://manage.auth0.com/dashboard/us/prod/actions/library
- **Flows:** https://manage.auth0.com/dashboard/us/prod/actions/flows
- **Logs:** https://manage.auth0.com/dashboard/us/prod/logs

---

**Status:** 🟢 Ready for code implementation after completing these configuration steps

**Estimated Time:** 30-45 minutes for Auth0 configuration
