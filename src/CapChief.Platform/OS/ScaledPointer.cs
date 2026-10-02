using System.Runtime.InteropServices;

namespace CapChief.OS;

/// <summary>
/// Provides the mouse pointer as Windows draws it on a monitor of a given scale. Programs get Windows'
/// own pointers at the main monitor's size only, so for another size this loads them again from the
/// user's pointer scheme, or resizes them where the scheme names no file; a program's own pointer is
/// left as it is, since Windows does not resize those either.
/// </summary>
public sealed class ScaledPointer
{
    private const string Scheme = @"Control Panel\Cursors";
    private const int StandardSize = 32;
    private const int PathChars = 1024;

    private static readonly (int Id, string Name)[] WindowsPointers =
    [
        (Win32.IDC_ARROW, "Arrow"), (Win32.IDC_IBEAM, "IBeam"), (Win32.IDC_WAIT, "Wait"),
        (Win32.IDC_CROSS, "Crosshair"), (Win32.IDC_UPARROW, "UpArrow"), (Win32.IDC_SIZENWSE, "SizeNWSE"),
        (Win32.IDC_SIZENESW, "SizeNESW"), (Win32.IDC_SIZEWE, "SizeWE"), (Win32.IDC_SIZENS, "SizeNS"),
        (Win32.IDC_SIZEALL, "SizeAll"), (Win32.IDC_NO, "No"), (Win32.IDC_HAND, "Hand"),
        (Win32.IDC_APPSTARTING, "AppStarting"), (Win32.IDC_HELP, "Help"), (Win32.IDC_PIN, "Pin"),
        (Win32.IDC_PERSON, "Person"),
    ];

    private IntPtr _original;
    private float _scale;
    private IntPtr _sized;

    public IntPtr At(IntPtr pointer, float scale)
    {
        if (pointer != _original || scale != _scale)
        {
            Free();
            _original = pointer;
            _scale = scale;
            _sized = Resized(pointer, scale);
        }
        if (_sized != IntPtr.Zero)
        {
            return _sized;
        }
        else
        {
            return pointer;
        }
    }

    public void Free()
    {
        if (_sized != IntPtr.Zero)
        {
            Win32.DestroyCursor(_sized);
        }
        _original = _sized = IntPtr.Zero;
    }

    private static IntPtr Resized(IntPtr pointer, float scale)
    {
        if (NameOf(pointer) is not { } name)
        {
            return IntPtr.Zero;
        }
        Look shown = LookOf(pointer);
        int size = (int)MathF.Round(BaseSize() * scale);
        if (shown.Width <= 0 || size <= 0 || size == shown.Width)
        {
            return IntPtr.Zero;
        }

        string file = SchemeFile(name);
        if (file.Length > 0 && Holds(file, shown))
        {
            IntPtr loaded = Win32.LoadImageW(IntPtr.Zero, file, Win32.IMAGE_CURSOR, size, size, Win32.LR_LOADFROMFILE);
            if (loaded != IntPtr.Zero)
            {
                return loaded;
            }
        }
        return Win32.CopyImage(pointer, Win32.IMAGE_CURSOR, size, size, 0);
    }

    private static string? NameOf(IntPtr pointer)
    {
        foreach ((int id, string name) in WindowsPointers)
        {
            if (Win32.LoadCursorW(IntPtr.Zero, id) == pointer)
            {
                return name;
            }
        }
        return null;
    }

    private static bool Holds(string file, Look pointer)
    {
        IntPtr loaded = Win32.LoadImageW(IntPtr.Zero, file, Win32.IMAGE_CURSOR, pointer.Width, pointer.Width, Win32.LR_LOADFROMFILE);
        if (loaded == IntPtr.Zero)
        {
            return false;
        }
        Look look = LookOf(loaded);
        Win32.DestroyCursor(loaded);
        return look.HotX == pointer.HotX && look.HotY == pointer.HotY && look.Pixels.AsSpan().SequenceEqual(pointer.Pixels);
    }

    private static unsafe Look LookOf(IntPtr cursor)
    {
        using var bits = PointerBitmaps.Of(cursor);
        if (!bits.IsRead)
        {
            return new Look(0, 0, 0, []);
        }
        var bm = default(Win32.BITMAP);
        Win32.GetObjectW(bits.Bitmap, Marshal.SizeOf<Win32.BITMAP>(), ref bm);
        var pixels = new uint[bm.bmWidth * bm.bmHeight];
        Win32.BITMAPINFOHEADER hdr = Surface.Hdr(bm.bmWidth, -bm.bmHeight);
        IntPtr dc = Win32.CreateCompatibleDC(IntPtr.Zero);
        fixed (uint* p = pixels)
        {
            Win32.GetDIBits(dc, bits.Bitmap, 0, (uint)bm.bmHeight, (IntPtr)p, ref hdr, Win32.DIB_RGB_COLORS);
        }
        Win32.DeleteDC(dc);
        return new Look(bm.bmWidth, bits.HotX, bits.HotY, pixels);
    }

    private static int BaseSize()
    {
        int bytes = sizeof(int);
        if (Win32.RegGetValueW(Win32.HKEY_CURRENT_USER, Scheme, "CursorBaseSize", Win32.RRF_RT_REG_DWORD, IntPtr.Zero, out int size, ref bytes) == 0 && size > 0)
        {
            return size;
        }
        else
        {
            return StandardSize;
        }
    }

    private static string SchemeFile(string name)
    {
        var path = new char[PathChars];
        int bytes = path.Length * sizeof(char);
        if (Win32.RegGetValueW(Win32.HKEY_CURRENT_USER, Scheme, name, Win32.RRF_RT_REG_SZ, IntPtr.Zero, path, ref bytes) == 0)
        {
            return new string(path, 0, Math.Max(0, bytes / sizeof(char) - 1));
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// Represents a pointer's width, hot spot and pixels, to tell two pointers apart.
    /// </summary>
    private readonly record struct Look(int Width, int HotX, int HotY, uint[] Pixels);
}
