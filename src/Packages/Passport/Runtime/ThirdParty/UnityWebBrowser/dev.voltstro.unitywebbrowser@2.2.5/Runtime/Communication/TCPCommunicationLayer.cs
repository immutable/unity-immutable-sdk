#if !IMMUTABLE_CUSTOM_BROWSER && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN))

// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System.Net;
using UnityEngine;
using VoltRpc.Communication;
using VoltRpc.Communication.TCP;

namespace VoltstroStudios.UnityWebBrowser.Communication
{
    /// <summary>
    ///     In-Built TCP layer that uses VoltRpc's <see cref="TCPClient" /> and <see cref="TCPHost" />
    /// </summary>
    [CreateAssetMenu(fileName = "TCP Communication Layer", menuName = "UWB/TCP Communication Layer")]
    public sealed class TCPCommunicationLayer : CommunicationLayer
    {
        /// <summary>
        ///     The in port to communicate on
        /// </summary>
        [Header("TCP Settings")] [Range(1024, 65353)] [Tooltip("The in port to communicate on")]
        public int inPort = 5555;

        /// <summary>
        ///     The out port to communicate on
        /// </summary>
        [Range(1024, 65353)] [Tooltip("The out port to communicate on")]
        public int outPort = 5556;

        public override Client CreateClient()
        {
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, inPort);
            return new TCPClient(ipEndPoint, int.MaxValue);
        }

        public override Host CreateHost()
        {
            IPEndPoint ipEndPoint = new(IPAddress.Loopback, outPort);
            return new TCPHost(ipEndPoint, int.MaxValue, int.MaxValue);
        }

        public override void GetIpcSettings(out object outLocation, out object inLocation, out string layerName)
        {
            outLocation = outPort;
            inLocation = inPort;
            layerName = "TCP";
        }
    }
}

#endif