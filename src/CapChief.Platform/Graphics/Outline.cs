namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI+ path built from lines, arcs, ellipses and curves.
/// </summary>
public readonly ref struct Outline
{
    private readonly IntPtr _h;

    public Outline(bool winding)
    {
        int fillMode;
        if (winding)
        {
            fillMode = Win32.FillModeWinding;
        }
        else
        {
            fillMode = Win32.FillModeAlternate;
        }
        Win32.GdipCreatePath(fillMode, out _h);
    }

    public static implicit operator IntPtr(Outline o)
    {
        return o._h;
    }

    public void Line(float x1, float y1, float x2, float y2)
    {
        Win32.GdipAddPathLine(_h, x1, y1, x2, y2);
    }

    public void Lines(PointF[] pts, int count)
    {
        Win32.GdipAddPathLine2(_h, pts, count);
    }

    public void Arc(float x, float y, float w, float h, float start, float sweep)
    {
        Win32.GdipAddPathArc(_h, x, y, w, h, start, sweep);
    }

    public void Ellipse(float x, float y, float w, float h)
    {
        Win32.GdipAddPathEllipse(_h, x, y, w, h);
    }

    public void Close()
    {
        Win32.GdipClosePathFigure(_h);
    }

    public void RoundRect(float x, float y, float w, float h, float r)
    {
        r = MathF.Min(r, MathF.Min(w, h) * 0.5f);
        float d = r * 2;
        Arc(x, y, d, d, 180, 90);
        Arc(x + w - d, y, d, d, 270, 90);
        Arc(x + w - d, y + h - d, d, d, 0, 90);
        Arc(x, y + h - d, d, d, 90, 90);
        Close();
    }

    public void Dispose()
    {
        Win32.GdipDeletePath(_h);
    }
}
