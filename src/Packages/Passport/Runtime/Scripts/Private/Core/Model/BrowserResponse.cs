using UnityEngine;
using Immutable.Passport.Json;

namespace Immutable.Passport.Core
{
    public class BrowserResponse
    {
        public string responseFor;
        public string requestId;
        public bool success;
        public string errorType;
        public string error;
    }

    public class StringResponse : BrowserResponse
    {
        public string result;
    }

    public static class BrowserResponseExtensions
    {
        /// <summary>
        /// Deserialises the json to StringResponse and returns the Result
        /// See <see cref="Immutable.Passport.Core.BrowserResponse.StringResponse"></param>
        /// </summary>
        public static string GetStringResult(this string json)
        {
            StringResponse stringResponse = json.OptDeserializeObject<StringResponse>();
            if (stringResponse != null)
            {
                return stringResponse.result;
            }
            else
            {
                return null;
            }
        }
    }
}
