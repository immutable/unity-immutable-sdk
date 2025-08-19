# UI Tests

## Prerequisites

### Passport SDK Log Level Configuration

For the authentication flow tests to work properly, the Passport SDK must be configured with an appropriate log level that enables auth URL capture. The test automation relies on capturing authentication URLs from Unity's Player.log.

**Required Configuration:**

In your Unity project's Passport initialisation script, ensure the log level is set to `Info` or `Debug`:

**File:** `src/Packages/Passport/Runtime/Scripts/Passport/PassportInitialisation/PassportInitialisationScript.cs`

```csharp
// Set the log level for the SDK (required for test automation)
Passport.LogLevel = LogLevel.Info; // or LogLevel.Debug
```

**Why This Is Required:**

- The test framework captures authentication URLs from Unity logs using `PassportLogger.Info()` calls
- Without proper logging, authentication URL interception will fail
- This enables the workaround for browser process isolation issues in automated testing environments

**Log Patterns Captured:**

The tests monitor Unity's `Player.log` for these patterns:

- `[Immutable] PASSPORT_AUTH_URL: <url>`
- `[Immutable] [Browser Communications Manager] LaunchAuthURL : <url>`

If authentication tests fail to capture URLs, verify that:

1. The Passport SDK log level is set correctly
2. Unity's Player.log is being written to the expected location
3. The authentication flow is actually being triggered
