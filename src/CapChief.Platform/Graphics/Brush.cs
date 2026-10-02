namespace CapChief.Graphics;

/// <summary>
/// Represents a solid color brush used to fill shapes.
/// </summary>
public sealed class Brush : IDisposable
{
    private IntPtr _h;

    public Brush(uint argb)
    {
        Win32.GdipCreateSolidFill(argb, out _h);
    }

    public static implicit operator IntPtr(Brush b)
    {
        return b._h;
    }

    public uint Color
    {
        set
        {
            Win32.GdipSetSolidFillColor(_h, value);
        }
    }

    void IDisposable.Dispose()
    {
        Free();
    }

    public void Free()
    {
        if (_h == IntPtr.Zero)
        {
            return;
        }
        Win32.GdipDeleteBrush(_h);
        _h = IntPtr.Zero;
    }
}
