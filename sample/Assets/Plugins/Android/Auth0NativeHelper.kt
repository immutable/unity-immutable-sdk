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

// Auth0 SDK imports
import com.auth0.android.Auth0
import com.auth0.android.authentication.AuthenticationAPIClient
import com.auth0.android.authentication.AuthenticationException
import com.auth0.android.callback.Callback
import com.auth0.android.result.Credentials

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
        // IMPORTANT: Use WEB Client ID (not Android Client ID) for serverClientId
        // This must match the "Allowed Mobile Client IDs" in Auth0 Google connection
        private const val GOOGLE_WEB_CLIENT_ID = "182709567437-juu00150qf2mfcmi833m3lvajsabjngv.apps.googleusercontent.com"

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
    private val auth0 = Auth0(AUTH0_CLIENT_ID, AUTH0_DOMAIN)
    private val authClient = AuthenticationAPIClient(auth0)

    /**
     * Launch native Google account picker and authenticate with Auth0
     *
     * This is the main entry point called from Unity.
     * Flow is asynchronous - result returned via Unity callbacks.
     */
    @JvmStatic
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

        // Configure Google ID credential request
        val googleIdOption = GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)  // Show all Google accounts on device
            .setServerClientId(GOOGLE_WEB_CLIENT_ID)  // Web Client ID for backend validation
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
     * Auth0 SDK handles:
     * - JWT signature verification
     * - Token audience validation
     * - User creation/lookup
     * - Running Auth0 Actions (custom logic)
     * - Token issuance
     *
     * @param googleIdToken Google ID token (JWT)
     */
    private fun authenticateWithAuth0(googleIdToken: String) {
        Log.d(TAG, "Calling Auth0.loginWithNativeSocialToken...")

        authClient
            .loginWithNativeSocialToken(googleIdToken, "google")
            .start(object : Callback<Credentials, AuthenticationException> {
                override fun onSuccess(credentials: Credentials) {
                    Log.d(TAG, "Auth0 authentication successful")
                    Log.d(TAG, "Access token received, expires in: ${credentials.expiresIn}")

                    sendSuccessToUnity(credentials)
                }

                override fun onFailure(error: AuthenticationException) {
                    Log.e(TAG, "Auth0 authentication failed", error)
                    Log.e(TAG, "Error code: ${error.getCode()}, Description: ${error.getDescription()}")

                    // Provide user-friendly error messages
                    val errorMessage = when {
                        error.isBrowserAppNotAvailable ->
                            "Browser not available on device"

                        error.isNetworkError ->
                            "Network error. Please check your internet connection."

                        error.isAuthenticationError -> {
                            val description = error.getDescription()
                            when {
                                description.contains("banned", ignoreCase = true) ||
                                description.contains("suspended", ignoreCase = true) ->
                                    "Account suspended: ${description}"

                                description.contains("audience", ignoreCase = true) ->
                                    "Configuration error. Please contact support."

                                else ->
                                    "Authentication failed: ${description}"
                            }
                        }

                        else ->
                            "Login failed: ${error.message ?: "Unknown error"}"
                    }

                    sendErrorToUnity(errorMessage)
                }
            })
    }

    /**
     * Send authentication success result to Unity
     *
     * Serializes tokens to JSON and calls Unity callback via UnitySendMessage.
     *
     * @param credentials Auth0 credentials (access token, ID token, refresh token)
     */
    private fun sendSuccessToUnity(credentials: Credentials) {
        val json = """
            {
                "access_token": "${credentials.accessToken}",
                "id_token": "${credentials.idToken}",
                "refresh_token": "${credentials.refreshToken ?: ""}",
                "token_type": "${credentials.type}",
                "expires_at": ${credentials.expiresAt.time},
                "scope": "${credentials.scope ?: ""}"
            }
        """.trimIndent()

        Log.d(TAG, "Sending success to Unity: ${UNITY_GAME_OBJECT}.${UNITY_SUCCESS_CALLBACK}")

        try {
            UnityPlayer.UnitySendMessage(
                UNITY_GAME_OBJECT,
                UNITY_SUCCESS_CALLBACK,
                json
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
