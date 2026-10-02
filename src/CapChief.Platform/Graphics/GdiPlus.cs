namespace CapChief.Graphics;

/// <summary>
/// Handles starting and shutting down the GDI+ library for the app.
/// </summary>
public sealed class GdiPlus
{
    private const int Ok = 0;

    private IntPtr _token;

    public bool Start()
    {
        var gsi = Win32.GdiplusStartupInput.Default;
        if (Win32.GdiplusStartup(out _token, ref gsi, IntPtr.Zero) == Ok)
        {
            return true;
        }
        _token = IntPtr.Zero;
        return false;
    }

    public void Stop()
    {
        if (_token == IntPtr.Zero)
        {
            return;
        }
        Win32.GdiplusShutdown(_token);
        _token = IntPtr.Zero;
    }
}
