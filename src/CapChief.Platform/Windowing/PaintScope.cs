namespace CapChief.Windowing;

/// <summary>
/// Represents a single paint operation on a window that ends when it is disposed.
/// </summary>
public sealed class PaintScope : IDisposable
{
    private readonly IntPtr _window;
    private Win32.PAINTSTRUCT _ps;

    public PaintScope(IntPtr window)
    {
        _window = window;
        HasBegun = Win32.BeginPaint(window, out _ps) != IntPtr.Zero;
    }

    public bool HasBegun { get; }

    public DeviceContext DeviceContext
    {
        get
        {
            return new(_ps.hdc);
        }
    }

    public Rect Area
    {
        get
        {
            return _ps.rcPaint;
        }
    }

    public void Dispose()
    {
        if (HasBegun)
        {
            Win32.EndPaint(_window, ref _ps);
        }
    }
}
