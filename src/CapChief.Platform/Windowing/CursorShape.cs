namespace CapChief.Windowing;

/// <summary>
/// Represents a mouse cursor and provides the standard system cursors.
/// </summary>
public sealed class CursorShape
{
    private readonly IntPtr _h;

    private CursorShape(IntPtr h)
    {
        _h = h;
    }

    public static implicit operator IntPtr(CursorShape c)
    {
        return c._h;
    }

    public static CursorShape Arrow
    {
        get
        {
            return Stock(Win32.IDC_ARROW);
        }
    }

    public static CursorShape Hand
    {
        get
        {
            return Stock(Win32.IDC_HAND);
        }
    }

    public static CursorShape Cross
    {
        get
        {
            return Stock(Win32.IDC_CROSS);
        }
    }

    public static CursorShape SizeNWSE
    {
        get
        {
            return Stock(Win32.IDC_SIZENWSE);
        }
    }

    public static CursorShape SizeNESW
    {
        get
        {
            return Stock(Win32.IDC_SIZENESW);
        }
    }

    public static CursorShape SizeNS
    {
        get
        {
            return Stock(Win32.IDC_SIZENS);
        }
    }

    public static CursorShape SizeWE
    {
        get
        {
            return Stock(Win32.IDC_SIZEWE);
        }
    }

    public static CursorShape SizeAll
    {
        get
        {
            return Stock(Win32.IDC_SIZEALL);
        }
    }

    public static CursorShape Made(IntPtr handle)
    {
        return new(handle);
    }

    private static CursorShape Stock(int id)
    {
        return new(Win32.LoadCursorW(IntPtr.Zero, (IntPtr)id));
    }
}
