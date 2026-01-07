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
    /// A lightweight HTTP server to serve the GameBridge index.html locally.
    /// This provides a proper origin (http://localhost:PORT) instead of null origin from file:// URLs.
    /// </summary>
    public class GameBridgeServer : IDisposable
    {
        private const string TAG = "[Game Bridge Server]";
        private const int MIN_PORT = 49152;
        private const int MAX_PORT = 65535;
        private const int MAX_PORT_ATTEMPTS = 100;

        private HttpListener? _listener;
        private Thread? _listenerThread;
        private byte[]? _indexHtmlContent;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        public int Port { get; private set; }

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

            // Cache the file content at startup
            _indexHtmlContent = File.ReadAllBytes(indexHtmlPath);
            Debug.Log($"{TAG} Loaded index.html ({_indexHtmlContent.Length} bytes)");
        }

        /// <summary>
        /// Starts the HTTP server on an available port.
        /// </summary>
        /// <returns>The URL to the index.html file.</returns>
        public string Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameBridgeServer));

            if (_listener?.IsListening == true)
                return $"http://localhost:{Port}/";

            Port = FindAvailablePort();
            
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            
            Debug.Log($"{TAG} Started on http://localhost:{Port}/");

            _listenerThread = new Thread(ListenerLoop)
            {
                Name = "GameBridgeServer",
                IsBackground = true
            };
            _listenerThread.Start();

            return $"http://localhost:{Port}/";
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

        private int FindAvailablePort()
        {
            var random = new System.Random();
            for (int attempt = 0; attempt < MAX_PORT_ATTEMPTS; attempt++)
            {
                int port = random.Next(MIN_PORT, MAX_PORT);
                if (IsPortAvailable(port))
                    return port;
            }
            throw new InvalidOperationException($"{TAG} Could not find an available port");
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
