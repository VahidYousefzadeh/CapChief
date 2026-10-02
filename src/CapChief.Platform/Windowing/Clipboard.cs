using System.Runtime.InteropServices;

namespace CapChief.Windowing;

/// <summary>
/// Provides methods for placing a bitmap on the Windows clipboard.
/// </summary>
public static class Clipboard
{
    private const int Tries = 6;
    private const int WaitMs = 40;

    public static unsafe bool Put(Surface s, Window owner)
    {
        if (!owner.Exists)
        {
            return false;
        }
        int w = s.Width;
        int h = s.Height;

        uint hdrSz = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
        nuint total = hdrSz + (nuint)((long)w * h * 4);
        IntPtr hg = Win32.GlobalAlloc(Win32.GMEM_MOVEABLE, total);
        if (hg == IntPtr.Zero)
        {
            return false;
        }
        IntPtr p = Win32.GlobalLock(hg);
        if (p == IntPtr.Zero)
        {
            Win32.GlobalFree(hg);
            return false;
        }
        *(Win32.BITMAPINFOHEADER*)p = Surface.Hdr(w, h);
        bool read = s.ReadInto((IntPtr)(p + (long)hdrSz), bottomUp: true);
        Win32.GlobalUnlock(hg);
        if (!read)
        {
            Win32.GlobalFree(hg);
            return false;
        }

        bool opened = Open(owner);
        if (!opened)
        {
            Win32.GlobalFree(hg);
            return false;
        }

        Win32.EmptyClipboard();
        bool tookDib = Win32.SetClipboardData(Win32.CF_DIB, hg) != IntPtr.Zero;
        if (!tookDib)
        {
            Win32.GlobalFree(hg);
        }
        IntPtr bitmap = s.TakeBitmap();
        bool tookBitmap = Win32.SetClipboardData(Win32.CF_BITMAP, bitmap) != IntPtr.Zero;
        if (!tookBitmap)
        {
            Win32.DeleteObject(bitmap);
        }
        Win32.CloseClipboard();
        return tookDib || tookBitmap;
    }

    public static bool Open(Window owner)
    {
        for (int i = 0; i < Tries; i++)
        {
            if (Win32.OpenClipboard(owner.Handle))
            {
                return true;
            }
            Thread.Sleep(WaitMs);
        }
        return false;
    }
}
