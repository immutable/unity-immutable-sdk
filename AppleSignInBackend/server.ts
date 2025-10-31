import 'dotenv/config';
import express, { Request, Response, NextFunction } from 'express';
import cors from 'cors';
import jwt, { JwtHeader, VerifyErrors } from 'jsonwebtoken';
import jwksClient, { SigningKey, RsaSigningKey } from 'jwks-rsa';
import axios, { AxiosError } from 'axios';

const app = express();
const PORT = process.env.PORT || 3000;

// Configuration
const APPLE_BUNDLE_ID = process.env.APPLE_BUNDLE_ID || 'com.immutable.Immutable-Sample-GameSDK';

// IMX Engine Auth Service Configuration
const IMX_AUTH_API_URL = process.env.IMX_AUTH_API_URL || 'https://api.sandbox.immutable.com';
const IMMUTABLE_API_KEY = process.env.IMMUTABLE_API_KEY;
const PASSPORT_CLIENT_ID = process.env.PASSPORT_CLIENT_ID;

// Validate required configuration
if (!IMMUTABLE_API_KEY) {
  console.error('ERROR: IMMUTABLE_API_KEY is required but not set');
  console.error('   Set it in your .env file');
  process.exit(1);
}

if (!PASSPORT_CLIENT_ID) {
  console.error('ERROR: PASSPORT_CLIENT_ID is required but not set');
  console.error('   Set it in your .env file');
  process.exit(1);
}

// Type definitions
interface AppleDecodedToken {
  sub: string;
  email?: string;
  iss: string;
  aud: string;
  exp: number;
  iat: number;
}

interface AppleSignInRequest {
  identityToken: string;
  authorizationCode?: string;
  userId: string;
  email?: string;
  fullName?: string;
  clientId: string;
}

interface Auth0TokenResponse {
  access_token: string;
  id_token?: string;
  refresh_token?: string;
  token_type: string;
  expires_in: number;
}

interface TokenExchangeRequest {
  email: string;
  client_id: string;
}

interface ErrorResponse {
  error: string;
  error_description: string;
}

interface HealthResponse {
  status: string;
  environment: string;
  timestamp: string;
}

// Middleware
app.use(cors());
app.use(express.json());

