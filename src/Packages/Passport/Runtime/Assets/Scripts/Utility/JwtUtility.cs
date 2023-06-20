using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Immutable.Passport.Utility
{
    public class JwtUtility
    {
        private const string TAG = "[JWT Utility]";

        public static string? decodeJwt(string jwt)
        {
            try
            {
                string[] parts = jwt.Split(".");
                string payload = parts[1];
                string padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                byte[] data = Convert.FromBase64String(padded);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Error decoding JWT: {ex.Message}");
                return null;
            }
        }
    }
}