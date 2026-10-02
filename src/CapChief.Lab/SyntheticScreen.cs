namespace CapChief.Lab;

/// <summary>
/// Provides a screen with known content, so a capture looks the same on every run and machine.
/// </summary>
public sealed class SyntheticScreen : ScreenSource
{
    private const int TickMs = 100;

    private readonly ScreenSource _real;
    private readonly Rect _screen;
    private readonly Surface _made = new();

    public SyntheticScreen(Desktop desk)
    {
        _real = new DesktopScreen(desk);
        _screen = desk.ScreenBounds;
    }

    public bool On { get; set; }

    public override bool Capture(Surface dst)
    {
        if (On)
        {
            return BlitInto(dst, new Rect { Right = _screen.Width, Bottom = _screen.Height });
        }
        return _real.Capture(dst);
    }

    public override void CaptureWithPointer(Surface dst, Rect area)
    {
        if (!On)
        {
            _real.CaptureWithPointer(dst, area);
            return;
        }
        BlitInto(dst, area);
        DeviceContext.Flush();
    }

    public override int ElapsedMs(long from, long now)
    {
        if (On)
        {
            return TickMs;
        }
        else
        {
            return _real.ElapsedMs(from, now);
        }
    }

    public bool BlitInto(Surface dst, Rect src)
    {
        if (!Made())
        {
            return false;
        }
        dst.DeviceContext.Blit(new Rect { Right = src.Width, Bottom = src.Height }, _made.DeviceContext, src.Left, src.Top);
        return true;
    }

    public void Free()
    {
        _made.Free();
    }

    private bool Made()
    {
        if (_made.IsOpen)
        {
            return true;
        }
        if (!_made.OpenDib(_screen.Width, _screen.Height))
        {
            return false;
        }
        Span<byte> p = _made.Pixels;
        int w = _screen.Width;
        int h = _screen.Height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;
                float v = (float)y / h;
                int b = (int)(38f + 24f * (1f - u));
                int g = (int)(56f + 42f * v);
                int r = (int)(10f + 26f * u);
                if (((x >> 2) + (y >> 2) & 1) == 0)
                {
                    b += 44;
                    g += 22;
                    r += 38;
                }
                if (x % 128 < 12)
                {
                    b = 0x20;
                    g = 0xC8;
                    r = 0xF0;
                }
                if (y % 96 < 8)
                {
                    b = 0xE0;
                    g = 0x40;
                    r = 0x30;
                }
                int tileX = x % 512;
                int tileY = y % 384;
                if ((tileX > 300 && tileX < 460) && (tileY > 220 && tileY < 340))
                {
                    b = 0xF4;
                    g = 0xF4;
                    r = 0xF4;
                }
                int o = (y * w + x) * 4;
                p[o] = (byte)Math.Min(b, 255);
                p[o + 1] = (byte)Math.Min(g, 255);
                p[o + 2] = (byte)Math.Min(r, 255);
                p[o + 3] = 255;
            }
        }
        return true;
    }
}
