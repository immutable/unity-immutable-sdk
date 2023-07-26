package com.immutable.authredirect;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;

public class RedirectActivity extends Activity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Intent intent = getIntent();
        if (intent != null && intent.getData() != null) {
            DeepLinkManager.callback.onDeepLink(intent.getData().toString());
        }
        finish();
    }
}
