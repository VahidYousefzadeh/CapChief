namespace CapChief.Graphics;

/// <summary>
/// Represents a drawing target that wraps a device context and an optional GDI+ graphics object.
/// </summary>
public readonly struct DeviceContext
{
    private readonly IntPtr _dc;
    private readonly IntPtr _g;

    public DeviceContext(IntPtr dc, IntPtr graphics = default)
    {
        _dc = dc;
        _g = graphics;
    }

    public static implicit operator IntPtr(DeviceContext dc)
    {
        return dc._dc;
    }

    public IntPtr Graphics
    {
        get
        {
            return _g;
        }
    }

    public bool Exists
    {
        get
        {
            return _dc != IntPtr.Zero;
        }
    }

    public static void Flush()
    {
        Win32.GdiFlush();
    }

    public void Blit(Rect at, DeviceContext from, int sx, int sy)
    {
        Win32.BitBlt(_dc, at.Left, at.Top, at.Width, at.Height, from, sx, sy, Win32.SRCCOPY);
    }

    public void StretchBlit(Rect at, DeviceContext from, Rect src)
    {
        int oldMode = Win32.SetStretchBltMode(_dc, Win32.HALFTONE);
        Win32.SetBrushOrgEx(_dc, 0, 0, IntPtr.Zero);
        Win32.StretchBlt(_dc, at.Left, at.Top, at.Width, at.Height, from, src.Left, src.Top, src.Width, src.Height, Win32.SRCCOPY);
        if (oldMode != 0)
        {
            Win32.SetStretchBltMode(_dc, oldMode);
        }
    }

    public void AlphaBlend(Rect at, DeviceContext from, Rect src)
    {
        Win32.AlphaBlend(_dc, at.Left, at.Top, at.Width, at.Height, from, src.Left, src.Top, src.Width, src.Height, Win32.BLENDFUNCTION.Over);
    }

    public void DrawText(string s, Rect r, uint colorRef, IntPtr font, uint format)
    {
        using FontScope selected = SelectFont(font);
        Win32.SetTextColor(_dc, colorRef);
        Win32.DrawTextW(_dc, s, s.Length, ref r, format);
    }

    public void DrawTextRun(string s, Rect r, uint colorRef)
    {
        Win32.SetTextColor(_dc, colorRef);
        Win32.DrawTextW(_dc, s, s.Length, ref r, TextLayout.Line);
    }

    public FontScope SelectFont(IntPtr font)
    {
        return new(_dc, font);
    }

    public void FillRect(Rect r, uint colorRef)
    {
        IntPtr brush = Win32.CreateSolidBrush(colorRef);
        Win32.FillRect(_dc, ref r, brush);
        Win32.DeleteObject(brush);
    }

    public void SetTextColor(uint colorRef)
    {
        Win32.SetTextColor(_dc, colorRef);
        Win32.SetBkMode(_dc, Win32.TRANSPARENT);
    }

    public void Ring(uint argb, float width, float x, float y, float w, float h)
    {
        if (_g == IntPtr.Zero)
        {
            return;
        }
        using var pen = new Pen(argb, width);
        Win32.GdipDrawEllipse(_g, pen, x, y, w, h);
    }

    public void Line(uint argb, float width, float x1, float y1, float x2, float y2)
    {
        if (_g == IntPtr.Zero)
        {
            return;
        }
        using var pen = new Pen(argb, width);
        Win32.GdipDrawLine(_g, pen, x1, y1, x2, y2);
    }
}
