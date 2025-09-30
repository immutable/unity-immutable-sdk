#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2024 Voltstro-Studios
// 
// This project is under the MIT license.See the LICENSE.md file for more details.

using System.Diagnostics;
using VoltstroStudios.UnityWebBrowser.Helper;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

namespace VoltstroStudios.UnityWebBrowser.Core.Engines.Process
{
    internal sealed class MacOsProcess : IProcess
    {
        private readonly System.Diagnostics.Process process;
        
        public MacOsProcess()
        {
            process = new System.Diagnostics.Process();
        }

        public void StartProcess(string executable, string workingDir, string arguments, DataReceivedEventHandler onLogEvent,
            DataReceivedEventHandler onErrorLogEvent)
        {
            try
            {
                ProcessStartInfo startInfo = new(executable, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDir
                };

                process.StartInfo = startInfo;
                process.OutputDataReceived += onLogEvent;
                process.ErrorDataReceived += onErrorLogEvent;
                
                UnityEngine.Debug.Log($"[MacOsProcess] 🚀 Starting process: {executable}");
                UnityEngine.Debug.Log($"[MacOsProcess] 📁 Working directory: {workingDir}");
                UnityEngine.Debug.Log($"[MacOsProcess] ⚙️ Arguments: {arguments}");
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                UnityEngine.Debug.Log($"[MacOsProcess] ✅ Process started successfully with PID: {process.Id}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[MacOsProcess] ❌ Failed to start process: {ex.Message}");
                UnityEngine.Debug.LogError($"[MacOsProcess] 📍 Executable: {executable}");
                UnityEngine.Debug.LogError($"[MacOsProcess] 📍 Working Dir: {workingDir}");
                throw;
            }
        }

        public void KillProcess()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.KillTree();
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[MacOsProcess] ⚠️ Error killing process: {ex.Message}");
            }
        }

        public bool HasExited => process?.HasExited ?? true;
        public int ExitCode => process?.ExitCode ?? -1;
        
        public void Dispose()
        {
            try
            {
                process?.Dispose();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[MacOsProcess] ⚠️ Error disposing process: {ex.Message}");
            }
        }
    }
}

#endif


#endif