#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2024 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using System.Diagnostics;

namespace VoltstroStudios.UnityWebBrowser.Core.Engines.Process
{
    internal interface IProcess : IDisposable
    {
        public void StartProcess(string executable, string workingDir, string arguments, DataReceivedEventHandler onLogEvent, DataReceivedEventHandler onErrorLogEvent);

        public void KillProcess();
        
        public bool HasExited { get; }
        
        public int ExitCode { get; }
    }
}

#endif