using UnityEngine;
using Immutable.Passport;
using System;

public class UnauthenticatedSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject LoginButtons;
    [SerializeField] private GameObject ReloginButtons;

    private async void Start()
    {
        if (Passport.Instance != null)
        {
            var hasCredentialsSaved = await Passport.Instance.HasCredentialsSaved();
            ReloginButtons.SetActive(hasCredentialsSaved);
            LoginButtons.SetActive(!hasCredentialsSaved);
        }
        else
        {
            Debug.LogError("[UnauthenticatedSceneManager] Passport.Instance is null");
        }
    }
}