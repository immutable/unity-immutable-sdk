using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SocialLoginButton : MonoBehaviour
{
    
    public enum Provider
    {
        Google,
        Apple,
        Facebook
    }
    
    [Header("Select the social provider")]
    public Provider provider;
    
    [Header("Reference to the icon Image on the button")]
    public Image iconImage;

    [Header("Sprites for providers")]
    public Sprite googleSprite;
    public Sprite appleSprite;
    public Sprite facebookSprite;

    private void OnValidate()
    {
        UpdateIcon();
    }

    private void Awake()
    {
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;

        switch (provider)
        {
            case Provider.Google:
                iconImage.sprite = googleSprite;
                break;
            case Provider.Apple:
                iconImage.sprite = appleSprite;
                break;
            case Provider.Facebook:
                iconImage.sprite = facebookSprite;
                break;
        }
    }
}
