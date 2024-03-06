#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;

namespace VoltstroStudios.UnityWebBrowser
{
    /// <summary>
    ///     An <see cref="Exception" /> related to when you are trying to change something that cannot be changed when the
    ///     engine is already running
    /// </summary>
    public sealed class UwbIsConnectedException : Exception
    {
        internal UwbIsConnectedException(string message)
            : base(message)
        {
        }
    }
}

#endif