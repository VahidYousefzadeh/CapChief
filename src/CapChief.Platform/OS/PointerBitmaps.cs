namespace CapChief.OS;

/// <summary>
/// Reads a mouse pointer's bitmaps and hot spot, and frees the bitmaps when disposed.
/// </summary>
public readonly ref struct PointerBitmaps
{
    private readonly IntPtr _color;
    private readonly IntPtr _mask;

    private PointerBitmaps(Win32.ICONINFO info)
    {
        _color = info.hbmColor;
        _mask = info.hbmMask;
        IsRead = true;
        HotX = (int)info.xHotspot;
        HotY = (int)info.yHotspot;
    }

    public bool IsRead { get; }

    public int HotX { get; }

    public int HotY { get; }

    public IntPtr Bitmap
    {
        get
        {
            if (_color != IntPtr.Zero)
            {
                return _color;
            }
            else
            {
                return _mask;
            }
        }
    }

    public static PointerBitmaps Of(IntPtr cursor)
    {
        var info = default(Win32.ICONINFO);
        if (!Win32.GetIconInfo(cursor, ref info))
        {
            return default;
        }
        return new(info);
    }

    public void Dispose()
    {
        if (_color != IntPtr.Zero)
        {
            Win32.DeleteObject(_color);
        }
        if (_mask != IntPtr.Zero)
        {
            Win32.DeleteObject(_mask);
        }
    }
}
