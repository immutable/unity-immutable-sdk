#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>

// Runtime dispatch (NSClassFromString + IMP-cast) is used throughout. The
// alternative — hard-importing AppTrackingTransparency.framework, AdSupport.framework,
// and StoreKit.framework — would force the Xcode project to link them and, in
// the case of StoreKit, trigger the In-App Purchase capability check on
// personal teams. Runtime lookup keeps the binary clean for studios that don't
// opt into attribution and avoids any compile-time framework dependency.

extern "C" {

const char* _AudienceGetIDFV(void)
{
    NSString *idfv = [[UIDevice currentDevice].identifierForVendor UUIDString];
    // strdup: IL2CPP calls free() on the returned pointer after copying into a
    // managed string. UTF8String is autoreleased (not malloc'd), so free() would
    // crash — strdup gives IL2CPP a heap-allocated copy it can safely free.
    return idfv ? strdup([idfv UTF8String]) : NULL;
}

void _AudienceRegisterSKAN(void)
{
    Class cls = NSClassFromString(@"SKAdNetwork");
    if (cls) {
        [cls performSelector:@selector(registerAppForAdNetworkAttribution)];
    }
}

// Matches C# delegate ATTBridge.NativeStatusCallback(int).
typedef void (*AudienceATTStatusCallback)(int status);

void _AudienceRequestATT(AudienceATTStatusCallback callback)
{
    if (!callback) return;

    Class cls = NSClassFromString(@"ATTrackingManager");
    SEL sel = NSSelectorFromString(@"requestTrackingAuthorizationWithCompletionHandler:");
    if (!cls || ![cls respondsToSelector:sel]) {
        // ATT unavailable (iOS < 14). Surface as notDetermined so callers can
        // proceed with a deterministic value rather than hanging.
        callback(0);
        return;
    }

    // Apple's completion handler may fire on a background thread. Cross back
    // into managed code on whatever thread we're given — IL2CPP tolerates
    // this and the C# side dispatches its own continuation.
    void (^handler)(NSUInteger) = ^(NSUInteger status) {
        callback((int)status);
    };

    // objc_msgSend with an explicit signature cast preserves block typing —
    // performSelector:withObject: would erase the block to id, and ARC's
    // retain rules for blocks-as-id are inconsistent across compilers.
    typedef void (*RequestFn)(id, SEL, void (^)(NSUInteger));
    RequestFn fn = (RequestFn)objc_msgSend;
    fn(cls, sel, handler);
}

int _AudienceGetATTStatus(void)
{
    Class cls = NSClassFromString(@"ATTrackingManager");
    SEL sel = NSSelectorFromString(@"trackingAuthorizationStatus");
    if (!cls || ![cls respondsToSelector:sel]) {
        return 0; // notDetermined fallback for iOS < 14.
    }

    // trackingAuthorizationStatus returns NSUInteger; performSelector returns
    // id, which would mis-marshal. Cast the IMP to a typed function pointer
    // so the value comes back without trip through NSInvocation.
    IMP imp = [cls methodForSelector:sel];
    NSUInteger (*fn)(id, SEL) = (NSUInteger (*)(id, SEL))imp;
    return (int)fn(cls, sel);
}

const char* _AudienceGetIDFA(void)
{
    Class cls = NSClassFromString(@"ASIdentifierManager");
    if (!cls) return NULL;

    id manager = [cls performSelector:@selector(sharedManager)];
    if (!manager) return NULL;

    NSUUID *uuid = [manager performSelector:@selector(advertisingIdentifier)];
    if (!uuid) return NULL;

    NSString *str = [uuid UUIDString];
    // Apple returns the all-zeros UUID when ATT is not authorized. Surfacing
    // it would pollute the dataset; treat as no-IDFA so the C# side can omit
    // the field entirely.
    if ([str isEqualToString:@"00000000-0000-0000-0000-000000000000"]) {
        return NULL;
    }

    return strdup([str UTF8String]);
}

}
