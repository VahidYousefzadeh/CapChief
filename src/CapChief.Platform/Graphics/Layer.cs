namespace CapChief.Graphics;

/// <summary>
/// Represents the screen-sized bitmap that a layered window displays on the screen.
/// </summary>
public sealed class Layer
{
    // The lowest alpha at which a layered window still catches the pointer while looking clear.
    public const byte ClickableAlpha = 1;

    private readonly Surface _pixels = new();
    private Rect _screen;
    private bool _partialUpdatesWork = true;

    private byte _outsideAlpha = ClickableAlpha;

    public bool IsOpen
    {
        get
        {
            return _pixels.IsOpen;
        }
    }

    public DeviceContext DeviceContext
    {
        get
        {
            return _pixels.DeviceContext;
        }
    }

    public void Open(Rect screen)
    {
        _screen = screen;
        _pixels.OpenDib(_screen.Width, _screen.Height);
    }

    public void Close()
    {
        _pixels.Free();
    }

    public unsafe void ClearAll(byte outsideAlpha)
    {
        if (!_pixels.IsOpen)
        {
            return;
        }
        _outsideAlpha = outsideAlpha;
        new Span<uint>((void*)_pixels.Bits, _screen.Width * _screen.Height).Fill((uint)outsideAlpha << 24);
    }

    public void Restore(Rect r, Rect hole)
    {
        Rect inside = Rect.Intersect(r, hole);
        Span<Rect> bands = stackalloc Rect[RectBands.OutsideBandCount];
        int n = RectBands.Outside(r, inside, bands);
        Erase(bands[..n]);
        if (!inside.IsEmpty)
        {
            Fill(inside, 0, ClickableAlpha);
        }
    }

    public unsafe void Fill(Rect r, uint colorRef, byte a)
    {
        Rect c = Rect.Intersect(r, _pixels.Bounds);
        if (c.IsEmpty || !_pixels.IsOpen)
        {
            return;
        }
        byte b = (byte)(((colorRef >> 16) & 0xFF) * a / 255);
        byte g = (byte)(((colorRef >> 8) & 0xFF) * a / 255);
        byte rr = (byte)((colorRef & 0xFF) * a / 255);
        byte* p = (byte*)_pixels.Bits;
        for (int y = c.Top; y < c.Bottom; y++)
        {
            byte* row = p + (long)y * _screen.Width * 4;
            for (int x = c.Left; x < c.Right; x++)
            {
                byte* q = row + (long)x * 4;
                q[0] = b;
                q[1] = g;
                q[2] = rr;
                q[3] = a;
            }
        }
    }

    public unsafe void DrawOver(Surface src, Rect r, Rect hole, bool alphaOnly)
    {
        for (int y = 0; y < r.Height; y++)
        {
            int dy = r.Top + y;
            if (dy < 0 || dy >= _screen.Height)
            {
                continue;
            }
            bool rowInHole = dy >= hole.Top && dy < hole.Bottom;
            byte* srow = (byte*)src.Bits + (long)y * src.Width * 4;
            byte* drow = (byte*)_pixels.Bits + (long)dy * _screen.Width * 4;
            for (int x = 0; x < r.Width; x++)
            {
                int dx = r.Left + x;
                if (dx < 0 || dx >= _screen.Width)
                {
                    continue;
                }
                uint px = ((uint*)srow)[x];
                int sa = (int)(px >> 24);
                int ba;
                if (rowInHole && dx >= hole.Left && dx < hole.Right)
                {
                    ba = ClickableAlpha;
                }
                else
                {
                    ba = _outsideAlpha;
                }
                int a = sa + ba * (255 - sa) / 255;
                if (alphaOnly)
                {
                    drow[(long)dx * 4 + 3] = (byte)a;
                }
                else
                {
                    ((uint*)drow)[dx] = ((uint)a << 24) | (px & 0x00FFFFFFu);
                }
            }
        }
    }

    public void Erase(Rect r)
    {
        Fill(r, 0, _outsideAlpha);
    }

    public void Erase(ReadOnlySpan<Rect> rs)
    {
        foreach (Rect r in rs)
        {
            Erase(r);
        }
    }

    public int BorderBands(Rect r, int th, uint colorRef, byte a, Span<Rect> into)
    {
        int n = RectBands.Outside(r, r.Inflated(-Math.Max(1, th)), into);
        foreach (Rect band in into[..n])
        {
            Fill(band, colorRef, a);
        }
        return n;
    }

    public unsafe void Push(IntPtr owner, ReadOnlySpan<Rect> dirty)
    {
        if (!_pixels.IsOpen || dirty.Length == 0)
        {
            return;
        }
        if (!_partialUpdatesWork)
        {
            Push(owner);
            return;
        }
        Placement place = WholeLayer();
        using var sdc = ScreenDc.Borrow();
        bool ok = true;
        for (int i = 0; i < dirty.Length && ok; i++)
        {
            Rect d = Rect.Intersect(dirty[i], _pixels.Bounds);
            if (d.IsEmpty)
            {
                continue;
            }
            var info = new Win32.UPDATELAYEREDWINDOWINFO
            {
                cbSize = (uint)sizeof(Win32.UPDATELAYEREDWINDOWINFO),
                hdcDst = sdc.Dc,
                pptDst = (IntPtr)(&place.Dst),
                psize = (IntPtr)(&place.Size),
                hdcSrc = _pixels.Dc,
                pptSrc = (IntPtr)(&place.Src),
                pblend = (IntPtr)(&place.Blend),
                dwFlags = Win32.ULW_ALPHA,
                prcDirty = (IntPtr)(&d),
            };
            ok = Win32.UpdateLayeredWindowIndirect(owner, ref info);
        }
        if (ok)
        {
            return;
        }
        _partialUpdatesWork = false;
        Push(owner);
    }

    public void Push(IntPtr owner)
    {
        if (!_pixels.IsOpen)
        {
            return;
        }
        Placement place = WholeLayer();
        using var sdc = ScreenDc.Borrow();
        Win32.UpdateLayeredWindow(owner, sdc.Dc, ref place.Dst, ref place.Size, _pixels.Dc, ref place.Src, 0, ref place.Blend, Win32.ULW_ALPHA);
    }

    public byte[]? ReadPixels(Rect r)
    {
        return _pixels.ReadPixels(r);
    }

    private Placement WholeLayer()
    {
        return new()
        {
            Dst = _screen.TopLeft,
            Size = _screen.Size,
            Src = new(0, 0),
            Blend = Win32.BLENDFUNCTION.Over,
        };
    }

    /// <summary>
    /// Stores the position, size and blend settings used to update a layered window.
    /// </summary>
    private struct Placement
    {
        public Point Dst;
        public Size Size;
        public Point Src;
        public Win32.BLENDFUNCTION Blend;
    }
}
