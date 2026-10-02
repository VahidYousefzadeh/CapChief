using System.Runtime.InteropServices;
using CapChief.Interop;

namespace CapChief.Lab;

/// <summary>
/// Checks that a copy puts on the clipboard exactly the capture as it is drawn, as a device
/// independent bitmap and as a GDI bitmap, and that copying again and again leaks no GDI handles.
/// </summary>
public sealed class ClipboardCheck
{
    private const int LeakRuns = 20;

    private readonly App _app;
    private readonly Report _report;

    public ClipboardCheck(App app, Report report)
    {
        _app = app;
        _report = report;
    }

    public byte[]? Render(Capture capture)
    {
        using var image = new Surface();
        if (!capture.Render(image, _app.MarkPainter))
        {
            return null;
        }
        return image.ReadPixels(image.Bounds);
    }

    public void CopiedAsDrawn(byte[]? want, Rect crop)
    {
        byte[]? got = ReadDib(out int w, out int h, out bool bottomUp);
        _report.Check(want is not null && got is not null, "the copy is on the clipboard as a device independent bitmap");
        if (want is null || got is null)
        {
            return;
        }

        _report.Check(w == crop.Width && h == crop.Height, $"the size of the area, {w}x{h} for {crop.Width}x{crop.Height}");
        _report.Check(bottomUp, "with its rows bottom-up, as every reader of the format expects");
        int differ = 0;
        for (int i = 0; i < want.Length && i < got.Length; i++)
        {
            if (want[i] != got[i])
            {
                differ++;
            }
        }
        _report.Check(differ == 0 && want.Length == got.Length, $"and every byte as drawn ({differ} differ)");

        _report.Check(BitmapSize(out int bw, out int bh) && bw == crop.Width && bh == crop.Height, $"a GDI bitmap of it can be had too, {bw}x{bh}");
    }

    public void CopiesLeakNothing(Capture capture)
    {
        uint before = Win32.GetGuiResources(Win32.GetCurrentProcess(), Win32.GR_GDIOBJECTS);
        for (int i = 0; i < LeakRuns; i++)
        {
            using var image = new Surface();
            if (capture.Render(image, _app.MarkPainter))
            {
                Export.ToClipboard(image, _app.Overlay.Window);
            }
        }
        uint after = Win32.GetGuiResources(Win32.GetCurrentProcess(), Win32.GR_GDIOBJECTS);
        long grew = (long)after - before;
        _report.Check(grew <= 8, $"{LeakRuns} copies leak no GDI handles ({before} before, {after} after)");
    }

    private unsafe byte[]? ReadDib(out int w, out int h, out bool bottomUp)
    {
        w = h = 0;
        bottomUp = false;
        if (!Win32.IsClipboardFormatAvailable(Win32.CF_DIB) || !Clipboard.Open(_app.Overlay.Window))
        {
            return null;
        }
        try
        {
            IntPtr global = Win32.GetClipboardData(Win32.CF_DIB);
            IntPtr p;
            if (global == IntPtr.Zero)
            {
                p = IntPtr.Zero;
            }
            else
            {
                p = Win32.GlobalLock(global);
            }
            if (p == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                var header = *(Win32.BITMAPINFOHEADER*)p;
                if (header.biBitCount != 32 || header.biCompression != Win32.BI_RGB)
                {
                    return null;
                }
                w = header.biWidth;
                bottomUp = header.biHeight > 0;
                h = Math.Abs(header.biHeight);
                if (w <= 0 || h <= 0 || (long)w * h * 4 > int.MaxValue)
                {
                    return null;
                }

                int stride = w * 4;
                var px = new byte[stride * h];
                byte* rows = (byte*)p + header.biSize;
                for (int y = 0; y < h; y++)
                {
                    int srcRow;
                    if (bottomUp)
                    {
                        srcRow = h - 1 - y;
                    }
                    else
                    {
                        srcRow = y;
                    }
                    new ReadOnlySpan<byte>(rows + (long)srcRow * stride, stride).CopyTo(px.AsSpan(y * stride));
                }
                return px;
            }
            finally
            {
                Win32.GlobalUnlock(global);
            }
        }
        finally
        {
            Win32.CloseClipboard();
        }
    }

    private bool BitmapSize(out int w, out int h)
    {
        w = h = 0;
        if (!Clipboard.Open(_app.Overlay.Window))
        {
            return false;
        }
        try
        {
            IntPtr bitmap = Win32.GetClipboardData(Win32.CF_BITMAP);
            var info = default(Win32.BITMAP);
            if (bitmap == IntPtr.Zero || Win32.GetObjectW(bitmap, Marshal.SizeOf<Win32.BITMAP>(), ref info) == 0)
            {
                return false;
            }
            w = info.bmWidth;
            h = info.bmHeight;
            return true;
        }
        finally
        {
            Win32.CloseClipboard();
        }
    }
}
