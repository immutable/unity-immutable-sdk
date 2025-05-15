using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class UnauthenticatedSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject LoginButtons;
    [SerializeField] private GameObject ReloginButtons;
    [SerializeField] private InputField DeviceCodeTimeoutMs;

    private async void Start()
    {
        if (Passport.Instance != null)
        {
            bool hasCredsSaved = await Passport.Instance.HasCredentialsSaved();
            ReloginButtons.SetActive(hasCredsSaved);
            LoginButtons.SetActive(!hasCredsSaved);
            DeviceCodeTimeoutMs.gameObject.SetActive(!hasCredsSaved && !SampleAppManager.UsePKCE);
        }
        else
        {
            Debug.LogError("[UnauthenticatedSceneManager] Passport.Instance is null");
        }
    }
} 