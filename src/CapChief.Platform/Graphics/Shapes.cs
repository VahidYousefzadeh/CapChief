namespace CapChief.Graphics;

/// <summary>
/// Draws arrows, boxes, ellipses, freehand strokes, highlighter strokes, pixelated areas and labeled
/// discs on a surface.
/// </summary>
public sealed class Shapes
{
    private const int StrokeInitial = 256;

    private const float CurveTension = 0.5f;

    private const double BarbSpread = Math.PI / 6;

    private const uint White = 0x00FFFFFFu;

    private readonly Surface _ink = new();
    private readonly Surface _under = new();

    private PointF[] _strokeBuf = new PointF[StrokeInitial];

    public void DrawArrow(DeviceContext dc, int x1, int y1, int x2, int y2, uint colorRef, float width, float headLen)
    {
        using var g = new Graphic(dc);
        using var pen = new Pen(Argb.ToArgb(colorRef), width);

        Barbs(x1, y1, x2, y2, headLen, out Point left, out Point right);
        Win32.GdipDrawLineI(g, pen, x1, y1, x2, y2);
        Win32.GdipDrawLineI(g, pen, x2, y2, left.X, left.Y);
        Win32.GdipDrawLineI(g, pen, x2, y2, right.X, right.Y);
    }

    private static void Barbs(int x1, int y1, int x2, int y2, double len, out Point left, out Point right)
    {
        double ang = Math.Atan2(y2 - y1, x2 - x1);
        left = BarbEnd(x2, y2, ang - BarbSpread, len);
        right = BarbEnd(x2, y2, ang + BarbSpread, len);
    }

    private static Point BarbEnd(int x, int y, double ang, double len)
    {
        return new((int)Math.Round(x - len * Math.Cos(ang)), (int)Math.Round(y - len * Math.Sin(ang)));
    }

    public void DrawBox(DeviceContext dc, Rect r, uint colorRef, float width)
    {
        r = Rect.FromCorners(r.Left, r.Top, r.Right, r.Bottom);
        int o = (int)MathF.Round(width / 2f);
        int i = Math.Max(1, (int)MathF.Round(width)) - o;
        var outside = new Rect
        {
            Left = r.Left - o,
            Top = r.Top - o,
            Right = r.Right + i,
            Bottom = r.Bottom + i,
        };
        var inside = new Rect
        {
            Left = r.Left + i,
            Top = r.Top + i,
            Right = r.Right - o,
            Bottom = r.Bottom - o,
        };
        Span<Rect> strips = stackalloc Rect[RectBands.OutsideBandCount];
        int n = RectBands.Outside(outside, inside, strips);
        for (int k = 0; k < n; k++)
        {
            dc.FillRect(strips[k], colorRef);
        }
    }

    public void DrawEllipse(DeviceContext dc, Rect r, uint colorRef, float width)
    {
        r = Rect.FromCorners(r.Left, r.Top, r.Right, r.Bottom);

        using var g = new Graphic(dc);
        using var pen = new Pen(Argb.ToArgb(colorRef), width, rounded: false);
        Win32.GdipDrawEllipse(g, pen, r.Left, r.Top, r.Width, r.Height);
    }

    public void DrawStroke(DeviceContext dc, ReadOnlySpan<Point> pts, Point offset, uint colorRef, float width)
    {
        int n = pts.Length;
        if (n < 1)
        {
            return;
        }
        if (_strokeBuf.Length < n)
        {
            _strokeBuf = new PointF[Math.Max(n, _strokeBuf.Length * 2)];
        }
        for (int i = 0; i < n; i++)
        {
            _strokeBuf[i] = new PointF(pts[i].X + offset.X, pts[i].Y + offset.Y);
        }

        using var g = new Graphic(dc);
        if (n == 1)
        {
            Dot(g, colorRef, width);
            return;
        }

        using var pen = new Pen(Argb.ToArgb(colorRef), width);
        using var p = new Outline(winding: true);
        if (n >= 3)
        {
            Win32.GdipAddPathCurve2(p, _strokeBuf, n, CurveTension);
        }
        else
        {
            Win32.GdipAddPathLine2(p, _strokeBuf, n);
        }
        Win32.GdipDrawPath(g, pen, p);
    }

    private void Dot(IntPtr g, uint colorRef, float width)
    {
        using var br = new Brush(Argb.ToArgb(colorRef));
        float hr = width * 0.5f;
        Win32.GdipFillEllipse(g, br, _strokeBuf[0].X - hr, _strokeBuf[0].Y - hr, width, width);
    }

    /// <summary>
    /// Multiplies what is under the stroke by its color, like highlighter ink: text stays readable
    /// and a stroke that crosses itself does not get darker where it overlaps.
    /// </summary>
    public void DrawHighlight(DeviceContext dc, ReadOnlySpan<Point> pts, Point offset, uint colorRef, float width)
    {
        if (pts.Length < 1 || Win32.GetClipBox(dc, out Rect clip) <= Win32.NULLREGION)
        {
            return;
        }
        Rect box = Rect.Intersect(StrokeBox(pts, offset, InkPad(width)), clip);
        if (box.IsEmpty || !_ink.EnsureAtLeast(box.Width, box.Height))
        {
            return;
        }
        PaintInk(_ink, box.Width, box.Height, pts, offset - box.TopLeft, colorRef, width);
        Tint(dc, _ink, box);
    }

