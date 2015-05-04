namespace JPB.RemoteAdmin.Server.Native.KeyAddon.Controls
{
    /// <summary>
    /// Indicates which hooks to listen to application or global.
    /// </summary>
    public enum HookType
    {
        /// <summary>
        /// Only events inside the application are monitored and fired.
        /// </summary>
        Application,

        /// <summary>
        /// All events system wide are monitored and fired.
        /// </summary>
        Global
    }
}
