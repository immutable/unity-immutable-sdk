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

// Auth0 SDK imports for native social authentication
import com.auth0.android.Auth0
import com.auth0.android.authentication.AuthenticationAPIClient
import com.auth0.android.authentication.AuthenticationException
import com.auth0.android.callback.Callback
import com.auth0.android.result.Credentials
import org.json.JSONObject
import java.util.UUID

/**
 * Auth0 Native Social Authentication Helper
 *
 * Integrates Android Credential Manager with Auth0 SDK for native social login.
 * Uses Auth0's loginWithNativeSocialToken() method for proper token handling.
 *
 * Flow:
 * 1. User taps login → Shows native Google account picker (Credential Manager)
 * 2. User selects account + biometric → Receives Google ID token
 * 3. Sends token to Auth0 SDK → Auth0 verifies and creates/finds user
 * 4. Auth0 returns JWT tokens (respects JWE dashboard settings) → Passed to Unity
 *
 * Token Format:
 * - Returns JWT (3 parts) when JWE is disabled in Auth0 Dashboard (APIs work ✓)
 * - Returns JWE (5 parts) when JWE is enabled in Auth0 Dashboard (APIs fail ✗)
 * - Check: Auth0 Dashboard → APIs → Platform API → Token Settings → JWE toggle
 *
 * @author Immutable SDK Team
 * @since 2025-10-18
 */
