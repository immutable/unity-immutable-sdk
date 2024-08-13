using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Immutable.Passport.Core.Logging;

namespace Immutable.Passport.Helpers
{
    public static class JsonExtensions
    {
        /// <summary>
        /// Return null if the deserialisation fails.
        /// </summary> 
        public static T OptDeserializeObject<T>(this string json) where T : class
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                PassportLogger.Debug($"Failed to deserialise {json}: {e.Message}");
                return null;
            }
        }

        public static string ToJson<T>(this T[] array)
        {
            // Need a wrapper to serialise arrays
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            string wrapped = JsonUtility.ToJson(wrapper);
            // Remove the wrapper
            return wrapped.ReplaceFirst("{\"Items\":", "").ReplaceLast("}", "");
        }

        private static string ReplaceFirst(this string source, string search, string replace)
        {
            int pos = source.IndexOf(search);
            if (pos < 0)
            {
                return source;
            }
            return source.Substring(0, pos) + replace + source.Substring(pos + search.Length);
        }

        private static string ReplaceLast(this string source, string search, string replace)
        {
            int place = source.LastIndexOf(search);
            if (place == -1)
            {
                return source;
            }
            return source.Remove(place, search.Length).Insert(place, replace);
        }

        public static string ToJson(this IDictionary<string, object> dictionary)
        {
            // JsonUtility cannot serialise dictionary, but not using newtonsoft json as it doesn't
            // work properly with older unity versions so doing it manually
            StringBuilder sb = new StringBuilder("{");
            for (int i = 0; i < dictionary.Count; i++)
            {
                object value = dictionary.ElementAt(i).Value;
                if (value is string || value is int || value is long || value is double || value is bool)
                {
                    string key = dictionary.ElementAt(i).Key;
                    sb = sb.Append("\"").Append(key).Append("\":");
                    if (value is string)
                    {
                        sb = sb.Append($"\"{value}\"");
                    }
                    else if (value is int || value is long || value is double)
                    {
                        sb = sb.Append(value);
                    }
                    else if (value is bool)
                    {
                        sb = sb.Append(value.ToString().ToLower());
                    }
                }
                if (i < dictionary.Count - 1)
                {
                    sb = sb.Append(",");
                }
            }
            sb = sb.Append("}");
            return sb.ToString();
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}