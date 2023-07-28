package com.immutable.authredirect;

public class DeepLinkManager {
    protected static DeepLinkCallback callback = null;

    public static void setCallback(DeepLinkCallback cb) {
        callback = cb;
    }

    void clearCallback() {
        callback = null;
    }
}

interface DeepLinkCallback {
    void onDeepLink(String uri);
}