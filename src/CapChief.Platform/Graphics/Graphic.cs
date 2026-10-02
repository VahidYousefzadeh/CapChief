namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI+ drawing surface created over, or borrowed from, a device context.
/// </summary>
public readonly ref struct Graphic
{
    private readonly IntPtr _h;

    public Graphic(DeviceContext dc)
    {
        _h = dc.Graphics;
        IsBorrowed = _h != IntPtr.Zero;
        if (IsBorrowed)
        {
            return;
        }
        Win32.GdipCreateFromHDC(dc, out _h);
        Win32.GdipSetSmoothingMode(_h, Win32.SmoothingModeAntiAlias);
    }

    public static implicit operator IntPtr(Graphic g)
    {
        return g._h;
    }

    public bool IsBorrowed { get; }

    public void Dispose()
    {
        if (!IsBorrowed)
        {
            Win32.GdipDeleteGraphics(_h);
        }
    }
}
