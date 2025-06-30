public static class SampleAppManager
{
    /// <summary>
    /// Indicates whether the user is connected to IMX.
    /// </summary>
    public static bool IsConnectedToImx { get; set; }

    /// <summary>
    /// Indicates whether the user is connected to zkEVM.
    /// </summary>
    public static bool IsConnectedToZkEvm { get; set; }

    /// <summary>
    /// Holds the Passport instance for IMX operations.
    /// </summary>
    public static Immutable.Passport.Passport PassportInstance { get; set; }
}