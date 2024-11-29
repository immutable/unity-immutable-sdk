#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using VoltstroStudios.UnityWebBrowser.Core.Popups;

namespace VoltstroStudios.UnityWebBrowser.Events
{
    public delegate void OnPopup(WebBrowserPopupInfo popupInfo);
}

#endif