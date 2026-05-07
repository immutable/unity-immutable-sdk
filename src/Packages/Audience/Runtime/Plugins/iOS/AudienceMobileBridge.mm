#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

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
    // Runtime dispatch avoids a hard link to StoreKit.framework, which would
    // trigger Xcode's In-App Purchase capability check. StoreKit is always
    // present on device; NSClassFromString finds it without a compile-time dep.
    Class cls = NSClassFromString(@"SKAdNetwork");
    if (cls) {
        [cls performSelector:@selector(registerAppForAdNetworkAttribution)];
    }
}

}
