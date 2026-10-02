namespace CapChief.Graphics;

/// <summary>
/// Temporarily restricts drawing on a device context to a rectangle or region.
/// </summary>
public readonly ref struct ClippedDc
{
    private readonly IntPtr _dc;
    private readonly int _saved;

    private ClippedDc(IntPtr dc, int saved)
    {
        _dc = dc;
        _saved = saved;
    }

    public static ClippedDc ToRect(IntPtr dc, Rect r)
    {
        int saved = Win32.SaveDC(dc);
        Win32.IntersectClipRect(dc, r.Left, r.Top, r.Right, r.Bottom);
        return new ClippedDc(dc, saved);
    }

    public static ClippedDc ToRegion(IntPtr dc, IntPtr rgn)
    {
        int saved = Win32.SaveDC(dc);
        Win32.SelectClipRgn(dc, rgn);
        return new ClippedDc(dc, saved);
    }

    public void Dispose()
    {
        if (_saved != 0)
        {
            Win32.RestoreDC(_dc, _saved);
        }
    }
}
