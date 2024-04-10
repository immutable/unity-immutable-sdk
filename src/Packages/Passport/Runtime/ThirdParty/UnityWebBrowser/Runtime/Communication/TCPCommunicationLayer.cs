#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;
using VoltRpc.Communication;
using VoltRpc.Communication.TCP;

namespace VoltstroStudios.UnityWebBrowser.Communication
{
    /// <summary>
    ///     In-Built TCP layer that uses VoltRpc's <see cref="TCPClient" /> and <see cref="TCPHost" />
    /// </summary>
    public sealed class TCPCommunicationLayer : CommunicationLayer
    {
        /// <summary>
        ///     The in port to communicate on
        /// </summary>
        [Header("TCP Settings")]
        [Range(1024, 65353)]
        [Tooltip("The in port to communicate on")]
        public int inPort = 5555;

        /// <summary>
        ///     The out port to communicate on
        /// </summary>
        [Range(1024, 65353)]
        [Tooltip("The out port to communicate on")]
        public int outPort = 5556;

        public TCPCommunicationLayer()
        {
            // If ports are not available, use different ports
            System.Random rnd = new System.Random();
            while (!CheckAvailableServerPort(inPort) || !CheckAvailableServerPort(outPort))
            {
                int port = rnd.Next(1024, 65353);
                inPort = port;
                outPort = port + 1;
            }
        }

        public override Client CreateClient()
        {
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, inPort);
            return new TCPClient(ipEndPoint, connectionTimeout);
        }

        public override Host CreateHost()
        {
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, outPort);
            return new TCPHost(ipEndPoint);
        }

        public override void GetIpcSettings(out object outLocation, out object inLocation, out string assemblyLocation)
        {
            outLocation = outPort;
            inLocation = inPort;
            assemblyLocation = null;
        }

        private bool CheckAvailableServerPort(int port) {
            Debug.Log($"Checking Port {port}");
            bool isAvailable = true;

            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray) {
                if (endpoint.Port == port) {
                    isAvailable = false;
                    break;
                }
            }

            Debug.Log($"Port {port} available = {isAvailable}");

            return isAvailable;
        }
    }
}

#endif