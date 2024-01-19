package com.immutable.unity;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.content.pm.ResolveInfo;
import android.graphics.Insets;
import android.net.Uri;
import android.os.Build;
import android.util.DisplayMetrics;
import android.view.WindowInsets;
import android.view.WindowMetrics;

import androidx.annotation.NonNull;
import androidx.browser.customtabs.CustomTabsCallback;
import androidx.browser.customtabs.CustomTabsClient;
import androidx.browser.customtabs.CustomTabsIntent;
import androidx.browser.customtabs.CustomTabsService;
import androidx.browser.customtabs.CustomTabsServiceConnection;
import androidx.browser.customtabs.CustomTabsSession;

import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicReference;

public class CustomTabsController extends CustomTabsServiceConnection {
    private static final long MAX_WAIT_TIME_SECONDS = 1;

    private final WeakReference<Activity> context;
    private final AtomicReference<CustomTabsSession> session;
    private final CountDownLatch sessionLatch;
    private final String preferredPackage;
    private final CustomTabsCallback callback;

    private boolean didTryToBindService;

    public CustomTabsController(@NonNull Activity context, CustomTabsCallback callback) {
        this.context = new WeakReference<>(context);
        this.session = new AtomicReference<>();
        this.sessionLatch = new CountDownLatch(1);
        this.callback = callback;
        this.preferredPackage = getPreferredCustomTabsPackage(context);
    }

    // Get all apps that can support Custom Tabs Service
    // i.e. services that can handle ACTION_CUSTOM_TABS_CONNECTION intents
    private String getPreferredCustomTabsPackage(@NonNull Activity context) {
        PackageManager packageManager = context.getPackageManager();
        Intent serviceIntent = new Intent();
        serviceIntent.setAction(CustomTabsService.ACTION_CUSTOM_TABS_CONNECTION);
        List<ResolveInfo> resolvedList = packageManager.queryIntentServices(serviceIntent, 0);
        List<String> packageNames = new ArrayList<>();
        for (ResolveInfo info : resolvedList) {
            if (info.serviceInfo != null) {
                packageNames.add(info.serviceInfo.packageName);
            }
        }
        if (packageNames.size() > 0) {
            // Get the preferred Custom Tabs package
            return CustomTabsClient.getPackageName(context, packageNames);
        } else {
            return null;
        }
    }

    @Override
    public void onCustomTabsServiceConnected(@NonNull ComponentName componentName, @NonNull CustomTabsClient client) {
        client.warmup(0L);
        CustomTabsSession customTabsSession = client.newSession(callback);
        session.set(customTabsSession);
        sessionLatch.countDown();
    }

    @Override
    public void onServiceDisconnected(ComponentName componentName) {
        session.set(null);
    }

    public void bindService() {
        Context context = this.context.get();
        didTryToBindService = false;
        if (context != null && preferredPackage != null) {
            didTryToBindService = true;
            CustomTabsClient.bindCustomTabsService(context, preferredPackage, this);
        }
    }

    public void unbindService() {
        Context context = this.context.get();
        if (didTryToBindService && context != null) {
            context.unbindService(this);
            didTryToBindService = false;
        }
    }

    public void launch(@NonNull final Uri uri) {
        final Activity context = this.context.get();
        if (context == null) {
            // Custom tabs context is no longer valid
            return;
        }

        if (preferredPackage == null) {
            // Could not get the preferred Custom Tab browser, so launch URL in any browser
            context.startActivity(new Intent(Intent.ACTION_VIEW, uri));
        } else {
            // Running in a different thread to prevent doing too much work on main thread
            new Thread(() -> {
                try {
                    launchCustomTabs(context, uri);
                } catch (ActivityNotFoundException ex) {
                    // Failed to launch Custom Tab browser, so launch in browser
                    context.startActivity(new Intent(Intent.ACTION_VIEW, uri));
                }
            }).start();
        }
    }

    private void launchCustomTabs(Activity context, Uri uri) {
        bindService();
        try {
            boolean ignored = sessionLatch.await(MAX_WAIT_TIME_SECONDS, TimeUnit.SECONDS);
        } catch (InterruptedException ignored) {
        }

        final CustomTabsIntent.Builder builder = new CustomTabsIntent.Builder(session.get())
                .setInitialActivityHeightPx(getCustomTabsHeight(context))
                .setShareState(CustomTabsIntent.SHARE_STATE_OFF);
        final Intent intent = builder.build().intent;
        intent.setData(uri);
        context.startActivity(intent);
    }

    private int getCustomTabsHeight(Activity context) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            WindowMetrics windowMetrics = context.getWindowManager().getCurrentWindowMetrics();
            Insets insets = windowMetrics.getWindowInsets()
                    .getInsetsIgnoringVisibility(WindowInsets.Type.systemBars());
            return windowMetrics.getBounds().height() - insets.top - insets.bottom;
        } else {
            DisplayMetrics displayMetrics = new DisplayMetrics();
            context.getWindowManager().getDefaultDisplay().getMetrics(displayMetrics);
            return displayMetrics.heightPixels;
        }
    }

}
