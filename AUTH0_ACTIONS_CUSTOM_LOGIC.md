# Auth0 Actions - Custom Authentication Logic

**Version:** 1.0
**Date:** 2025-10-18
**Purpose:** Implementing custom logic with Auth0 SDK approach

---

## Overview

When using Auth0 SDK instead of custom backend, you can still implement custom authentication logic using **Auth0 Actions**.

**Auth0 Actions** are serverless functions that run during the authentication flow, allowing you to:
- ✅ Check if user is banned
- ✅ Add custom claims to tokens
- ✅ Call external APIs for validation
- ✅ Log authentication events
- ✅ Enforce business rules

**Comparison with Custom Backend:**

| Capability | Custom Backend | Auth0 Actions |
|------------|----------------|---------------|
| **When runs** | Before Auth0 user creation | After Auth0 user creation |
| **Language** | Go | JavaScript (Node.js) |
| **Hosting** | Your servers | Auth0 (serverless) |
| **Can block auth?** | ✅ Yes (before user) | ✅ Yes (after user, throw error) |
| **Can add claims?** | ✅ Yes | ✅ Yes |
| **Call external APIs?** | ✅ Yes | ✅ Yes |
| **Maintenance** | You maintain | Auth0 maintains infrastructure |

---

## Table of Contents

