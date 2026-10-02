namespace CapChief.Graphics;

/// <summary>
/// Represents an in-memory bitmap with an alpha channel that GDI+ draws on with anti-aliasing.
/// </summary>
public sealed class AlphaSurface
{
    private const long MaxPx = 64L << 20;

    private readonly Surface _surf = new();
    private readonly Sheet _sheet = new();
    private readonly bool _highQualityPixelOffset;

    public AlphaSurface(bool highQualityPixelOffset = false)
    {
        _highQualityPixelOffset = highQualityPixelOffset;
    }

    public DeviceContext DeviceContext
    {
        get
        {
            return new(_surf.Dc, _sheet.Graphics);
        }
    }

    public Surface Surface
    {
        get
        {
            return _surf;
        }
    }

    public int Width
    {
        get
        {
            return _surf.Width;
        }
    }

    public int Height
    {
        get
        {
            return _surf.Height;
        }
    }

    public bool IsOpen
    {
        get
        {
            return _surf.IsOpen;
        }
    }

    public bool Open(int w, int h)
    {
        Free();
        if (w <= 0 || h <= 0 || (long)w * h > MaxPx)
        {
            return false;
        }
        if (!_surf.OpenDib(w, h))
        {
            return false;
        }
        Clear();
        if (_sheet.Open(_surf, _highQualityPixelOffset))
        {
            return true;
        }
        Free();
        return false;
    }

    public unsafe void Clear()
    {
        if (!_surf.IsOpen)
        {
            return;
        }
        Win32.GdiFlush();
        new Span<uint>((void*)_surf.Bits, Width * Height).Clear();
    }

    public void Seal()
    {
        if (!_sheet.IsOpen)
        {
            return;
        }
        _sheet.Flush();
        _sheet.Free();
    }

    public void Free()
    {
        _sheet.Free();
        _surf.Free();
    }

    public unsafe void Opaque(Rect r)
    {
        if (!_surf.IsOpen)
        {
            return;
        }
        Win32.GdiFlush();
        Rect c = Rect.Intersect(r, _surf.Bounds);
        for (int y = c.Top; y < c.Bottom; y++)
        {
            byte* row = (byte*)_surf.Bits + (long)y * _surf.Width * 4;
            for (int x = c.Left; x < c.Right; x++)
            {
                row[x * 4 + 3] = 0xFF;
            }
        }
    }
}
