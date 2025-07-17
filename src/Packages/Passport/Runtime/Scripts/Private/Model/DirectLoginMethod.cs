using System;

namespace Immutable.Passport.Model
{
    /// <summary>
    /// Enum for direct login methods supported by Passport.
    /// </summary>
    [Serializable]
    public enum DirectLoginMethod
    {
        None,
        Google,
        Apple,
        Facebook
    }
}