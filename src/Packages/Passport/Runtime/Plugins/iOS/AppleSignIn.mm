#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AuthenticationServices/AuthenticationServices.h>
#import "AppleSignIn.h"

// Callbacks from Unity
static AppleSignIn_OnSuccess _onSuccessCallback = NULL;
static AppleSignIn_OnError _onErrorCallback = NULL;
static AppleSignIn_OnCancel _onCancelCallback = NULL;

// Forward declaration
extern UIViewController* UnityGetGLViewController();

// Delegate class for ASAuthorizationController
@interface AppleSignInDelegate : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@end

@implementation AppleSignInDelegate

// Called when authorization succeeds
- (void)authorizationController:(ASAuthorizationController *)controller 
   didCompleteWithAuthorization:(ASAuthorization *)authorization API_AVAILABLE(ios(13.0))
{
    NSLog(@"[AppleSignIn] ✅ Authorization completed successfully!");
    
    if ([authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]]) {
        ASAuthorizationAppleIDCredential *credential = (ASAuthorizationAppleIDCredential *)authorization.credential;
        
        // Extract user ID
        NSString *userID = credential.user ?: @"";
        NSLog(@"[AppleSignIn] User ID: %@", userID);
        
        // Extract email (may be empty on subsequent logins)
        NSString *email = credential.email ?: @"";
        if (email.length > 0) {
            NSLog(@"[AppleSignIn] Email: %@", email);
        } else {
            NSLog(@"[AppleSignIn] Email: (not provided - may be subsequent login)");
        }
        
        // Extract full name (may be empty on subsequent logins)
        NSString *fullName = @"";
        if (credential.fullName) {
            NSPersonNameComponents *name = credential.fullName;
            NSPersonNameComponentsFormatter *formatter = [[NSPersonNameComponentsFormatter alloc] init];
            formatter.style = NSPersonNameComponentsFormatterStyleDefault;
            fullName = [formatter stringFromPersonNameComponents:name] ?: @"";
            if (fullName.length > 0) {
                NSLog(@"[AppleSignIn] Full Name: %@", fullName);
            } else {
                NSLog(@"[AppleSignIn] Full Name: (not provided - may be subsequent login)");
            }
        }
        
        // Extract identity token (JWT)
        NSString *identityToken = @"";
        if (credential.identityToken) {
            identityToken = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding] ?: @"";
            NSLog(@"[AppleSignIn] Identity Token: %@...", [identityToken substringToIndex:MIN(50, identityToken.length)]);
        } else {
            NSLog(@"[AppleSignIn] ⚠️ Identity Token: (not provided)");
        }
        
        // Extract authorization code
        NSString *authorizationCode = @"";
        if (credential.authorizationCode) {
            authorizationCode = [[NSString alloc] initWithData:credential.authorizationCode encoding:NSUTF8StringEncoding] ?: @"";
            NSLog(@"[AppleSignIn] Authorization Code: %@...", [authorizationCode substringToIndex:MIN(20, authorizationCode.length)]);
        } else {
            NSLog(@"[AppleSignIn] ⚠️ Authorization Code: (not provided)");
        }
        
        // Call Unity success callback on main thread
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_onSuccessCallback) {
                NSLog(@"[AppleSignIn] Calling Unity success callback...");
                _onSuccessCallback(
                    [identityToken UTF8String],
                    [authorizationCode UTF8String],
                    [userID UTF8String],
                    [email UTF8String],
                    [fullName UTF8String]
                );
            } else {
                NSLog(@"[AppleSignIn] ⚠️ Success callback is NULL!");
            }
        });
    } else {
        NSLog(@"[AppleSignIn] ❌ Unexpected credential type");
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_onErrorCallback) {
                _onErrorCallback("UNEXPECTED_CREDENTIAL", "Received unexpected credential type");
            }
        });
    }
}

// Called when authorization fails
- (void)authorizationController:(ASAuthorizationController *)controller 
           didCompleteWithError:(NSError *)error API_AVAILABLE(ios(13.0))
{
    NSLog(@"[AppleSignIn] ❌ Authorization failed with error: %@", error.localizedDescription);
    NSLog(@"[AppleSignIn] Error code: %ld", (long)error.code);
    NSLog(@"[AppleSignIn] Error domain: %@", error.domain);
    
    NSString *errorCode = [NSString stringWithFormat:@"%ld", (long)error.code];
    NSString *errorMessage = error.localizedDescription ?: @"Unknown error";
    
    // Check if user canceled (error code 1001)
    if (error.code == ASAuthorizationErrorCanceled) {
        NSLog(@"[AppleSignIn] User cancelled Apple Sign In");
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_onCancelCallback) {
                _onCancelCallback();
            }
        });
    } else {
        // Other error
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_onErrorCallback) {
                _onErrorCallback([errorCode UTF8String], [errorMessage UTF8String]);
            }
        });
    }
}

