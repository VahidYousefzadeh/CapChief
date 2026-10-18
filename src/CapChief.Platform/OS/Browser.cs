namespace CapChief.OS;

/// <summary>
/// Provides a method for opening a web address in the user's default browser.
/// </summary>
public static class Browser
{
    /// <summary>
    /// Opens the address and tells whether Windows started the browser for it.
    /// </summary>
    public static bool Open(string url)
    {
        return Win32.ShellExecuteW(IntPtr.Zero, "open", url, null, null, Win32.SW_SHOWNORMAL).ToInt64() > 32;
    }
}
