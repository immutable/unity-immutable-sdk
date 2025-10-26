package com.immutable.unity;

import android.os.Bundle;
import android.util.Log;
import com.unity3d.player.UnityPlayerActivity;

/**
 * Custom Unity Activity for Immutable SDK
 * Extends UnityPlayerActivity to support Credential Manager native authentication
 */
public class ImmutableUnityActivity extends UnityPlayerActivity {
    private static final String TAG = "ImmutableUnityActivity";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Log.d(TAG, "ImmutableUnityActivity created");
    }
}
