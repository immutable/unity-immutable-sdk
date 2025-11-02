using System;
using System.Net;
using System.Text;
using System.Threading;
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
    <title>Logged In</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: #000000;
        }
        .container {
            background: white;
            padding: 3rem;
            border-radius: 1rem;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
            max-width: 400px;
        }
        h1 {
            color: #333;
            margin: 0 0 1rem 0;
            font-size: 1.75rem;
        }
        p {
            color: #666;
            margin: 0 0 1rem 0;
            font-size: 1.1rem;
        }
        .success-icon {
            font-size: 4rem;
            margin-bottom: 1rem;
        }
        a {
            color: #667eea;
            text-decoration: none;
            font-weight: 500;
        }
        a:hover {
            text-decoration: underline;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'>✓</div>
        <h1>Logged In</h1>
        <p>You have been successfully logged in. Redirecting you automatically...</p>
        <p>If it doesn't redirect, <a href='#' id='redirectLink'>click here</a>.</p>
    </div>
    <script>
        (function() {
            // Parse URL parameters
            const urlParams = new URLSearchParams(window.location.search);
            const redirectUri = urlParams.get('redirect_uri');
            
            if (redirectUri) {
                // Remove redirect_uri from params to pass the rest to the deep link
                urlParams.delete('redirect_uri');
                
                // Build final redirect URL with remaining parameters
                let finalUrl = redirectUri;
                const remainingParams = urlParams.toString();
                if (remainingParams) {
                    // Check if redirect_uri already has query params
                    const separator = redirectUri.includes('?') ? '&' : '?';
                    finalUrl = redirectUri + separator + remainingParams;
                }
                
                // Set up manual redirect link
                const redirectLink = document.getElementById('redirectLink');
                redirectLink.href = finalUrl;
                redirectLink.onclick = function(e) {
                    e.preventDefault();
                    window.location.href = finalUrl;
                };
                
                // Automatic redirect after 1 second
                setTimeout(function() {
                    window.location.href = finalUrl;
                }, 1000);
            }
        })();
    </script>
</body>
</html>";

        private static LoopbackServer? _instance;
        private HttpListener? _httpListener;
        private Action<string>? _callback;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _port;

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

