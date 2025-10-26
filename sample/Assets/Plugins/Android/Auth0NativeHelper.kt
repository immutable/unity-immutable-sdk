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

// Auth0 SDK imports (not using AuthenticationAPIClient anymore - direct HTTP instead)
import com.auth0.android.Auth0

// OkHttp for direct API calls
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONObject
import java.util.UUID

/**
 * Auth0 Native Social Authentication Helper
 *
 * Integrates Android Credential Manager with Auth0's native social login.
 * Replaces custom backend implementation with Auth0 SDK.
 *
 * Flow:
 * 1. User taps login → Shows native Google account picker (Credential Manager)
 * 2. User selects account + biometric → Receives Google ID token
 * 3. Sends token to Auth0 via SDK → Auth0 verifies and creates/finds user
 * 4. Auth0 returns tokens → Passed back to Unity
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
        private const val AUTH0_DOMAIN = "prod.immutable.auth0app.com"
        private const val AUTH0_CLIENT_ID = "mp6rxfMDwwZDogcdgNrAaHnG0qMlXuMK"

        // Google OAuth Configuration
        // IMPORTANT: Use WEB Client ID for serverClientId (required by Credential Manager)
        // Android Client ID is used internally by Google for APK signature validation
        private const val GOOGLE_WEB_CLIENT_ID = "410239185541-kgflh9f9g1a0r2vrs7ilto5f8521od77.apps.googleusercontent.com"

        // Android Client ID - must be in Auth0's "Allowed Mobile Client IDs"
        private const val GOOGLE_ANDROID_CLIENT_ID = "410239185541-hkielganvnnvgmd40iep6c630d15bfr4.apps.googleusercontent.com"

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
    private val httpClient = OkHttpClient()

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
     * Authenticate with Auth0 using Google ID token via direct HTTP POST
     *
     * Makes OAuth 2.0 Token Exchange request directly to Auth0's /oauth/token endpoint
     * This bypasses Auth0 SDK limitations with native social login
     *
     * @param googleIdToken Google ID token (JWT)
     */
    private fun authenticateWithAuth0(googleIdToken: String) {
        Log.d(TAG, "Calling Auth0 /oauth/token endpoint...")

        scope.launch(Dispatchers.IO) {
            try {
                // Build JSON request body for Auth0 native social login
                // Auth0 expects specific format for Google ID token exchange
                val requestBody = JSONObject().apply {
                    put("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange")
                    put("subject_token", googleIdToken)
                    put("subject_token_type", "http://auth0.com/oauth/token-type/google-id-token")
                    put("client_id", AUTH0_CLIENT_ID)
                    put("scope", "openid profile email offline_access")
                }.toString()

                val mediaType = "application/json; charset=utf-8".toMediaType()
                val body = requestBody.toRequestBody(mediaType)

                // Make HTTP POST request to Auth0
                val request = Request.Builder()
                    .url("https://$AUTH0_DOMAIN/oauth/token")
                    .post(body)
                    .build()

                val response = httpClient.newCall(request).execute()
                val responseBody = response.body?.string() ?: ""

                if (response.isSuccessful) {
                    // Parse successful response directly (don't use Credentials class - it has wrong field order)
                    val json = JSONObject(responseBody)

                    Log.d(TAG, "Auth0 authentication successful")
                    Log.d(TAG, "Access token length: ${json.optString("access_token").length}")
                    Log.d(TAG, "ID token length: ${json.optString("id_token").length}")
                    Log.d(TAG, "Token type: ${json.optString("token_type", "Bearer")}")

                    // Switch to Main thread for Unity callback
                    scope.launch(Dispatchers.Main) {
                        sendSuccessToUnity(json)
                    }
                } else {
                    // Parse error response
                    Log.e(TAG, "Auth0 authentication failed: HTTP ${response.code}")
                    Log.e(TAG, "Response body: $responseBody")

                    val errorMessage = try {
                        val json = JSONObject(responseBody)
                        val error = json.optString("error", "unknown_error")
                        val description = json.optString("error_description", "Authentication failed")
                        "Authentication failed: $description ($error)"
                    } catch (e: Exception) {
                        "Authentication failed: HTTP ${response.code}"
                    }

                    // Switch to Main thread for Unity callback
                    scope.launch(Dispatchers.Main) {
                        sendErrorToUnity(errorMessage)
                    }
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error calling Auth0 API", e)
                // Switch to Main thread for Unity callback
                scope.launch(Dispatchers.Main) {
                    sendErrorToUnity("Network error: ${e.message}")
                }
            }
        }
    }

    /**
     * Send authentication success result to Unity
     *
     * Serializes tokens to JSON and calls Unity callback via UnitySendMessage.
     * Reads directly from Auth0 response JSON to avoid Credentials class parameter order bug.
     *
     * @param authResponse Auth0 /oauth/token response JSON
     */
    private fun sendSuccessToUnity(authResponse: JSONObject) {
        // Build JSON for Unity directly from Auth0 response
        // This bypasses the Auth0 SDK Credentials class which has incorrect parameter ordering
        val unityJson = JSONObject().apply {
            put("access_token", authResponse.optString("access_token", ""))
            put("id_token", authResponse.optString("id_token", ""))
            put("refresh_token", authResponse.optString("refresh_token", ""))
            put("token_type", authResponse.optString("token_type", "Bearer"))
            // Calculate expires_at from expires_in (Auth0 returns expiry in seconds)
            val expiresIn = authResponse.optInt("expires_in", 3600)
            put("expires_at", System.currentTimeMillis() + (expiresIn * 1000L))
            put("scope", authResponse.optString("scope", ""))
        }.toString()

        Log.d(TAG, "Sending success to Unity: ${UNITY_GAME_OBJECT}.${UNITY_SUCCESS_CALLBACK}")
        Log.d(TAG, "Unity JSON payload length: ${unityJson.length} characters")
        Log.d(TAG, "Unity JSON access_token length: ${authResponse.optString("access_token").length}")
        Log.d(TAG, "Unity JSON id_token length: ${authResponse.optString("id_token").length}")

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
