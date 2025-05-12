public static class SampleAppManager
{
    /// <summary>
    /// Indicates whether the selected authentication method is PKCE.
    /// </summary>
    public static bool UsePKCE { get; set; }

    /// <summary>
    /// Indicates whether the user is connected to IMX.
    /// </summary>
    public static bool IsConnectedToImx { get; set; }

    /// <summary>
    /// Indicates whether the user is connected to zkEVM.
    /// </summary>
    public static bool IsConnectedToZkEvm { get; set; }
} 