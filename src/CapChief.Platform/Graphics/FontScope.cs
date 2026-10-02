namespace CapChief.Graphics;

/// <summary>
/// Temporarily selects a font into a device context for a drawing operation, and makes text draw
/// with no background box; that background setting stays after the scope ends.
/// </summary>
public readonly ref struct FontScope
{
    private readonly IntPtr _dc;
    private readonly IntPtr _old;

    public FontScope(IntPtr dc, IntPtr font)
    {
        _dc = dc;
        _old = Win32.SelectObject(dc, font);
        Win32.SetBkMode(dc, Win32.TRANSPARENT);
    }

    public void Dispose()
    {
        Win32.SelectObject(_dc, _old);
    }
}