    /// <summary>
    /// Draws a highlighter stroke's ink once, so a stroke that only moves can tint what is under it
    /// at each step without being drawn again.
    /// </summary>
    public void DrawInk(Ink ink, ReadOnlySpan<Point> pts, uint colorRef, float width)
    {
        ink.Free();
        if (pts.Length < 1)
        {
            return;
        }
        Rect box = StrokeBox(pts, default, InkPad(width));
        if (!ink.Pixels.OpenDib(box.Width, box.Height))
        {
            return;
        }
        ink.Box = box;
        PaintInk(ink.Pixels, box.Width, box.Height, pts, -box.TopLeft, colorRef, width);
    }

    public void Tint(DeviceContext dc, Ink ink, Point offset)
    {
        if (ink.Pixels.IsOpen)
        {
            Tint(dc, ink.Pixels, ink.Box.Moved(offset));
        }
    }

    /// <summary>
    /// Gives how far highlighter ink reaches past the box around the stroke's points.
    /// </summary>
    public static int InkPad(float width)
    {
        return (int)MathF.Ceiling(width / 2) + 2;
    }

    private void PaintInk(Surface ink, int w, int h, ReadOnlySpan<Point> pts, Point offset, uint colorRef, float width)
    {
        ink.DeviceContext.FillRect(Rect.At(0, 0, w, h), White);
        DrawStroke(ink.DeviceContext, pts, offset, colorRef, width);
    }

    /// <summary>
    /// Multiplies what is under the rectangle by the ink, whose top left pixel lies on the
    /// rectangle's top left corner.
    /// </summary>
    private void Tint(DeviceContext dc, Surface ink, Rect at)
    {
        if (Win32.GetClipBox(dc, out Rect clip) <= Win32.NULLREGION)
        {
            return;
        }
        Rect box = Rect.Intersect(at, clip);
        if (box.IsEmpty || !_under.EnsureAtLeast(box.Width, box.Height))
        {
            return;
        }

        _under.DeviceContext.Blit(Rect.At(0, 0, box.Width, box.Height), dc, box.Left, box.Top);
        DeviceContext.Flush();
        int skipped = ((box.Top - at.Top) * ink.Width + box.Left - at.Left) * 4;
        Multiply(_under.Pixels, _under.Width * 4, ink.Pixels[skipped..], ink.Width * 4, box.Width, box.Height);
        dc.Blit(box, _under.DeviceContext, 0, 0);
    }

    private static Rect StrokeBox(ReadOnlySpan<Point> pts, Point offset, int pad)
    {
        var box = new Rect
        {
            Left = pts[0].X,
            Top = pts[0].Y,
            Right = pts[0].X,
            Bottom = pts[0].Y,
        };
        foreach (Point p in pts)
        {
            box.Left = Math.Min(box.Left, p.X);
            box.Top = Math.Min(box.Top, p.Y);
            box.Right = Math.Max(box.Right, p.X);
            box.Bottom = Math.Max(box.Bottom, p.Y);
        }
        return box.Moved(offset).Inflated(pad);
    }

    private static void Multiply(Span<byte> under, int underStride, ReadOnlySpan<byte> ink, int inkStride, int w, int h)
    {
        for (int y = 0; y < h; y++)
        {
            Span<byte> row = under.Slice(y * underStride, w * 4);
            ReadOnlySpan<byte> tint = ink.Slice(y * inkStride, w * 4);
            for (int i = 0; i < row.Length; i += 4)
            {
                row[i] = (byte)((row[i] * tint[i] + 127) / 255);
                row[i + 1] = (byte)((row[i + 1] * tint[i + 1] + 127) / 255);
                row[i + 2] = (byte)((row[i + 2] * tint[i + 2] + 127) / 255);
            }
        }
    }

    public void Pixelate(DeviceContext dc, DeviceContext from, Rect dst, Rect src, int block)
    {
        if (!from.Exists || dst.IsEmpty || block < 1)
        {
            return;
        }
        int bw = Math.Max(1, src.Width / block);
        int bh = Math.Max(1, src.Height / block);

        using var small = new Surface();
        if (!small.OpenCompatible(bw, bh))
        {
            return;
        }
        small.DeviceContext.StretchBlit(small.Bounds, from, src);

        int oldMode = Win32.SetStretchBltMode(dc, Win32.COLORONCOLOR);
        Win32.StretchBlt(dc, dst.Left, dst.Top, dst.Width, dst.Height, small.Dc, 0, 0, bw, bh, Win32.SRCCOPY);
        if (oldMode != 0)
        {
            Win32.SetStretchBltMode(dc, oldMode);
        }
    }

    public void DrawDisc(DeviceContext dc, Rect r, uint colorRef, string label, uint labelColorRef, TextFont font)
    {
        if (r.IsEmpty)
        {
            return;
        }

        using (var g = new Graphic(dc))
        {
            Win32.GdipSetPixelOffsetMode(g, Win32.PixelOffsetModeHighQuality);
            using var br = new Brush(Argb.ToArgb(colorRef));
            Win32.GdipFillEllipse(g, br, r.Left, r.Top, r.Width, r.Height);
            if (g.IsBorrowed)
            {
                Win32.GdipSetPixelOffsetMode(g, Win32.PixelOffsetModeDefault);
            }
        }

        CenteredText.Draw(dc, r, label, font, labelColorRef);
    }
}
