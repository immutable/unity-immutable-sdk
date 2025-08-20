using UnityEngine;

namespace Immutable.Passport.UI
{
    /// <summary>
    /// Manages cursor lock state when the Passport UI is active.
    /// Automatically unlocks the cursor for UI interaction and restores the previous state when done.
    /// </summary>
    public class PassportUICursorManager : MonoBehaviour
    {
        [Header("Cursor Management Settings")]
        [Tooltip("Enable automatic cursor unlock when UI is active")]
        public bool enableCursorManagement = true;

        [Tooltip("Force cursor to stay unlocked while UI is active (overrides game cursor locks)")]
        public bool forceCursorUnlock = true;

        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private Canvas canvas;
        private bool hasStoredState = false;

        private void Start()
        {
            if (!enableCursorManagement) return;

            canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                StoreCursorState();
                SetCursorForUI(true);

                Debug.Log($"[PassportUICursorManager] Initialized - Lock: {previousCursorLockMode}, Visible: {previousCursorVisible}");
            }
        }

        private void Update()
        {
            if (!enableCursorManagement || !forceCursorUnlock) return;

            // Ensure cursor stays unlocked while UI is active
            if (canvas != null && canvas.gameObject.activeInHierarchy)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    SetCursorForUI(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (enableCursorManagement && hasStoredState)
            {
                RestoreCursorState();
            }
        }

        /// <summary>
        /// Call this when hiding/closing the UI
        /// </summary>
        public void OnUIHidden()
        {
            if (enableCursorManagement && hasStoredState)
            {
                RestoreCursorState();
            }
        }

        /// <summary>
        /// Call this when showing the UI
        /// </summary>
        public void OnUIShown()
        {
            if (enableCursorManagement)
            {
                if (!hasStoredState)
                {
                    StoreCursorState();
                }
                SetCursorForUI(true);
            }
        }

        /// <summary>
        /// Enable or disable cursor management at runtime
        /// </summary>
        public void SetCursorManagementEnabled(bool enabled)
        {
            enableCursorManagement = enabled;

            if (!enabled && hasStoredState)
            {
                RestoreCursorState();
            }
            else if (enabled && canvas != null && canvas.gameObject.activeInHierarchy)
            {
                if (!hasStoredState)
                {
                    StoreCursorState();
                }
                SetCursorForUI(true);
            }
        }

        private void StoreCursorState()
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            hasStoredState = true;
        }

        private void SetCursorForUI(bool enableForUI)
        {
            if (enableForUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("[PassportUICursorManager] Cursor unlocked for UI interaction");
            }
        }

        private void RestoreCursorState()
        {
            if (hasStoredState)
            {
                Cursor.lockState = previousCursorLockMode;
                Cursor.visible = previousCursorVisible;
                hasStoredState = false;
                Debug.Log($"[PassportUICursorManager] Restored cursor state - Lock: {previousCursorLockMode}, Visible: {previousCursorVisible}");
            }
        }

        /// <summary>
        /// Force unlock cursor (useful for debugging)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ForceUnlockCursor()
        {
            SetCursorForUI(true);
        }
    }
}
