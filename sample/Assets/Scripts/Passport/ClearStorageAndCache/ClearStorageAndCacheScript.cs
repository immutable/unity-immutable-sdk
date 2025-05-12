using UnityEngine;
using UnityEngine.UI;

namespace Immutable.Passport.Sample.PassportFeatures
{
    public class ClearStorageAndCacheScript : MonoBehaviour
    {
        [SerializeField] private Text Output;

        public void ClearStorageAndCache()
        {
#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
            Passport.ClearStorage();
            Passport.ClearCache(true);
            ShowOutput("Cleared storage and cache");
#else
            ShowOutput("Support on Android and iOS devices only");
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