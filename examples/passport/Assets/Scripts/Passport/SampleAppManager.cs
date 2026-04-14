public static class SampleAppManager
{
    /// <summary>
    /// Indicates whether the user is connected to zkEVM.
    /// </summary>
    public static bool IsConnectedToZkEvm { get; set; }

    /// <summary>
    /// Holds the Passport instance.
    /// </summary>
    public static Immutable.Passport.Passport PassportInstance { get; set; }
}