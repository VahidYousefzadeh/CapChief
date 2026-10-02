namespace CapChief.Graphics;

/// <summary>
/// Measures the width and height of a text string in a given font.
/// </summary>
public sealed class TextRuler
{
    private IntPtr _dc;

    public IntPtr Dc
    {
        get
        {
            if (_dc != IntPtr.Zero)
            {
                return _dc;
            }
            using var screen = ScreenDc.Borrow();
            _dc = Win32.CreateCompatibleDC(screen.Dc);
            return _dc;
        }
    }

    public Size Measure(string s, IntPtr font)
    {
        IntPtr dc = Dc;
        using var selected = new FontScope(dc, font);
        Win32.GetTextExtentPoint32W(dc, s, s.Length, out Size sz);
        return sz;
    }

    public void Free()
    {
        if (_dc == IntPtr.Zero)
        {
            return;
        }
        Win32.DeleteDC(_dc);
        _dc = IntPtr.Zero;
    }
}