1. [Action Triggers](#action-triggers)
2. [Creating Actions](#creating-actions)
3. [Common Use Cases](#common-use-cases)
4. [Advanced Patterns](#advanced-patterns)
5. [Testing Actions](#testing-actions)
6. [Production Considerations](#production-considerations)
7. [Limitations](#limitations)

---

## Action Triggers

Auth0 Actions can run at different points in the authentication flow:

| Trigger | When It Runs | Use Case |
|---------|--------------|----------|
| **Post-Login** | After successful authentication | Add claims, check bans, log events |
| **Pre-User Registration** | Before user is created | Validate email domain, set initial metadata |
| **Post-User Registration** | After user is created | Send welcome email, create related records |
| **Post-Change Password** | After password change | Notify user, log security event |
| **Send Phone Message** | Before SMS sent | Customize SMS content, use custom provider |
| **M2M Token Exchange** | Machine-to-machine auth | Add custom scopes |

**For Native Social Login:** Use **Post-Login** trigger

---

## Creating Actions

### Step 1: Create Action

**Auth0 Dashboard:**

1. Navigate to: **Actions** → **Library**
2. Click **Build Custom**
3. Fill form:
   - Name: `Passport Authentication Logic`
   - Trigger: **Login / Post Login**
   - Runtime: **Node 18** (recommended)

### Step 2: Write Action Code

**Template:**

```javascript
/**
* Handler that will be called during the execution of a PostLogin flow.
*
* @param {Event} event - Details about the user and the context in which they are logging in.
* @param {PostLoginAPI} api - Interface whose methods can be used to change the behavior of the login.
*/
exports.onExecutePostLogin = async (event, api) => {
  // Your custom logic here

  console.log('User authenticated:', event.user.email);

  // Example: Add custom claim
  api.idToken.setCustomClaim('custom_claim', 'value');
};
```

### Step 3: Add to Flow

**Auth0 Dashboard:**

1. Navigate to: **Actions** → **Flows** → **Login**
2. Drag your Action from right panel
3. Place between **Start** and **Complete**
4. Click **Apply**

---

## Common Use Cases

### Use Case 1: Ban User Check

**Requirement:** Prevent banned users from authenticating

**Implementation:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const email = event.user.email;

  // Call your ban check API
  try {
    const response = await fetch('https://api.immutable.com/users/check-ban', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${event.secrets.API_KEY}`
      },
      body: JSON.stringify({ email })
    });

    const result = await response.json();

    if (result.isBanned) {
      console.log(`User banned: ${email}, reason: ${result.reason}`);

      // DENY authentication
      api.access.deny(`Account suspended: ${result.reason}`);
      return; // Stop execution
    }

    console.log(`Ban check passed for: ${email}`);
  } catch (error) {
    console.error('Ban check API failed:', error);

    // DECISION: Fail open (allow) or fail closed (deny)?
    // For high-security: deny on error
    // For availability: allow on error

    // Fail open (allow if API is down)
    console.warn('Ban check failed, allowing authentication');
  }
};
```

**Testing:**

```bash
# Test with banned user
# Should see error: "Account suspended: [reason]"
```

### Use Case 2: Add Custom Claims

**Requirement:** Add organization ID, user tier, custom metadata to tokens

**Implementation:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const email = event.user.email;
  const userId = event.user.user_id;

  // Fetch user's organization and tier from your API
  try {
    const response = await fetch(`https://api.immutable.com/users/${userId}/profile`, {
      headers: { 'Authorization': `Bearer ${event.secrets.API_KEY}` }
    });

    const profile = await response.json();

    // Add claims to ID token (visible to client)
    api.idToken.setCustomClaim('https://immutable.com/org_id', profile.organizationId);
    api.idToken.setCustomClaim('https://immutable.com/tier', profile.subscriptionTier);
    api.idToken.setCustomClaim('https://immutable.com/role', profile.role);

    // Add claims to access token (used for API authorization)
    api.accessToken.setCustomClaim('https://immutable.com/org_id', profile.organizationId);
    api.accessToken.setCustomClaim('https://immutable.com/permissions', profile.permissions);

    console.log(`Custom claims added for ${email}`);
  } catch (error) {
    console.error('Failed to fetch user profile:', error);
    // Continue without custom claims (fail gracefully)
  }
};
```

**Why `https://immutable.com/` prefix?**
Auth0 requires custom claims to be namespaced with a URL to avoid collisions.

**Accessing claims in Unity:**

```csharp
// Decode ID token
var idToken = JsonUtility.FromJson<IdToken>(idTokenString);
string orgId = idToken.GetClaim("https://immutable.com/org_id");
string tier = idToken.GetClaim("https://immutable.com/tier");
```

### Use Case 3: Authentication Logging

**Requirement:** Log all authentication attempts for security monitoring

**Implementation:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const logData = {
    timestamp: new Date().toISOString(),
    userId: event.user.user_id,
    email: event.user.email,
    provider: event.connection.name, // 'google'
    ipAddress: event.request.ip,
    userAgent: event.request.userAgent,
    location: event.request.geoip,
    successful: true
  };

  // Send to your logging service
  try {
    await fetch('https://api.immutable.com/audit/auth-events', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${event.secrets.API_KEY}`
      },
      body: JSON.stringify(logData)
    });

    console.log('Auth event logged:', logData.email);
  } catch (error) {
    console.error('Failed to log auth event:', error);
    // Don't block authentication if logging fails
  }
};
```

### Use Case 4: Email Domain Restriction

**Requirement:** Only allow users from specific email domains (e.g., company domain)

**Implementation:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const email = event.user.email;
  const allowedDomains = ['immutable.com', 'partner.com'];

  const domain = email.split('@')[1];

  if (!allowedDomains.includes(domain)) {
    console.log(`Blocked login from unauthorized domain: ${domain}`);
    api.access.deny(`Only ${allowedDomains.join(', ')} email addresses are allowed`);
    return;
  }

  console.log(`Domain check passed: ${domain}`);
};
```

### Use Case 5: First-Time User Setup

**Requirement:** Create related records when user logs in for first time

**Implementation:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const isFirstLogin = event.stats.logins_count === 1;

  if (isFirstLogin) {
    console.log('First login detected for:', event.user.email);

    try {
      // Create user profile in your database
      await fetch('https://api.immutable.com/users/create-profile', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${event.secrets.API_KEY}`
        },
        body: JSON.stringify({
          auth0Id: event.user.user_id,
          email: event.user.email,
          name: event.user.name,
          picture: event.user.picture,
          provider: event.connection.name
        })
      });

      console.log('User profile created');

      // Add claim indicating first login
      api.idToken.setCustomClaim('https://immutable.com/first_login', true);
    } catch (error) {
      console.error('Failed to create user profile:', error);
      // Continue authentication even if profile creation fails
    }
  }
};
```

---

## Advanced Patterns

### Pattern 1: Rate Limiting

**Prevent brute force attacks:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const email = event.user.email;
  const ipAddress = event.request.ip;

  // Check rate limit (e.g., max 10 logins per hour per IP)
  const response = await fetch(`https://api.immutable.com/rate-limit/check`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key: ipAddress, limit: 10, window: 3600 })
  });

  const rateLimit = await response.json();

  if (rateLimit.exceeded) {
    api.access.deny('Too many login attempts. Please try again later.');
    return;
  }
};
```

### Pattern 2: Multi-Factor Requirement

**Force MFA for specific users:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const userRole = event.user.app_metadata?.role;

  // Require MFA for admins
  if (userRole === 'admin' && !event.authentication.methods.some(m => m.name === 'mfa')) {
    api.access.deny('Multi-factor authentication required for admin users');
    return;
  }
};
```

