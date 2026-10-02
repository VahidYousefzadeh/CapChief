namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI region built up from rectangles.
/// </summary>
public sealed class Region
{
    private IntPtr _h;

    public static implicit operator IntPtr(Region r)
    {
        return r._h;
    }

    public bool Exists
    {
        get
        {
            return _h != IntPtr.Zero;
        }
    }

    public bool Open()
    {
        if (_h == IntPtr.Zero)
        {
            _h = Win32.CreateRectRgn(0, 0, 0, 0);
        }
        return _h != IntPtr.Zero;
    }

    public void Add(Rect r)
    {
        IntPtr piece = Win32.CreateRectRgn(r.Left, r.Top, r.Right, r.Bottom);
        if (piece == IntPtr.Zero)
        {
            return;
        }
        Win32.CombineRgn(_h, _h, piece, Win32.RGN_OR);
        Win32.DeleteObject(piece);
    }

    public bool Box(out Rect r)
    {
        return Win32.GetRgnBox(_h, out r) != Win32.NULLREGION;
    }

    public void Free()
    {
        if (_h == IntPtr.Zero)
        {
            return;
        }
        Win32.DeleteObject(_h);
        _h = IntPtr.Zero;
    }
}
