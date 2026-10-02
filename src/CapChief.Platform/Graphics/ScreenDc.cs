namespace CapChief.Graphics;

/// <summary>
/// Temporarily borrows the device context for the whole screen.
/// </summary>
public readonly ref struct ScreenDc
{
    private ScreenDc(IntPtr dc)
    {
        Dc = dc;
    }

    public IntPtr Dc { get; }

    public static ScreenDc Borrow()
    {
        return new(Win32.GetDC(IntPtr.Zero));
    }

    public void Dispose()
    {
        Win32.ReleaseDC(IntPtr.Zero, Dc);
    }
}
