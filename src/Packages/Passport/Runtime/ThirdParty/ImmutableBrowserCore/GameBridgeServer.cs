#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Immutable.Browser.Core
{
    /// <summary>
    /// Local HTTP server for index.html to provide a proper origin instead of null from file:// URLs.
    /// </summary>
    public class GameBridgeServer : IDisposable
    {
        private const string TAG = "[Game Bridge Server]";
        
        // Fixed port to maintain consistent origin for localStorage/IndexedDB persistence
        private const int PORT = 51990;
        private static readonly string URL = "http://localhost:" + PORT + "/";

        private HttpListener? _listener;
        private Thread? _listenerThread;
        private byte[]? _indexHtmlContent;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        /// <summary>
        /// Creates a new GameBridgeServer instance.
        /// </summary>
        /// <param name="indexHtmlPath">The file system path to the index.html file.</param>
        public GameBridgeServer(string indexHtmlPath)
        {
            if (string.IsNullOrEmpty(indexHtmlPath))
                throw new ArgumentNullException(nameof(indexHtmlPath));

            if (!File.Exists(indexHtmlPath))
                throw new FileNotFoundException($"{TAG} index.html not found: {indexHtmlPath}");

            _indexHtmlContent = File.ReadAllBytes(indexHtmlPath);
            Debug.Log($"{TAG} Loaded index.html ({_indexHtmlContent.Length} bytes)");
        }

        /// <summary>
        /// Starts the game bridge server.
        /// </summary>
        /// <returns>The URL to the index.html file.</returns>
        public string Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameBridgeServer));

            if (_listener?.IsListening == true)
                return URL;

            EnsurePortAvailable();
            
            _listener = new HttpListener();
            _listener.Prefixes.Add(URL);
            _listener.Start();
            
            Debug.Log($"{TAG} Started on {URL}");

            _listenerThread = new Thread(ListenerLoop)
            {
                Name = "GameBridgeServer",
                IsBackground = true
            };
            _listenerThread.Start();

            return URL;
        }

        private void ListenerLoop()
        {
            while (!_cts.Token.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var context = _listener.GetContext();
                    HandleRequest(context);
                }
                catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        Debug.LogError($"{TAG} Error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var response = context.Response;
            try
            {
                response.StatusCode = 200;
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = _indexHtmlContent!.Length;
                response.OutputStream.Write(_indexHtmlContent, 0, _indexHtmlContent.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{TAG} Error handling request: {ex.Message}");
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private void EnsurePortAvailable()
        {
            if (!IsPortAvailable(PORT))
            {
                throw new InvalidOperationException(
                    $"{TAG} Port {PORT} is already in use. " +
                    "Please close any application using this port to ensure localStorage/IndexedDB data persists correctly.");
            }
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
            
            _listenerThread?.Join(TimeSpan.FromSeconds(1));
            _cts.Dispose();
            _indexHtmlContent = null;
            
            Debug.Log($"{TAG} Stopped");
        }
    }
}

#endif
