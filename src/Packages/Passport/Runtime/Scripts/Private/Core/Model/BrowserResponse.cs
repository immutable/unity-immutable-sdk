using Immutable.Passport.Helpers;

namespace Immutable.Passport.Core
{
    public class BrowserResponse
    {
        public string responseFor;
        public string requestId;
        public bool success;
        public string errorType;
        public string? error;
    }

    public class StringResponse : BrowserResponse
    {
        public string? result;
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
        /// Deserialises the json to StringResponse and returns the result
        /// See <see cref="Immutable.Passport.Core.StringResponse" />
        /// </summary>
        public static string? GetStringResult(this string json)
        {
            var stringResponse = json.OptDeserializeObject<StringResponse>();
            return stringResponse?.result;
        }

        /// <summary>
        /// Deserialises the json to StringListResponse and returns the result
        /// See <see cref="Immutable.Passport.Core.StringListResponse" />
        /// </summary>
        public static string[]? GetStringListResult(this string json)
        {
            var stringResponse = json.OptDeserializeObject<StringListResponse>();
            return stringResponse?.result;
        }

        /// <summary>
        /// Deserialises the json to BoolResponse and returns the result
        /// See <see cref="Immutable.Passport.Core.BoolResponse" />
        /// </summary>
        public static bool? GetBoolResponse(this string json)
        {
            var boolResponse = json.OptDeserializeObject<BoolResponse>();
            return boolResponse?.result;
        }
    }
}