// Provide the window for presenting the authorization UI
- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller API_AVAILABLE(ios(13.0))
{
    // Get Unity's view controller and return its window
    UIViewController *unityViewController = UnityGetGLViewController();
    if (unityViewController && unityViewController.view.window) {
        NSLog(@"[AppleSignIn] Using Unity view controller's window for presentation");
        return unityViewController.view.window;
    }
    
    // Fallback: try to get key window
    if (@available(iOS 13.0, *)) {
        for (UIWindowScene *scene in [UIApplication sharedApplication].connectedScenes) {
            if (scene.activationState == UISceneActivationStateForegroundActive) {
                for (UIWindow *window in scene.windows) {
                    if (window.isKeyWindow) {
                        NSLog(@"[AppleSignIn] Using key window for presentation");
                        return window;
                    }
                }
            }
        }
    }
    
    // Final fallback
    NSLog(@"[AppleSignIn] ⚠️ Using fallback window for presentation");
    return [UIApplication sharedApplication].windows.firstObject;
}

@end

// Singleton delegate instance
static AppleSignInDelegate *_delegate = nil;

// Initialize
void AppleSignIn_Init()
{
    NSLog(@"[AppleSignIn] Init called - initializing Apple Sign In plugin ✅");
    
    if (_delegate == nil) {
        _delegate = [[AppleSignInDelegate alloc] init];
        NSLog(@"[AppleSignIn] Delegate created");
    }
}

// Check if available
bool AppleSignIn_IsAvailable()
{
    if (@available(iOS 13.0, *)) {
        NSLog(@"[AppleSignIn] iOS 13.0+ detected - Apple Sign In is available ✅");
        return true;
    } else {
        NSLog(@"[AppleSignIn] iOS version too old - Apple Sign In requires iOS 13.0+ ❌");
        return false;
    }
}

// Start Apple Sign In
void AppleSignIn_Start()
{
    NSLog(@"[AppleSignIn] Start called - beginning Apple Sign In flow...");
    
    if (@available(iOS 13.0, *)) {
        // Ensure delegate is initialized
        if (_delegate == nil) {
            NSLog(@"[AppleSignIn] Delegate not initialized, calling Init...");
            AppleSignIn_Init();
        }
        
        // Create Apple ID provider
        ASAuthorizationAppleIDProvider *provider = [[ASAuthorizationAppleIDProvider alloc] init];
        ASAuthorizationAppleIDRequest *request = [provider createRequest];
        
        // Request email and full name scopes
        request.requestedScopes = @[ASAuthorizationScopeEmail, ASAuthorizationScopeFullName];
        NSLog(@"[AppleSignIn] Requesting scopes: email, fullName");
        
        // Create authorization controller
        ASAuthorizationController *controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        controller.delegate = _delegate;
        controller.presentationContextProvider = _delegate;
        
        // Perform the authorization request
        NSLog(@"[AppleSignIn] Presenting Apple Sign In UI...");
        [controller performRequests];
    } else {
        NSLog(@"[AppleSignIn] ❌ Apple Sign In requires iOS 13.0 or later");
        if (_onErrorCallback) {
            _onErrorCallback("UNSUPPORTED", "Apple Sign In requires iOS 13.0 or later");
        }
    }
}

// Set callbacks
void AppleSignIn_SetOnSuccessCallback(AppleSignIn_OnSuccess callback)
{
    NSLog(@"[AppleSignIn] Success callback registered");
    _onSuccessCallback = callback;
}

void AppleSignIn_SetOnErrorCallback(AppleSignIn_OnError callback)
{
    NSLog(@"[AppleSignIn] Error callback registered");
    _onErrorCallback = callback;
}

void AppleSignIn_SetOnCancelCallback(AppleSignIn_OnCancel callback)
{
    NSLog(@"[AppleSignIn] Cancel callback registered");
    _onCancelCallback = callback;
}

/*
 * SLICE 3 IMPLEMENTATION COMPLETE:
 * 
 * This native plugin now implements full Apple Sign In functionality:
 * ✅ Real ASAuthorizationController integration
 * ✅ Shows native Apple Sign In UI sheet
 * ✅ Handles Face ID / Touch ID / Passcode authentication
 * ✅ Requests email and fullName scopes
 * ✅ Returns real identity tokens (JWT) from Apple
 * ✅ Returns authorization code for server-to-server validation
 * ✅ Returns user ID, email, and full name
 * ✅ Handles user cancellation (error code 1001)
 * ✅ Handles other errors with proper error messages
 * ✅ Properly presents UI using Unity's view controller
 * 
 * IMPORTANT NOTES:
 * - Email and full name are only provided on FIRST sign-in
 * - Subsequent sign-ins will return empty strings for email/fullName
 * - Backend must store this data from the first login
 * - User ID (sub claim) is the stable identifier
 * - Identity token is a JWT that must be verified by backend
 * 
 * NEXT STEP (Slice 4):
 * - Implement backend token exchange endpoint
 * - Verify JWT with Apple's public keys
 * - Create/update user account
 * - Return Passport session tokens
 */