### Pattern 3: Progressive Profiling

**Collect additional info over time:**

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const missingFields = [];

  if (!event.user.user_metadata?.phone) missingFields.push('phone');
  if (!event.user.user_metadata?.country) missingFields.push('country');

  if (missingFields.length > 0 && event.stats.logins_count > 3) {
    // After 3 logins, prompt for missing fields
    api.idToken.setCustomClaim('https://immutable.com/missing_profile_fields', missingFields);
  }
};
```

---

## Testing Actions

### Local Testing

**Auth0 provides a testing interface:**

1. In Action editor, click **Test** tab
2. Modify test payload:

```json
{
  "user": {
    "email": "test@gmail.com",
    "user_id": "google-oauth2|123456"
  },
  "connection": {
    "name": "google"
  },
  "request": {
    "ip": "192.168.1.1"
  }
}
```

3. Click **Run Test**
4. View console logs and result

### Integration Testing

**Test with real authentication:**

```bash
# 1. Deploy Action to flow
# 2. Build Unity APK
# 3. Test login on device
# 4. Check Auth0 Dashboard → Monitoring → Logs
```

---

## Production Considerations

### Performance

**Actions add latency to authentication:**
- Each Action: ~50-200ms
- External API calls: +100-500ms each

**Optimization:**
```javascript
// BAD: Sequential API calls
const ban = await checkBan();
const profile = await fetchProfile();
const permissions = await fetchPermissions();

// GOOD: Parallel API calls
const [ban, profile, permissions] = await Promise.all([
  checkBan(),
  fetchProfile(),
  fetchPermissions()
]);
```

### Error Handling

**Always handle errors gracefully:**

```javascript
try {
  const result = await externalAPI();
} catch (error) {
  console.error('External API failed:', error);

  // DECISION: Fail open (allow) or fail closed (deny)?
  // Document your choice!

  // For non-critical features: fail open
  console.warn('Continuing despite API failure');

  // For critical security: fail closed
  // api.access.deny('Unable to verify user. Please try again later.');
}
```

### Secrets Management

**Store sensitive data in secrets:**

1. In Action editor, click **Secrets** (lock icon)
2. Add:
   ```
   API_KEY: [your-key]
   BAN_CHECK_URL: https://api.immutable.com/users/check-ban
   ```

3. Use in code:
   ```javascript
   const apiKey = event.secrets.API_KEY;
   const url = event.secrets.BAN_CHECK_URL;
   ```

### Monitoring

**Check Action execution:**

1. Auth0 Dashboard → **Monitoring** → **Logs**
2. Filter by Action name
3. View execution time, errors, console logs

---

## Limitations

### Auth0 Actions Limitations

| Limitation | Impact | Workaround |
|------------|--------|------------|
| **Timeout: 20 seconds** | Action must complete in 20s | Use fast APIs, parallelize calls |
| **Size limit: 1MB** | Code + dependencies < 1MB | Use lightweight libraries |
| **No NPM packages** | Can't use arbitrary NPM modules | Use built-in fetch, limited libraries allowed |
| **Cold start latency** | First run slower (~500ms) | Accept latency or keep action warm |
| **Runs AFTER user creation** | Can't prevent Auth0 user | Deny in Action, but user exists |

### Comparison with Custom Backend

| Feature | Custom Backend | Auth0 Actions |
|---------|----------------|---------------|
| **Runs before user creation?** | ✅ Yes | ❌ No (user created first) |
| **Any programming language?** | ✅ Yes | ❌ JavaScript only |
| **Unlimited execution time?** | ✅ Yes | ❌ 20 second max |
| **Unrestricted dependencies?** | ✅ Yes | ❌ Limited libraries |
| **Full control?** | ✅ Yes | ⚠️ Auth0 platform limits |
| **Maintenance burden?** | ❌ High | ✅ Low (Auth0 hosts) |
| **Scalability?** | ⚠️ You manage | ✅ Auto-scales |

---

## When to Use Custom Backend Instead

**Use custom backend if:**
- ✅ Must prevent Auth0 user creation entirely
- ✅ Need execution time > 20 seconds
- ✅ Require languages other than JavaScript
- ✅ Need unrestricted NPM dependencies
- ✅ Complex business logic (thousands of lines)

**Use Auth0 Actions if:**
- ✅ Logic is relatively simple (<500 lines)
- ✅ Can run AFTER user creation
- ✅ Want Auth0 to handle hosting/scaling
- ✅ Don't want to maintain custom infrastructure

---

## Example: Complete Production Action

```javascript
/**
 * Passport Production Authentication Logic
 *
 * Handles:
 * - Ban checking
 * - Custom claims
 * - Audit logging
 * - First-time user setup
 * - Error handling
 */

