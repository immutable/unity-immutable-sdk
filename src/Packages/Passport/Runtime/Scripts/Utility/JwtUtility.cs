using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Immutable.Passport.Utility {
    public class JwtUtility {
        public static string? decodeJwt(string jwt) {
            try {
                string[] parts = jwt.Split(".");
                byte[] data = Convert.FromBase64String(parts[1]);
                return Encoding.UTF8.GetString(data);
            } catch (Exception ex) {
                return null;
            }
        }
    }
}