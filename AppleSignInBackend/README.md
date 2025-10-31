# Apple Sign-in Backend (BFF)

A Backend-for-Frontend (BFF) server for Apple Sign-in integration with Immutable Passport Unity SDK. This server securely handles Apple identity token verification and exchanges them for Passport authentication tokens.

## Overview

This BFF is a critical security component that:
- **Protects sensitive credentials**: Securely stores your Immutable Project API Key (must never be in client applications)
- **Verifies Apple tokens**: Validates Apple identity tokens against Apple's public keys
- **Performs secure token exchange**: Communicates server-to-server with Immutable's authentication service

**Production Requirement**: Studios must host and maintain this BFF server for Apple Sign-in to work. The SDK will call your hosted BFF endpoint during the authentication flow.

## Features

- ✅ **Apple JWT Verification**: Real verification against Apple's public keys
- ✅ **Immutable Token Exchange**: Integrates with IMX Engine Auth Service
- ✅ **TypeScript Support**: Available in both TypeScript and JavaScript
- ✅ **Comprehensive Logging**: Detailed logs for debugging and monitoring
- ✅ **CORS Enabled**: Configurable for your client applications
- ✅ **Health Check**: Monitoring endpoint for uptime checks

---

## Quick Start

### 1. Install Dependencies

```bash
cd AppleSignInBackend
npm install
```

### 2. Configure Environment

Copy the example config:

```bash
cp env.example .env
```

Edit `.env`:

```bash
# Server Configuration
PORT=3000

# Apple Configuration
APPLE_BUNDLE_ID=com.yourstudio.yourgame

# Immutable Configuration
IMX_AUTH_API_URL=https://api.sandbox.immutable.com  # or https://api.immutable.com for production
IMMUTABLE_API_KEY=your_project_api_key_from_hub
PASSPORT_CLIENT_ID=your_passport_client_id
```

**Required Configuration:**
- `APPLE_BUNDLE_ID`: Your iOS app's bundle identifier (must match Xcode project)
- `IMMUTABLE_API_KEY`: Project API key from Immutable Hub (keep secret!)
- `PASSPORT_CLIENT_ID`: Your Passport OAuth client ID from Immutable Hub
- `IMX_AUTH_API_URL`: Environment endpoint (sandbox or production)

### 3. Build and Start Server

**TypeScript (Recommended):**

```bash
npm run build
npm start
```

**Development mode with hot-reload:**

```bash
npm run dev
```

**JavaScript (Legacy):**

```bash
npm run start:js
```

You should see:

```
========================================
Apple Sign-in Backend Started
========================================
Port: 3000
Bundle ID: com.yourstudio.yourgame

IMX Engine Configuration:
  Auth API URL: https://api.sandbox.immutable.com
  API Key: sk_imapik_xxx...
  Client ID: 2Dx7GLUZeFsMnmp1k...

Endpoints:
  POST   http://localhost:3000/auth/apple       - Apple Sign-in
  GET    http://localhost:3000/health           - Health check

Ready to receive Apple Sign-in requests from Unity!
========================================
```

---

## Unity Configuration

### Passport Initialization

Configure Passport to use your hosted BFF:

```csharp
// Initialize Passport with your environment
const string clientId = "YOUR_PASSPORT_CLIENT_ID";
const string environment = "production"; // or "sandbox"

var passport = await Passport.Init(
    clientId, 
    environment, 
    redirectUri, 
    logoutRedirectUri
);

// The SDK will determine the BFF URL based on environment
// For custom BFF URLs, configure via PassportConfig
```

The Unity SDK will call your BFF's `/auth/apple` endpoint during the Apple Sign-in flow.

---

## API Endpoints

### POST /auth/apple

Main authentication endpoint called by Unity SDK during Apple Sign-in.

**Request Headers:**
```
Content-Type: application/json
```

**Request Body:**

```json
{
  "identityToken": "eyJraWQiOiJIdlZJNkVzWlhKIi...",
  "authorizationCode": "c11fa69428a5b47e4ae6...",
  "userId": "001400.c6a985445c1e4e74a42e0d3dfe25697b.0005",
  "email": "user@privaterelay.appleid.com",
  "fullName": "John Doe",
  "clientId": "YOUR_PASSPORT_CLIENT_ID"
}
```

