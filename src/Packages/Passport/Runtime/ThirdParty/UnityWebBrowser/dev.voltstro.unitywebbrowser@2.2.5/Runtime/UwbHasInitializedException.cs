#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2024 Voltstro-Studios
// 
// This project is under the MIT license.See the LICENSE.md file for more details.

using System;

namespace VoltstroStudios.UnityWebBrowser
{
    /// <summary>
    ///     An <see cref="Exception"/> related to when trying to change something that cannot be changed when the
    ///     UWB has already initalized
    /// </summary>
    public sealed class UwbHasInitializedException : Exception
    {
        internal UwbHasInitializedException(string message) : base(message)
        {
        }
    }
}

#endif