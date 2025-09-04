#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using UnityEngine;
using VoltstroStudios.UnityWebBrowser.Core;

namespace VoltstroStudios.UnityWebBrowser
{
    /// <summary>
    ///     Basic version of UWB. Lacks fullscreen controls
    /// </summary>
    [AddComponentMenu("UWB/Web Browser Basic")]
    [HelpURL("https://github.com/Voltstro-Studios/UnityWebBrowser")]
    public sealed class WebBrowserUIBasic : RawImageUwbClientInputHandler
    {
    }
}

#endif