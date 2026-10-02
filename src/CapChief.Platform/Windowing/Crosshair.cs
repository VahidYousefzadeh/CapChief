namespace CapChief.Windowing;

/// <summary>
/// Creates a custom crosshair mouse cursor in a specified color, sized for the display scale it is
/// shown at.
/// </summary>
public sealed class Crosshair
{
    private const int MinSize = 32;
    private const int Arm = 8;
    private const uint Shadow = 0xFF000000u;

    private readonly uint _argb;
    private readonly Dictionary<float, IntPtr> _cursors = new();

    public Crosshair(uint argb)
    {
        _argb = argb;
    }

    public CursorShape ShapeAt(float scale)
    {
        if (!_cursors.TryGetValue(scale, out IntPtr cursor))
        {
            cursor = Create(scale);
            if (cursor == IntPtr.Zero)
            {
                return CursorShape.Cross;
            }
            _cursors[scale] = cursor;
        }
        return CursorShape.Made(cursor);
    }

    public void Free()
    {
        foreach (IntPtr cursor in _cursors.Values)
        {
            Win32.DestroyIcon(cursor);
        }
        _cursors.Clear();
    }

    private unsafe IntPtr Create(float scale)
    {
        int line = Math.Max(1, (int)MathF.Round(scale, MidpointRounding.AwayFromZero));
        int arm = Math.Max(Arm, (int)MathF.Round(Arm * scale));
        int outline = arm + line;
        int size = Math.Max(MinSize, (2 * outline + line + 2 + 15) / 16 * 16);
        int hot = (size - line) / 2;
        int maskStride = size / 8;

        var andMask = new byte[maskStride * size];
        Array.Fill(andMask, (byte)0xFF);

        using var color = new Surface();
        if (!color.OpenDib(size, size, withDc: false))
        {
            return IntPtr.Zero;
        }

        uint* px = (uint*)color.Bits;
        new Span<uint>(px, size * size).Clear();

        for (int d = -outline; d < outline + line; d++)
        {
            if (d >= -line && d < 2 * line)
            {
                continue;
            }
            for (int t = 0; t < line; t++)
            {
                px[(hot - line + t) * size + hot + d] = Shadow;
                px[(hot + line + t) * size + hot + d] = Shadow;
                px[(hot + d) * size + hot - line + t] = Shadow;
                px[(hot + d) * size + hot + line + t] = Shadow;
            }
        }

        for (int d = -arm; d < arm + line; d++)
        {
            for (int t = 0; t < line; t++)
            {
                Bare(andMask, maskStride, hot + d, hot + t);
                Bare(andMask, maskStride, hot + t, hot + d);
                px[(hot + t) * size + hot + d] = _argb;
                px[(hot + d) * size + hot + t] = _argb;
            }
        }

        IntPtr hColor = color.TakeBitmap();
        IntPtr hMask = Win32.CreateBitmap(size, size, 1, 1, andMask);

        var ii = new Win32.ICONINFO
        {
            fIcon = false,
            xHotspot = (uint)hot,
            yHotspot = (uint)hot,
            hbmMask = hMask,
            hbmColor = hColor,
        };

        IntPtr cursor = Win32.CreateIconIndirect(ref ii);
        Win32.DeleteObject(hColor);
        Win32.DeleteObject(hMask);
        return cursor;
    }

    private static void Bare(byte[] mask, int stride, int x, int y)
    {
        mask[y * stride + x / 8] &= (byte)(0xFF ^ (1 << (7 - (x % 8))));
    }
}
