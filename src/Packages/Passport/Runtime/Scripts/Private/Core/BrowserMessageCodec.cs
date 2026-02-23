using Immutable.Passport.Helpers;
using Immutable.Passport.Model;
using UnityEngine;

namespace Immutable.Passport.Core
{
    /// <summary>
    /// Handles serialization of outgoing requests and deserialization/validation
    /// of incoming responses for the game bridge.
    /// </summary>
    internal static class BrowserMessageCodec
    {
        /// <summary>
        /// Serializes a request into a JavaScript function call string.
        /// </summary>
        internal static string BuildJsCall(BrowserRequest request)
        {
            var escapedJson = JsonUtility.ToJson(request).Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"callFunction(\"{escapedJson}\")";
        }

        /// <summary>
        /// Deserializes and validates a raw game bridge response message.
        /// </summary>
        internal static BrowserResponse ParseAndValidateResponse(string message)
        {
            var response = message.OptDeserializeObject<BrowserResponse>();

            if (response == null || string.IsNullOrEmpty(response.responseFor) || string.IsNullOrEmpty(response.requestId))
            {
                throw new PassportException("Response from game bridge is incorrect. Check game bridge file.");
            }

            return response;
        }
    }
}