const ALLOWED_DOMAINS = ['gmail.com', 'immutable.com'];
const BAN_CHECK_TIMEOUT = 5000; // 5 seconds
const PROFILE_FETCH_TIMEOUT = 5000;

exports.onExecutePostLogin = async (event, api) => {
  const { email, user_id } = event.user;
  const provider = event.connection.name;
  const isFirstLogin = event.stats.logins_count === 1;

  console.log(`[Passport] Auth attempt: ${email} via ${provider} (login #${event.stats.logins_count})`);

  try {
    // 1. Domain check (synchronous)
    const domain = email.split('@')[1];
    if (!ALLOWED_DOMAINS.includes(domain)) {
      console.log(`[Passport] Blocked unauthorized domain: ${domain}`);
      api.access.deny(`Only ${ALLOWED_DOMAINS.join(', ')} domains allowed`);
      return;
    }

    // 2. Parallel API calls with timeouts
    const [banCheck, profile] = await Promise.all([
      checkIfBanned(email, event.secrets.API_KEY).catch(e => {
        console.error('[Passport] Ban check failed:', e);
        return { isBanned: false }; // Fail open for availability
      }),
      fetchUserProfile(user_id, event.secrets.API_KEY).catch(e => {
        console.error('[Passport] Profile fetch failed:', e);
        return null; // Continue without profile
      })
    ]);

    // 3. Ban check result
    if (banCheck.isBanned) {
      console.log(`[Passport] User banned: ${email}`);
      api.access.deny(`Account suspended: ${banCheck.reason}`);
      return;
    }

    // 4. Add custom claims
    if (profile) {
      api.idToken.setCustomClaim('https://immutable.com/org_id', profile.organizationId);
      api.idToken.setCustomClaim('https://immutable.com/tier', profile.tier);
      api.accessToken.setCustomClaim('https://immutable.com/org_id', profile.organizationId);
    }

    // 5. First-time user setup
    if (isFirstLogin) {
      api.idToken.setCustomClaim('https://immutable.com/first_login', true);
      createUserProfile(user_id, email, event.secrets.API_KEY).catch(e => {
        console.error('[Passport] Profile creation failed:', e);
        // Continue even if profile creation fails
      });
    }

    // 6. Audit log (fire and forget)
    logAuthEvent({
      userId: user_id,
      email,
      provider,
      ip: event.request.ip,
      timestamp: new Date().toISOString()
    }, event.secrets.API_KEY).catch(e => {
      console.error('[Passport] Audit log failed:', e);
    });

    console.log(`[Passport] Auth successful: ${email}`);

  } catch (error) {
    console.error('[Passport] Unexpected error:', error);
    // Fail open: allow authentication despite error
    console.warn('[Passport] Allowing auth despite error');
  }
};

// Helper functions
async function checkIfBanned(email, apiKey) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), BAN_CHECK_TIMEOUT);

  try {
    const response = await fetch('https://api.immutable.com/users/check-ban', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${apiKey}` },
      body: JSON.stringify({ email }),
      signal: controller.signal
    });
    return await response.json();
  } finally {
    clearTimeout(timeout);
  }
}

async function fetchUserProfile(userId, apiKey) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), PROFILE_FETCH_TIMEOUT);

  try {
    const response = await fetch(`https://api.immutable.com/users/${userId}/profile`, {
      headers: { 'Authorization': `Bearer ${apiKey}` },
      signal: controller.signal
    });
    return await response.json();
  } finally {
    clearTimeout(timeout);
  }
}

async function createUserProfile(userId, email, apiKey) {
  await fetch('https://api.immutable.com/users/create-profile', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${apiKey}` },
    body: JSON.stringify({ userId, email })
  });
}

async function logAuthEvent(data, apiKey) {
  await fetch('https://api.immutable.com/audit/auth-events', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${apiKey}` },
    body: JSON.stringify(data)
  });
}
```

---

**Back to:** [INDEX](./ANDROID_NATIVE_GOOGLE_AUTH_INDEX.md) | **See Also:** [MIGRATION_GUIDE](./MIGRATION_GUIDE_AUTH0_SDK.md)
