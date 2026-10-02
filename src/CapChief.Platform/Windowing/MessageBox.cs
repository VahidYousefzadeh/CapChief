namespace CapChief.Windowing;

/// <summary>
/// Provides methods for showing a warning message box to the user.
/// </summary>
public static class MessageBox
{
    public static void Warn(Window owner, string message, string title)
    {
        Win32.MessageBoxW(owner.Handle, message, title, Win32.MB_ICONWARNING);
    }
}