class Auth0NativeHelper(
    private val activity: Activity
) {
    companion object {
        private const val TAG = "Auth0NativeHelper"

        // Auth0 Configuration
        private const val AUTH0_DOMAIN = "auth.immutable.com"
        private const val PASSPORT_CLIENT_ID = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK"
        private const val API_AUDIENCE = "platform_api"
        private const val OAUTH_SCOPE = "openid profile email offline_access transact"

        // Google OAuth Configuration
        // IMPORTANT: Use WEB Client ID for serverClientId (required by Credential Manager)
        // Android Client ID is used internally by Google for APK signature validation
        private const val GOOGLE_WEB_CLIENT_ID = "410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com"

        // Unity Callback Configuration
        private const val UNITY_GAME_OBJECT = "PassportManager"
        private const val UNITY_SUCCESS_CALLBACK = "OnAuth0Success"
        private const val UNITY_ERROR_CALLBACK = "OnAuth0Error"

        // Singleton instance
        @Volatile
        private var instance: Auth0NativeHelper? = null

        /**
         * Get or create singleton instance
         * Called from Unity via AndroidJavaObject
         */
        @JvmStatic
        fun getInstance(): Auth0NativeHelper {
            return instance ?: synchronized(this) {
                instance ?: Auth0NativeHelper(UnityPlayer.currentActivity).also {
                    instance = it
                }
            }
        }
    }

    private val scope = CoroutineScope(Dispatchers.Main + SupervisorJob())
    private val auth0 = Auth0(PASSPORT_CLIENT_ID, AUTH0_DOMAIN)
    private val authenticationClient = AuthenticationAPIClient(auth0)

    /**
     * Launch native Google account picker and authenticate with Auth0
     *
     * This is the main entry point called from Unity.
     * Flow is asynchronous - result returned via Unity callbacks.
     */
    fun loginWithGoogle() {
        Log.d(TAG, "loginWithGoogle called")

        scope.launch {
            try {
                Log.d(TAG, "Launching Google Credential Manager...")

                // Step 1: Get Google ID token using Android Credential Manager
                val googleIdToken = getGoogleIdTokenFromCredentialManager()

                Log.d(TAG, "Google ID token received, authenticating with Auth0...")

                // Step 2: Send token to Auth0 for verification and user creation
                authenticateWithAuth0(googleIdToken)

            } catch (e: GetCredentialCancellationException) {
                Log.w(TAG, "User cancelled sign-in")
                sendErrorToUnity("User cancelled sign-in")

            } catch (e: NoCredentialException) {
                Log.w(TAG, "No Google account found on device")
                sendErrorToUnity("No Google account found. Please add a Google account to your device.")

            } catch (e: GetCredentialException) {
                Log.e(TAG, "Credential retrieval failed", e)
                sendErrorToUnity("Sign-in failed: ${e.message}")

            } catch (e: Exception) {
                Log.e(TAG, "Unexpected error during login", e)
                sendErrorToUnity("Login error: ${e.message}")
            }
        }
    }

    /**
     * Get Google ID token using Android Credential Manager
     *
     * Shows native Android UI for selecting Google account.
     * User authenticates with biometric/PIN.
     *
     * @return Google ID token (JWT)
     * @throws GetCredentialException if credential retrieval fails
     */
    private suspend fun getGoogleIdTokenFromCredentialManager(): String {
        val credentialManager = CredentialManager.create(activity)

        // Generate nonce for security (required by Auth0)
        val nonce = UUID.randomUUID().toString()
        Log.d(TAG, "Generated nonce for credential request")

        // Configure Google ID credential request
        val googleIdOption = GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)  // Show all Google accounts on device
            .setServerClientId(GOOGLE_WEB_CLIENT_ID)  // Web Client ID for backend validation
            .setNonce(nonce)  // Add nonce for Auth0 verification
            .setAutoSelectEnabled(false)  // Don't auto-select account (user must choose)
            .build()

        val request = GetCredentialRequest.Builder()
            .addCredentialOption(googleIdOption)
            .build()

        Log.d(TAG, "Requesting credential from Credential Manager...")

        // Launch native picker (suspends until user completes selection)
        val result = credentialManager.getCredential(
            request = request,
            context = activity
        )

        // Extract ID token from response
        return extractGoogleIdTokenFromCredential(result)
    }

    /**
     * Extract Google ID token from credential response
     *
     * @param result Credential response from Credential Manager
     * @return Google ID token (JWT)
     * @throws IllegalStateException if credential type is unexpected
     */
    private fun extractGoogleIdTokenFromCredential(result: GetCredentialResponse): String {
        val credential = result.credential

        // Verify it's a Google ID token credential
        if (credential is CustomCredential &&
            credential.type == GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL) {

            val googleIdTokenCredential = GoogleIdTokenCredential.createFrom(credential.data)

            Log.d(TAG, "Google ID token extracted successfully")
            Log.d(TAG, "User: ${googleIdTokenCredential.displayName} (${googleIdTokenCredential.id})")

            return googleIdTokenCredential.idToken

        } else {
            throw IllegalStateException("Unexpected credential type: ${credential.type}")
        }
    }

    /**
     * Authenticate with Auth0 using Google ID token
     *
     * Uses Auth0 SDK's loginWithNativeSocialToken() method for native social authentication.
     * This method properly respects the API's JWE settings and returns JWT tokens when
     * JWE encryption is disabled in the Auth0 dashboard.
     *
     * @param googleIdToken Google ID token from Credential Manager
     */
    private fun authenticateWithAuth0(googleIdToken: String) {
        Log.d(TAG, "Authenticating with Auth0 SDK...")
        Log.d(TAG, "Auth0 Domain: $AUTH0_DOMAIN")
        Log.d(TAG, "Client ID: $PASSPORT_CLIENT_ID")
        Log.d(TAG, "API Audience: $API_AUDIENCE")
        Log.d(TAG, "Scope: $OAUTH_SCOPE")

        // Use Auth0's native social login method
        authenticationClient
            .loginWithNativeSocialToken(
                googleIdToken,
                "http://auth0.com/oauth/token-type/google-id-token"
            )
            .setAudience(API_AUDIENCE)
            .setScope(OAUTH_SCOPE)
            .start(object : Callback<Credentials, AuthenticationException> {
                override fun onFailure(error: AuthenticationException) {
                    Log.e(TAG, "Auth0 authentication failed", error)
                    Log.e(TAG, "Error code: ${error.getCode()}")
                    Log.e(TAG, "Error description: ${error.getDescription()}")

                    val errorMessage = when {
                        error.isCanceled -> "Authentication cancelled"
                        error.isNetworkError -> "Network error: ${error.message}"
                        else -> "Authentication failed: ${error.getDescription()}"
                    }

                    sendErrorToUnity(errorMessage)
                }

                override fun onSuccess(credentials: Credentials) {
                    Log.d(TAG, "Auth0 authentication successful!")

                    val accessToken = credentials.accessToken
                    val idToken = credentials.idToken

                    Log.d(TAG, "Access token length: ${accessToken.length}")
                    Log.d(TAG, "ID token length: ${idToken.length}")
                    Log.d(TAG, "Token type: ${credentials.type}")

                    // Verify access token format (JWT should have 3 parts separated by '.')
                    val accessTokenParts = accessToken.split(".")
                    Log.d(TAG, "Access token format: ${accessTokenParts.size} parts (should be 3 for JWT)")
                    if (accessTokenParts.size == 3) {
                        Log.d(TAG, "✓ Access token is valid JWT format - APIs will work!")
                    } else if (accessTokenParts.size == 5) {
                        Log.e(TAG, "✗ Access token is JWE encrypted format - API calls will fail!")
                        Log.e(TAG, "  Check Auth0 Dashboard → APIs → Platform API → Token Settings")
                        Log.e(TAG, "  Ensure 'JSON Web Encryption (JWE)' toggle is OFF")
                    } else {
                        Log.w(TAG, "⚠ Access token has unexpected format: ${accessTokenParts.size} parts")
                    }

                    sendSuccessToUnity(credentials)
                }
            })
    }

    /**
     * Send authentication success result to Unity
     *
     * Serializes Auth0 Credentials to JSON and calls Unity callback via UnitySendMessage.
     *
     * @param credentials Auth0 Credentials object containing access token, ID token, etc.
     */
    private fun sendSuccessToUnity(credentials: Credentials) {
        // Build JSON for Unity from Auth0 Credentials
        val unityJson = JSONObject().apply {
            put("access_token", credentials.accessToken)
            put("id_token", credentials.idToken)
            put("refresh_token", credentials.refreshToken ?: "")
            put("token_type", credentials.type)
            // Calculate expires_at from expiresAt timestamp
            put("expires_at", credentials.expiresAt.time)
            put("scope", credentials.scope ?: "")
        }.toString()

        Log.d(TAG, "Sending success to Unity: ${UNITY_GAME_OBJECT}.${UNITY_SUCCESS_CALLBACK}")
        Log.d(TAG, "Unity JSON payload length: ${unityJson.length} characters")
        Log.d(TAG, "Unity JSON access_token length: ${credentials.accessToken.length}")
        Log.d(TAG, "Unity JSON id_token length: ${credentials.idToken.length}")

        try {
            UnityPlayer.UnitySendMessage(
                UNITY_GAME_OBJECT,
                UNITY_SUCCESS_CALLBACK,
                unityJson
            )
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send success to Unity", e)
        }
    }

    /**
     * Send authentication error to Unity
     *
     * @param errorMessage User-friendly error message
     */
    private fun sendErrorToUnity(errorMessage: String) {
        Log.d(TAG, "Sending error to Unity: ${UNITY_GAME_OBJECT}.${UNITY_ERROR_CALLBACK}")

        try {
            UnityPlayer.UnitySendMessage(
                UNITY_GAME_OBJECT,
                UNITY_ERROR_CALLBACK,
                errorMessage
            )
        } catch (e: Exception) {
            Log.e(TAG, "Failed to send error to Unity", e)
        }
    }

    /**
     * Cleanup when helper is no longer needed
     */
    fun dispose() {
        Log.d(TAG, "Disposing Auth0NativeHelper")
        instance = null
    }
}