**Response (Success - 200):**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "id_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refresh_token": "v1.abc123...",
  "token_type": "Bearer",
  "expires_in": 86400
}
```

**Response (Error - 4xx/5xx):**

```json
{
  "error": "invalid_token",
  "error_description": "Apple token verification failed: invalid signature"
}
```

**Error Codes:**
- `400 Bad Request`: Missing required fields
- `401 Unauthorized`: Token verification failed
- `403 Forbidden`: Client ID mismatch
- `500 Internal Server Error`: Server error during token exchange

### GET /health

Health check endpoint for monitoring.

**Response (200):**

```json
{
  "status": "ok",
  "environment": "https://api.sandbox.immutable.com",
  "timestamp": "2025-10-30T12:34:56.789Z"
}
```

---

## How It Works

### Authentication Flow

1. **User initiates Apple Sign-in** in Unity app
2. **Apple authentication** completes, returns identity token
3. **Unity sends token to BFF** via POST /auth/apple
4. **BFF verifies Apple token** against Apple's public keys
5. **BFF extracts user email** from verified token
6. **BFF exchanges for Passport tokens** via IMX Engine Auth Service:
   - Endpoint: `/v1/token-exchange`
   - Headers: `x-immutable-api-key: YOUR_API_KEY`
   - Body: `{ email, client_id }`
7. **BFF returns Auth0 tokens** to Unity
8. **Unity completes authentication** with Passport

### Token Exchange (BYOA)

The BFF uses BYOA connection:

```typescript
// BFF calls IMX Engine Auth Service
POST https://api.immutable.com/v1/token-exchange
Headers:
  Content-Type: application/json
  x-immutable-api-key: YOUR_API_KEY
Body:
  {
    "email": "user@example.com",
    "client_id": "YOUR_PASSPORT_CLIENT_ID"
  }

// Returns Auth0 tokens
Response:
  {
    "access_token": "...",
    "id_token": "...",
    "refresh_token": "...",
    "token_type": "Bearer",
    "expires_in": 86400
  }
```

---

## Testing

### Health Check

Verify the server is running:

```bash
curl http://localhost:3000/health
```

Expected response:
```json
{
  "status": "ok",
  "environment": "https://api.sandbox.immutable.com",
  "timestamp": "2025-10-30T12:34:56.789Z"
}
```

### Test Authentication Flow

You can test the endpoint with cURL (requires valid Apple identity token):

```bash
curl -X POST http://localhost:3000/auth/apple \
  -H "Content-Type: application/json" \
  -d '{
    "identityToken": "VALID_APPLE_IDENTITY_TOKEN",
    "userId": "001400.xxx.0005",
    "email": "user@example.com",
    "clientId": "YOUR_PASSPORT_CLIENT_ID"
  }'
```

### Testing with Unity

1. Build and deploy your BFF server
2. Configure Unity SDK to use your BFF URL
3. Run the Unity app on iOS device
4. Tap "Sign in with Apple"
5. Complete Apple authentication
6. Check BFF logs for the request flow

**Expected BFF Logs:**

```
========================================
Apple Sign-in Request Received
========================================
User ID: 001400.xxx.0005
Email: user@privaterelay.appleid.com
Client ID: 2Dx7GLUZeFsMnmp1k...

Step 1: Verifying Apple identity token with Apple's public keys...
SUCCESS: Token verified successfully
   Token subject (sub): 001400.xxx.0005
   Token email: user@privaterelay.appleid.com

Step 2: Exchanging for Auth0 tokens via IMX Engine Auth Service...
SUCCESS: Token exchange successful
   Status: 200
   Token Type: Bearer
   Expires In: 86400

========================================
SUCCESS: Apple Sign-in Complete - Returning Tokens
========================================
```

---

## Scripts

```bash
# TypeScript
npm run build          # Compile TypeScript to JavaScript
npm start              # Run compiled JavaScript (production)
npm run dev            # Development with hot-reload

# Build utilities
npm run dev:build      # Watch mode - compile on changes
```
