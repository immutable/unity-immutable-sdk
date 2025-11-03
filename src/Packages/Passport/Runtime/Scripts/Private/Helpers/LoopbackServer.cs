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
        private static LoopbackServer? _instance;
        private HttpListener? _httpListener;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _port;

        /// <summary>
        /// Initialises the loopback server for handling OAuth callbacks.
        /// The server will redirect to a deep link URI passed in the redirect_uri query parameter.
        /// </summary>
        /// <param name="port">The port to listen on (e.g. 2963)</param>
        public static void Initialise(int port)
        {
            if (_instance == null)
            {
                _instance = new GameObject(nameof(LoopbackServer)).AddComponent<LoopbackServer>();
                DontDestroyOnLoad(_instance.gameObject);
            }

            _instance._port = port;
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

                    // Send HTML response that redirects to the deep link
                    var response = context.Response;
                    var redirectHtml = @"<html><body>
<p>You have been successfully logged in. Redirecting you automatically...</p>
<p>If it doesn't redirect, <a href='#' id='redirectLink'>click here</a>.</p>
<script>
(function() {
    var urlParams = new URLSearchParams(window.location.search);
    var redirectUri = urlParams.get('redirect_uri');
    
    if (redirectUri) {
        // Remove redirect_uri from params to pass the rest to the deep link
        urlParams.delete('redirect_uri');
        
        // Build final redirect URL with remaining parameters
        var finalUrl = redirectUri;
        var remainingParams = urlParams.toString();
        if (remainingParams) {
            var separator = redirectUri.includes('?') ? '&' : '?';
            finalUrl = redirectUri + separator + remainingParams;
        }
        
        // Set up manual redirect link
        var redirectLink = document.getElementById('redirectLink');
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
</body></html>";
                    var buffer = Encoding.UTF8.GetBytes(redirectHtml);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 200;
                    
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                    response.OutputStream.Close();

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

