using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Immutable.Passport.Core.Logging;

#nullable enable
namespace Immutable.Passport.Helpers
{
    /// <summary>
    /// A simple HTTP loopback server for handling OAuth redirects locally.
    /// This allows the OAuth flow to redirect to http://localhost:port instead of a custom deep link protocol.
    /// </summary>
    public class LoopbackServer : MonoBehaviour
    {
        private const string SUCCESS_HTML = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Authentication Successful</title>
</head>
<body>
    <p>Authentication successful. You can close this window.</p>
    <script>
        // Attempt to close the window automatically
        window.close();
        
        // If that doesn't work (browser security may prevent it), try alternative methods
        setTimeout(function() {
            window.open('', '_self').close();
        }, 100);
    </script>
</body>
</html>";

        private static LoopbackServer? _instance;
        private HttpListener? _httpListener;
        private Action<string>? _callback;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _port;

        // Windows P/Invoke declarations for window focusing
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        /// <summary>
        /// Initialises the loopback server for handling OAuth callbacks.
        /// </summary>
        /// <param name="port">The port to listen on (e.g. 2963)</param>
        /// <param name="callback">Callback to invoke when a redirect is received with the full URL</param>
        public static void Initialise(int port, Action<string> callback)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(LoopbackServer)).AddComponent<LoopbackServer>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            _instance._port = port;
            _instance._callback = callback;
            _instance.StartServer();
        }

        /// <summary>
        /// Stops the loopback server and cleans up resources.
        /// </summary>
        public static void Stop()
        {
            if (_instance != null)
            {
                _instance.StopServer();
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void StartServer()
        {
            if (_httpListener != null && _httpListener.IsListening)
            {
                PassportLogger.Warn("Loopback server is already running");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                _httpListener.Start();
                PassportLogger.Debug($"Loopback server started on http://localhost:{_port}/");

                // Start listening for requests asynchronously
                _ = ListenForRequests(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"Failed to start loopback server: {ex.Message}");
            }
        }

        private async UniTaskVoid ListenForRequests(CancellationToken cancellationToken)
        {
            try
            {
                while (_httpListener != null && _httpListener.IsListening && !cancellationToken.IsCancellationRequested)
                {
                    // Wait for an incoming request
                    var context = await _httpListener.GetContextAsync();
                    
                    // Get the full URL including query parameters
                    var url = context.Request.Url?.ToString();
                    PassportLogger.Debug($"Loopback server received request: {url}");

                    // Send the HTML response
                    var response = context.Response;
                    var buffer = Encoding.UTF8.GetBytes(SUCCESS_HTML);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 200;
                    
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                    response.OutputStream.Close();

                    // Invoke the callback with the URL
                    if (!string.IsNullOrEmpty(url))
                    {
                        // Focus the Unity window to bring user back to the game
                        FocusUnityWindow();
                        
                        _callback?.Invoke(url);
                    }

                    // Stop the server after handling the request
                    StopServer();
                    break;
                }
            }
            catch (HttpListenerException ex)
            {
                // This is expected when the listener is stopped
                if (ex.ErrorCode != 995) // ERROR_OPERATION_ABORTED
                {
                    PassportLogger.Error($"Loopback server error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Error($"Loopback server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Focuses the Unity application window to bring it to the foreground.
        /// </summary>
        private void FocusUnityWindow()
        {
            try
            {
                // Get the current process
                var currentProcess = Process.GetCurrentProcess();
                IntPtr windowHandle = currentProcess.MainWindowHandle;

                if (windowHandle != IntPtr.Zero)
                {
                    // If window is minimised, restore it first
                    if (IsIconic(windowHandle))
                    {
                        ShowWindow(windowHandle, SW_RESTORE);
                    }

                    // Bring window to foreground
                    SetForegroundWindow(windowHandle);
                    PassportLogger.Debug("Unity window brought to focus");
                }
                else
                {
                    PassportLogger.Warn("Could not get Unity window handle");
                }
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"Error focusing Unity window: {ex.Message}");
            }
        }

        private void StopServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_httpListener != null)
                {
                    if (_httpListener.IsListening)
                    {
                        _httpListener.Stop();
                    }
                    _httpListener.Close();
                    _httpListener = null;
                    PassportLogger.Debug("Loopback server stopped");
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                PassportLogger.Warn($"Error stopping loopback server: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            StopServer();
        }
    }
}

