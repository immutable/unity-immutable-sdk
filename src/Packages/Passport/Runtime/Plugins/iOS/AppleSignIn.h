#ifndef AppleSignIn_h
#define AppleSignIn_h

#ifdef __cplusplus
extern "C" {
#endif

// Initialize Apple Sign In (called once)
void AppleSignIn_Init();

// Check if Apple Sign In is available on this device/OS
bool AppleSignIn_IsAvailable();

// Start the Apple Sign In flow
void AppleSignIn_Start();

// Callbacks (set from Unity C#)
typedef void (*AppleSignIn_OnSuccess)(const char* identityToken, const char* authorizationCode, const char* userID, const char* email, const char* fullName);
typedef void (*AppleSignIn_OnError)(const char* errorCode, const char* errorMessage);
typedef void (*AppleSignIn_OnCancel)();

void AppleSignIn_SetOnSuccessCallback(AppleSignIn_OnSuccess callback);
void AppleSignIn_SetOnErrorCallback(AppleSignIn_OnError callback);
void AppleSignIn_SetOnCancelCallback(AppleSignIn_OnCancel callback);

#ifdef __cplusplus
}
#endif

#endif /* AppleSignIn_h */

