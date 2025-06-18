using UnityEngine;
using UnityEngine.UI;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ClearStorageAndCacheScript : MonoBehaviour
    {
        [SerializeField] private Text Output;

        public void ClearStorageAndCache()
        {
            if (Passport.Instance == null)
            {
                ShowOutput("Passport instance is null. Initialise Passport first.");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            ShowOutput("Clearing storage and cache...");
            Passport.Instance.ClearStorage();
            Passport.Instance.ClearCache(true);
            ShowOutput("Storage and cache cleared (on Android).");
#elif UNITY_IPHONE && !UNITY_EDITOR
            ShowOutput("Clearing storage and cache...");
            Passport.Instance.ClearStorage();
            Passport.Instance.ClearCache(true);
            ShowOutput("Storage and cache cleared (on iOS).");
#else
            ShowOutput("ClearStorageAndCache is only available on Android and iOS devices.");
#endif
        }

        private void ShowOutput(string message)
        {
            if (Output != null)
            {
                Output.text = message;
            }
        }
    }
}