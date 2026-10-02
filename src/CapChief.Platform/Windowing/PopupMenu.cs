namespace CapChief.Windowing;

/// <summary>
/// Represents a context menu that can be shown at a screen position.
/// </summary>
public sealed class PopupMenu
{
    private IntPtr _h;

    public bool Open()
    {
        _h = Win32.CreatePopupMenu();
        return _h != IntPtr.Zero;
    }

    public void Add(int command, string label)
    {
        Win32.AppendMenuW(_h, Win32.MF_STRING, (UIntPtr)command, label);
    }

    public void AddSeparator()
    {
        Win32.AppendMenuW(_h, Win32.MF_SEPARATOR, UIntPtr.Zero, null);
    }

    public void Default(int command)
    {
        Win32.SetMenuDefaultItem(_h, (uint)command, Win32.MF_BYCOMMAND);
    }

    public int ShowAt(Window owner, Point at)
    {
        Win32.SetForegroundWindow(owner.Handle);
        int cmd = Win32.TrackPopupMenu(_h, Win32.TPM_RIGHTBUTTON | Win32.TPM_RETURNCMD, at.X, at.Y, 0, owner.Handle, IntPtr.Zero);
        Win32.PostMessageW(owner.Handle, Win32.WM_NULL, IntPtr.Zero, IntPtr.Zero);
        Win32.DestroyMenu(_h);
        _h = IntPtr.Zero;
        return cmd;
    }
}
