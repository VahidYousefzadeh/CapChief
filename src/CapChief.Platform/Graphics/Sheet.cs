namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI+ drawing surface over an in-memory bitmap.
/// </summary>
public sealed class Sheet
{
    private IntPtr _img, _g;

    public bool IsOpen
    {
        get
        {
            return _g != IntPtr.Zero;
        }
    }

    public IntPtr Graphics
    {
        get
        {
            return _g;
        }
    }

    public bool Open(Surface surf, bool highQualityPixelOffset = true)
    {
        Free();
        if (!surf.IsOpen)
        {
            return false;
        }

        Win32.GdipCreateBitmapFromScan0(surf.Width, surf.Height, surf.Width * 4, Win32.PixelFormat32bppPARGB, surf.Bits, out _img);
        if (_img == IntPtr.Zero)
        {
            return false;
        }

        Win32.GdipGetImageGraphicsContext(_img, out _g);
        if (_g == IntPtr.Zero)
        {
            return false;
        }

        Win32.GdipSetSmoothingMode(_g, Win32.SmoothingModeAntiAlias);
        if (highQualityPixelOffset)
        {
            Win32.GdipSetPixelOffsetMode(_g, Win32.PixelOffsetModeHighQuality);
        }
        return true;
    }

    public void Free()
    {
        if (_g != IntPtr.Zero)
        {
            Win32.GdipDeleteGraphics(_g);
            _g = IntPtr.Zero;
        }
        if (_img != IntPtr.Zero)
        {
            Win32.GdipDisposeImage(_img);
            _img = IntPtr.Zero;
        }
    }

    public void Flush()
    {
        Win32.GdipFlush(_g, Win32.FlushIntentionSync);
    }

    public void Line(Pen pen, float x1, float y1, float x2, float y2)
    {
        Win32.GdipDrawLine(_g, pen, x1, y1, x2, y2);
    }

    public void Trace(Pen pen, Outline o)
    {
        Win32.GdipDrawPath(_g, pen, o);
    }

    public void Fill(Brush brush, Outline o)
    {
        Win32.GdipFillPath(_g, brush, o);
    }

    public void Ring(Pen pen, float x, float y, float w, float h)
    {
        Win32.GdipDrawEllipse(_g, pen, x, y, w, h);
    }

    public void Disc(Brush brush, float x, float y, float w, float h)
    {
        Win32.GdipFillEllipse(_g, brush, x, y, w, h);
    }

    public void Block(Brush brush, float x, float y, float w, float h)
    {
        Win32.GdipFillRectangle(_g, brush, x, y, w, h);
    }

    public void Round(Brush brush, float x, float y, float w, float h, float r)
    {
        using var o = new Outline(winding: true);
        o.RoundRect(x, y, w, h, r);
        Fill(brush, o);
    }
}
