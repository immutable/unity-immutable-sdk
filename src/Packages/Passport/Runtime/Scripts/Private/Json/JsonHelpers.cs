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
    }
}