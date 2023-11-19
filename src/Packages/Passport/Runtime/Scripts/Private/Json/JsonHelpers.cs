using System;
using UnityEngine;

namespace Immutable.Passport.Json
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
                Debug.Log($"Failed to deserialise {json}: {e.Message}");
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

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }
}