// Logging middleware
app.use((req: Request, res: Response, next: NextFunction) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.path}`);
  next();
});

// Apple JWKS client for real verification
const appleJwksClient = jwksClient({
  jwksUri: 'https://appleid.apple.com/auth/keys',
  cache: true,
  rateLimit: true
});

/**
 * Get Apple's public key for JWT verification
 */
function getApplePublicKey(
  header: JwtHeader,
  callback: (err: Error | null, publicKey?: string) => void
): void {
  appleJwksClient.getSigningKey(header.kid, (err: Error | null, key?: SigningKey) => {
    if (err) {
      callback(err);
      return;
    }
    if (!key) {
      callback(new Error('No signing key found'));
      return;
    }
    const signingKey = (key as RsaSigningKey).rsaPublicKey || key.getPublicKey();
    callback(null, signingKey);
  });
}

/**
 * Verify Apple identity token using Apple's public keys
 */
async function verifyAppleToken(identityToken: string): Promise<AppleDecodedToken> {
  return new Promise((resolve, reject) => {
    jwt.verify(
      identityToken,
      getApplePublicKey,
      {
        algorithms: ['RS256'],
        issuer: 'https://appleid.apple.com',
        audience: APPLE_BUNDLE_ID
      },
      (err: VerifyErrors | null, decoded: any) => {
        if (err) {
          reject(err);
          return;
        }
        resolve(decoded as AppleDecodedToken);
      }
    );
  });
}

/**
 * Exchange for Auth0 tokens via IMX Engine Auth Service
 * Uses the /v1/token-exchange endpoint
 */
async function exchangeForAuth0Tokens(
  email: string,
  clientId: string,
  apiKey: string
): Promise<Auth0TokenResponse> {
  if (!email) {
    throw new Error('Email is required for token exchange');
  }
  
  console.log('Calling IMX Engine Auth Service token exchange API...');
  console.log('   URL:', `${IMX_AUTH_API_URL}/v1/token-exchange`);
  console.log('   Email:', email);
  console.log('   Client ID:', clientId);
  
  try {
    const response = await axios.post<Auth0TokenResponse>(
      `${IMX_AUTH_API_URL}/v1/token-exchange`,
      {
        email: email,
        client_id: clientId
      } as TokenExchangeRequest,
      {
        headers: {
          'Content-Type': 'application/json',
          'x-immutable-api-key': apiKey
        }
      }
    );
    
    console.log('SUCCESS: Token exchange successful');
    console.log('   Status:', response.status);
    console.log('   Token Type:', response.data.token_type);
    console.log('   Expires In:', response.data.expires_in);
    console.log('   Response:', JSON.stringify(response.data, null, 2));
    
    return response.data;
  } catch (error) {
    console.error('ERROR: Token exchange API call failed');
    
    const axiosError = error as AxiosError;
    if (axiosError.response) {
      console.error('   Status:', axiosError.response.status);
      console.error('   Response:', JSON.stringify(axiosError.response.data, null, 2));
      throw new Error(`Token exchange failed with status ${axiosError.response.status}: ${JSON.stringify(axiosError.response.data)}`);
    } else if (axiosError.request) {
      console.error('   No response received from server');
      throw new Error('Token exchange failed: No response from server');
    } else {
      console.error('   Error:', axiosError.message);
      throw new Error(`Token exchange failed: ${axiosError.message}`);
    }
  }
}

// ========================================
// ROUTES
// ========================================

/**
 * Health check endpoint
 */
app.get('/health', (req: Request, res: Response<HealthResponse>) => {
  res.json({
    status: 'ok',
    environment: IMX_AUTH_API_URL,
    timestamp: new Date().toISOString()
  });
});

/**
 * Main Apple Sign-in endpoint
 * This is what Unity calls
 */
app.post('/auth/apple', async (req: Request<{}, {}, AppleSignInRequest>, res: Response<Auth0TokenResponse | ErrorResponse>) => {
  try {
    const { identityToken, authorizationCode, userId, email, fullName, clientId } = req.body;
    
    console.log('\n========================================');
    console.log('Apple Sign-in Request Received');
    console.log('========================================');
    console.log('User ID:', userId);
    console.log('Email:', email || '(not provided - will extract from token)');
    console.log('Full Name:', fullName || '(not provided)');
    console.log('Client ID:', clientId || '(missing)');
    console.log('Identity Token:', identityToken ? identityToken : '(missing)');
    console.log('Authorization Code:', authorizationCode ? authorizationCode : '(missing)');
    console.log('========================================\n');
    
    // Validate required fields
    if (!identityToken || !userId) {
      return res.status(400).json({
        error: 'invalid_request',
        error_description: 'Missing required fields: identityToken, userId'
      });
    }

    // Validate client ID matches expected value (security check)
    if (!clientId) {
      console.error('ERROR: Client ID not provided in request');
      return res.status(400).json({
        error: 'invalid_request',
        error_description: 'Client ID is required'
      });
    }

    if (clientId !== PASSPORT_CLIENT_ID) {
      console.error('ERROR: Client ID mismatch');
      console.error('   Expected:', PASSPORT_CLIENT_ID);
      console.error('   Received:', clientId);
      return res.status(403).json({
        error: 'invalid_client',
        error_description: 'Client ID does not match expected value'
      });
    }

    console.log('SUCCESS: Client ID validated');
    
    // Step 1: Verify Apple identity token
    console.log('Step 1: Verifying Apple identity token with Apple\'s public keys...');
    let decodedToken: AppleDecodedToken;
    
    try {
      decodedToken = await verifyAppleToken(identityToken);
    } catch (err) {
      console.error('ERROR: Apple token verification failed:', (err as Error).message);
      return res.status(401).json({
        error: 'invalid_token',
        error_description: `Apple token verification failed: ${(err as Error).message}`
      });
    }
    
    console.log('SUCCESS: Token verified successfully');
    console.log('   Token subject (sub):', decodedToken.sub);
    console.log('   Token email:', decodedToken.email);
    console.log('   Token issuer:', decodedToken.iss);
    console.log('   Token audience (aud):', decodedToken.aud);
    
    // Validate bundle ID matches token audience
    if (decodedToken.aud !== APPLE_BUNDLE_ID) {
      console.error('ERROR: Bundle ID mismatch with token audience');
      console.error('   Expected:', APPLE_BUNDLE_ID);
      console.error('   Token Audience:', decodedToken.aud);
      return res.status(401).json({
        error: 'invalid_token',
        error_description: 'Apple token audience does not match expected bundle ID'
      });
    }
    
    console.log('SUCCESS: Bundle ID matches token audience');
    
    // Step 2: Validate token claims
    if (decodedToken.sub !== userId) {
      console.error('ERROR: Token user ID mismatch');
      console.error('   Expected:', userId);
      console.error('   Got:', decodedToken.sub);
      return res.status(401).json({
        error: 'invalid_token',
        error_description: 'Token user ID does not match provided user ID'
      });
    }
    
    // Step 3: Extract email (prefer token email over provided email)
    const userEmail = decodedToken.email || email;
    if (!userEmail) {
      console.error('ERROR: No email found in token or request');
      return res.status(400).json({
        error: 'invalid_request',
        error_description: 'Email is required but was not found in token or request'
      });
    }
    
    console.log('SUCCESS: Using email:', userEmail);
    
    // Step 4: Exchange for Auth0 tokens via IMX Engine Auth Service
    console.log('\nStep 2: Exchanging for Auth0 tokens via IMX Engine Auth Service...');
    
    try {
      // { access_token, id_token, refresh_token, token_type, expires_in }
      const auth0Tokens = await exchangeForAuth0Tokens(
        userEmail,
        PASSPORT_CLIENT_ID,
        IMMUTABLE_API_KEY
      );
      
      console.log('SUCCESS: Auth0 tokens obtained');
      console.log('   Access Token:', auth0Tokens.access_token);
      console.log('   ID Token:', auth0Tokens.id_token ? auth0Tokens.id_token : '(none)');
      console.log('   Refresh Token:', auth0Tokens.refresh_token ? auth0Tokens.refresh_token : '(none)');
      
      console.log('\n========================================');
      console.log('SUCCESS: Apple Sign-in Complete - Returning Tokens');
      console.log('========================================\n');
      
      return res.json(auth0Tokens);
    } catch (err) {
      console.error('ERROR: Auth0 token exchange failed:', (err as Error).message);
      return res.status(401).json({
        error: 'token_exchange_failed',
        error_description: `Failed to exchange for Auth0 tokens: ${(err as Error).message}`
      });
    }
    
  } catch (error) {
    console.error('\nERROR: Error processing Apple Sign-in:', error);
    console.error('Stack trace:', (error as Error).stack);
    
    res.status(500).json({
      error: 'server_error',
      error_description: `Internal server error: ${(error as Error).message}`
    });
  }
});

// ========================================
// START SERVER
// ========================================

app.listen(PORT, () => {
  console.log('\n========================================');
  console.log('Apple Sign-in Backend Started');
  console.log('========================================');
  console.log('Port:', PORT);
  console.log('Bundle ID:', APPLE_BUNDLE_ID);
  console.log('');
  console.log('IMX Engine Configuration:');
  console.log('  Auth API URL:', IMX_AUTH_API_URL);
  console.log('  API Key:', `${IMMUTABLE_API_KEY.substring(0, 20)}...`);
  console.log('  Client ID:', `${PASSPORT_CLIENT_ID.substring(0, 20)}...`);
  console.log('');
  console.log('Endpoints:');
  console.log(`  POST   http://localhost:${PORT}/auth/apple       - Apple Sign-in`);
  console.log(`  GET    http://localhost:${PORT}/health           - Health check`);
  console.log('');
  console.log('Ready to receive Apple Sign-in requests from Unity!');
  console.log('========================================\n');
});

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('SIGTERM signal received: closing HTTP server');
  process.exit(0);
});

process.on('SIGINT', () => {
  console.log('\nSIGINT signal received: closing HTTP server');
  process.exit(0);
});

