using System;
using System.Text;
using UnityEngine;

namespace Immutable.Passport.Utility
{
    public class JwtUtility
    {
        private const string TAG = "[JWT Utility]";

        public static string? DecodeJwt(string jwt)
        {
            try
            {
                string[] parts = jwt.Split(".");
                string payload = parts[1];
                return Base64DecodeToString(payload);
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Error decoding JWT: {ex.Message} - {jwt}");
                return null;
            }
        }

        // https://github.com/greenygh0st/JWT-Decoder/blob/master/JWTDecoder/JWTDecoder.cs#L50
        private static string Base64DecodeToString(string ToDecode)
        {
            string decodePrepped = ToDecode.Replace("-", "+").Replace("_", "/");

            switch (decodePrepped.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    decodePrepped += "==";
                    break;
                case 3:
                    decodePrepped += "=";
                    break;
                default:
                    throw new Exception("Not a legal base64 string!");
            }

            byte[] data = Convert.FromBase64String(decodePrepped);
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }
}