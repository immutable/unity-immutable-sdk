package com.immutable.unity;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.browser.customtabs.CustomTabsCallback;

public class ImmutableActivity extends Activity {
    private static final String EXTRA_URI = "extra_uri";
    private static final String EXTRA_INTENT_LAUNCHED = "extra_intent_launched";

    private static Callback callbackInstance;

    private boolean customTabsLaunched = false;
    private CustomTabsController customTabsController;

    public static void startActivity(Context context, String url, Callback callback) {
        callbackInstance = callback;

        Intent intent = new Intent(context, ImmutableActivity.class);
        intent.putExtra(EXTRA_URI, Uri.parse(url));
        intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
        context.startActivity(intent);
    }

    @Override
    protected void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (savedInstanceState != null) {
            customTabsLaunched = savedInstanceState.getBoolean(EXTRA_INTENT_LAUNCHED, false);
        }
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        Intent resultData = resultCode == RESULT_CANCELED ? new Intent() : data;
        onDeeplinkResult(resultData);
        finish();
    }

    @Override
    protected void onSaveInstanceState(@NonNull Bundle outState) {
        super.onSaveInstanceState(outState);
        outState.putBoolean(EXTRA_INTENT_LAUNCHED, customTabsLaunched);
    }

    @Override
    protected void onResume() {
        super.onResume();
        Intent authenticationIntent = getIntent();
        if (!customTabsLaunched && authenticationIntent.getExtras() == null) {
            // This activity was launched in an unexpected way
            finish();
            return;
        } else if (!customTabsLaunched) {
            // Haven't launched custom tabs
            customTabsLaunched = true;
            launchCustomTabs();
            return;
        }
        onDeeplinkResult(authenticationIntent);
        finish();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (customTabsController != null) {
            customTabsController.unbindService();
            customTabsController = null;
        }
    }

    private void launchCustomTabs() {
        Bundle extras = getIntent().getExtras();
        Uri uri;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            uri = extras.getParcelable(EXTRA_URI, Uri.class);
        } else {
            uri = extras.getParcelable(EXTRA_URI);
        }
        customTabsController = new CustomTabsController(this, new CustomTabsCallback() {
            @Override
            public void onNavigationEvent(int navigationEvent, @Nullable Bundle extras) {
                if (navigationEvent == CustomTabsCallback.TAB_HIDDEN && callbackInstance != null) {
                    callbackInstance.onCustomTabsDismissed(uri.toString());
                }
            }
        });
        customTabsController.bindService();
        customTabsController.launch(uri);
    }

    private void onDeeplinkResult(@Nullable Intent intent) {
        if (callbackInstance != null && intent != null && intent.getData() != null) {
            callbackInstance.onDeeplinkResult(intent.getData().toString());
        }
    }

    public interface Callback {
        void onCustomTabsDismissed(String url);
        void onDeeplinkResult(String url);
    }
}

