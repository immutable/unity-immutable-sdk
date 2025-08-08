using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Enum representing direct login methods for authentication providers.
    /// </summary>
    [Serializable]
    public enum DirectLoginMethod
    {
        Email,
        Google,
        Apple,
        Facebook
    }
}