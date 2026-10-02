namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI+ pen used to draw lines and outlines, with rounded ends and corners unless made
/// plain.
/// </summary>
public sealed class Pen : IDisposable
{
    private IntPtr _h;

    public Pen(uint argb, float width, bool rounded = true)
    {
        Win32.GdipCreatePen1(argb, width, Win32.UnitPixel, out _h);
        if (!rounded)
        {
            return;
        }
        Win32.GdipSetPenStartCap(_h, Win32.LineCapRound);
        Win32.GdipSetPenEndCap(_h, Win32.LineCapRound);
        Win32.GdipSetPenLineJoin(_h, Win32.LineJoinRound);
    }

    public static implicit operator IntPtr(Pen p)
    {
        return p._h;
    }

    public uint Color
    {
        set
        {
            Win32.GdipSetPenColor(_h, value);
        }
    }

    public float Width
    {
        set
        {
            Win32.GdipSetPenWidth(_h, value);
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
        Win32.GdipDeletePen(_h);
        _h = IntPtr.Zero;
    }
}
