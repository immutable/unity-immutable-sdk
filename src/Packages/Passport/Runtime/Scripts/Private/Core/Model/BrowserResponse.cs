using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    public class BrowserResponse
    {
        public string error;
        public string errorType;
        public string requestId;
        public string responseFor;
        public bool success;
    }

    public class StringResponse : BrowserResponse
    {
        public string result;
    }

    public class StringListResponse : BrowserResponse
    {
        public string[] result;
    }

    public class BoolResponse : BrowserResponse
    {
        public bool result;
    }

    public static class BrowserResponseExtensions
    {
        /// <summary>
        ///     Deserialises the json to StringResponse and returns the result
        ///     See <see cref="Immutable.Passport.Core.BrowserResponse.StringResponse"></param>
        /// </summary>
        public static string GetStringResult(this string json)
        {
            var stringResponse = json.OptDeserializeObject<StringResponse>();
            if (stringResponse != null)
                return stringResponse.result;
            return null;
        }

        /// <summary>
        ///     Deserialises the json to StringListResponse and returns the result
        ///     See <see cref="Immutable.Passport.Core.BrowserResponse.StringListResponse"></param>
        /// </summary>
        public static string[] GetStringListResult(this string json)
        {
            var stringResponse = json.OptDeserializeObject<StringListResponse>();
            if (stringResponse != null)
                return stringResponse.result;
            return null;
        }

        /// <summary>
        ///     Deserialises the json to BoolResponse and returns the result
        ///     See <see cref="Immutable.Passport.Core.BrowserResponse.BoolResponse"></param>
        /// </summary>
        public static bool? GetBoolResponse(this string json)
        {
            var boolResponse = json.OptDeserializeObject<BoolResponse>();
            if (boolResponse != null)
                return boolResponse.result;
            return null;
        }
    }
}