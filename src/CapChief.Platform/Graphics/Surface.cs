using System.Runtime.InteropServices;

namespace CapChief.Graphics;

/// <summary>
/// Represents a bitmap in memory that can be drawn on with GDI.
/// </summary>
public sealed class Surface : IDisposable
{
    private IntPtr _oldBm;

    public IntPtr Dc { get; private set; }

    private IntPtr _bm;

    public IntPtr Bits { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Rect Bounds
    {
        get
        {
            return new() { Right = Width, Bottom = Height };
        }
    }

    public DeviceContext DeviceContext
    {
        get
        {
            return new(Dc);
        }
    }

    public unsafe Span<byte> Pixels
    {
        get
        {
            return new((void*)Bits, Width * Height * 4);
        }
    }

    public bool IsOpen
    {
        get
        {
            return _bm != IntPtr.Zero;
        }
    }

    public static Win32.BITMAPINFOHEADER Hdr(int w, int h)
    {
        return new()
        {
            biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
            biWidth = w,
            biHeight = h,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = Win32.BI_RGB,
        };
    }

    public bool OpenDib(int w, int h, bool withDc = true)
    {
        Free();
        if (w <= 0 || h <= 0)
        {
            return false;
        }

        using var screen = ScreenDc.Borrow();
        Win32.BITMAPINFOHEADER hdr = Hdr(w, -h);
        _bm = Win32.CreateDIBSection(screen.Dc, ref hdr, Win32.DIB_RGB_COLORS, out IntPtr bits, IntPtr.Zero, 0);
        Bits = bits;
        if (withDc)
        {
            SelectDc(screen.Dc);
        }

        if (_bm == IntPtr.Zero || Bits == IntPtr.Zero || (withDc && Dc == IntPtr.Zero))
        {
            Free();
            return false;
        }
        Width = w;
        Height = h;
        return true;
    }

    public bool OpenCompatible(int w, int h)
    {
        Free();
        if (w <= 0 || h <= 0)
        {
            return false;
        }

        using var screen = ScreenDc.Borrow();
        _bm = Win32.CreateCompatibleBitmap(screen.Dc, w, h);
        SelectDc(screen.Dc);

        if (_bm == IntPtr.Zero || Dc == IntPtr.Zero)
        {
            Free();
            return false;
        }
        Width = w;
        Height = h;
        return true;
    }

    public bool EnsureAtLeast(int w, int h, int minW = 0, int minH = 0)
    {
        return (IsOpen && w <= Width && h <= Height) || OpenDib(Math.Max(w, minW), Math.Max(h, minH));
    }

    public unsafe bool ReadInto(byte[] buf, bool bottomUp)
    {
        fixed (byte* p = buf)
        {
            return ReadInto((IntPtr)p, bottomUp);
        }
    }

    public byte[]? ReadPixels(Rect r)
    {
        Rect c = Rect.Intersect(r, Bounds);
        if (!IsOpen || c.IsEmpty)
        {
            return null;
        }

        var whole = new byte[(long)Width * Height * 4];
        if (!ReadInto(whole, bottomUp: false))
        {
            return null;
        }
        if (c == Bounds)
        {
            return whole;
        }

        var cut = new byte[(long)c.Width * c.Height * 4];
        new Pixmap(whole, Width, Height, Width).CopyArea(c, cut);
        return cut;
    }

    public void WriteFrom(byte[] src)
    {
        Marshal.Copy(src, 0, Bits, src.Length);
    }

    public bool ReadInto(IntPtr dst, bool bottomUp)
    {
        if (_bm == IntPtr.Zero || Dc == IntPtr.Zero || _oldBm == IntPtr.Zero || dst == IntPtr.Zero)
        {
            return false;
        }

        Win32.SelectObject(Dc, _oldBm);
        int hdrHeight;
        if (bottomUp)
        {
            hdrHeight = Height;
        }
        else
        {
            hdrHeight = -Height;
        }
        Win32.BITMAPINFOHEADER hdr = Hdr(Width, hdrHeight);
        int got = Win32.GetDIBits(Dc, _bm, 0, (uint)Height, dst, ref hdr, Win32.DIB_RGB_COLORS);
        Win32.SelectObject(Dc, _bm);
        return got == Height;
    }

    public IntPtr TakeBitmap()
    {
        IntPtr bm = _bm;
        if (Dc != IntPtr.Zero && _oldBm != IntPtr.Zero)
        {
            Win32.SelectObject(Dc, _oldBm);
            _oldBm = IntPtr.Zero;
        }
        _bm = Bits = IntPtr.Zero;
        return bm;
    }

    void IDisposable.Dispose()
    {
        Free();
    }

    public void Free()
    {
        if (Dc != IntPtr.Zero)
        {
            if (_oldBm != IntPtr.Zero)
            {
                Win32.SelectObject(Dc, _oldBm);
            }
            Win32.DeleteDC(Dc);
        }
        if (_bm != IntPtr.Zero)
        {
            Win32.DeleteObject(_bm);
        }
        Dc = _bm = Bits = _oldBm = IntPtr.Zero;
        Width = Height = 0;
    }

    private void SelectDc(IntPtr sdc)
    {
        if (_bm == IntPtr.Zero)
        {
            return;
        }
        Dc = Win32.CreateCompatibleDC(sdc);
        if (Dc != IntPtr.Zero)
        {
            _oldBm = Win32.SelectObject(Dc, _bm);
        }
    }
}
