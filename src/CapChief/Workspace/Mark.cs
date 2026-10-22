namespace CapChief.Workspace;

/// <summary>
/// Represents a single mark drawn on a screenshot, such as an arrow or a text box.
/// </summary>
public abstract record Mark
{
    protected const int PieceLength = 96, MaxPieces = 64;

    public Color Color { get; init; }
    public Point A { get; init; }
    public Point B { get; init; }

    public abstract MarkKind Kind { get; }

    public virtual Grip[] Grips
    {
        get
        {
            return RectGrips.All;
        }
    }

    public virtual bool TakesColor
    {
        get
        {
            return true;
        }
    }

    protected virtual bool AspectLocked
    {
        get
        {
            return false;
        }
    }

    public int LengthSquared
    {
        get
        {
            return (B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y);
        }
    }

    public Rect Bounds
    {
        get
        {
            return Rect.FromCorners(A.X, A.Y, B.X, B.Y);
        }
    }

    public virtual bool HasExtent
    {
        get
        {
            return LengthSquared > 0;
        }
    }

    public virtual bool CanMoveAsImage
    {
        get
        {
            return false;
        }
    }

    public virtual Mark WithBounds(Rect b)
    {
        return this with { A = b.TopLeft, B = new Point(b.Right, b.Bottom) };
    }

    public virtual Point GripCenter(Grip g)
    {
        return Bounds.GripCenter(g);
    }

    public Rect GripRect(Grip g, int gripRadius)
    {
        return Rect.Around(GripCenter(g), gripRadius);
    }

    public Grip GripAt(Point p, int gripRadius)
    {
        foreach (Grip g in Grips)
        {
            if (GripRect(g, gripRadius).Inflated(RectGrips.HitMargin).Contains(p))
            {
                return g;
            }
        }
        return Grip.None;
    }

    public virtual Point Constrained(Point anchor, Point p)
    {
        int dx = p.X - anchor.X;
        int dy = p.Y - anchor.Y;
        int s = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return new Point(anchor.X + int.CopySign(s, dx), anchor.Y + int.CopySign(s, dy));
    }

    public abstract bool IsHit(Point p, int band);

    public Mark Moved(Point offset)
    {
        return Dragged(Grip.Move, offset, false, 0);
    }

    public virtual Mark Dragged(Grip g, Point offset, bool shift, int min)
    {
        if (g == Grip.Move)
        {
            if (shift)
            {
                offset = offset.AxisLocked();
            }
            return this with { A = A + offset, B = B + offset };
        }

        Edges e = g.EdgesOf();
        Rect r = Bounds.Resized(e, offset, min, null);
        if (shift || AspectLocked)
        {
            r = r.LockedTo(Bounds, e, min, null);
        }
        return WithBounds(r);
    }

    public abstract void Draw(MarkPainter painter);

    public virtual Rect Extent(MarkSizes sizes)
    {
        return Bounds.Inflated(Pad(sizes));
    }

    public Rect SelectedExtent(MarkSizes sizes, int gripRadius)
    {
        return Rect.Union(Extent(sizes), Bounds.Inflated(gripRadius + 2));
    }

    public virtual void AddPaintedAreas(List<Rect> into, MarkSizes sizes)
    {
        into.Add(Extent(sizes));
    }

    public void AddSelectedAreas(List<Rect> into, MarkSizes sizes, int gripRadius)
    {
        AddPaintedAreas(into, sizes);
        foreach (Grip g in Grips)
        {
            into.Add(GripRect(g, gripRadius).Inflated(2));
        }
    }

    protected static int Pad(MarkSizes sizes)
    {
        return (int)MathF.Ceiling(sizes.StrokeWidth / 2) + 2;
    }

    protected static void AddFrame(List<Rect> into, Rect r, int pad)
    {
        Span<Rect> bands = stackalloc Rect[RectBands.OutsideBandCount];
        int n = RectBands.Outside(r.Inflated(pad), r.Inflated(-pad), bands);
        into.AddRange(bands[..n]);
    }

    protected static void AddSegments(List<Rect> into, ReadOnlySpan<Point> points, int pad)
    {
        for (int i = 1; i < points.Length; i++)
        {
            into.Add(Rect.FromCorners(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y).Inflated(pad));
        }
    }

    protected bool InBox(Point p)
    {
        return Bounds.Inflated(2).Contains(p);
    }

    protected static bool Thin(Rect r)
    {
        return r.Width < 2 || r.Height < 2;
    }

    protected static float Radial2(Rect r, Point p)
    {
        float rx = r.Width * 0.5f;
        float ry = r.Height * 0.5f;
        float nx = (p.X - (r.Left + rx)) / rx;
        float ny = (p.Y - (r.Top + ry)) / ry;
        return nx * nx + ny * ny;
    }

    protected static long SegDist2(Point p, Point from, Point to)
    {
        long vx = to.X - from.X;
        long vy = to.Y - from.Y;
        long wx = p.X - from.X;
        long wy = p.Y - from.Y;
        long len2 = vx * vx + vy * vy;
        double t;
        if (len2 == 0)
        {
            t = 0;
        }
        else
        {
            t = Math.Clamp((double)(wx * vx + wy * vy) / len2, 0, 1);
        }
        double dx = wx - t * vx;
        double dy = wy - t * vy;
        return (long)(dx * dx + dy * dy);
    }

    public static Mark Create(MarkKind kind)
    {
        switch (kind)
        {
            case MarkKind.Arrow:
                return new ArrowMark();
            case MarkKind.Rectangle:
                return new RectangleMark();
            case MarkKind.Mosaic:
                return new MosaicMark();
            case MarkKind.Text:
                return new TextMark();
            case MarkKind.Pen:
                return new PenMark();
            case MarkKind.Ellipse:
                return new EllipseMark();
            case MarkKind.Blackout:
                return new BlackoutMark();
            case MarkKind.Step:
                return new StepMark();
            case MarkKind.Highlighter:
                return new HighlighterMark();
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}

/// <summary>
/// Represents an arrow drawn on a screenshot, pointing from its tail to its head.
/// </summary>
public sealed record ArrowMark : Mark
{
    private static readonly Grip[] Ends = [Grip.NW, Grip.SE];

    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Arrow;
        }
    }

    public override Grip[] Grips
    {
        get
        {
            return Ends;
        }
    }

    public override bool CanMoveAsImage
    {
        get
        {
            return true;
        }
    }

    public override Point GripCenter(Grip g)
    {
        if (g == Grip.NW)
        {
            return A;
        }
        else
        {
            return B;
        }
    }

    public override Point Constrained(Point anchor, Point p)
    {
        return p.Snap45(anchor);
    }

    public override bool IsHit(Point p, int band)
    {
        return SegDist2(p, A, B) <= (long)band * band;
    }

    public override Mark Dragged(Grip g, Point offset, bool shift, int min)
    {
        if (g == Grip.Move)
        {
            return base.Dragged(g, offset, shift, min);
        }

        if (g == Grip.NW)
        {
            Point start = A + offset;
            if (shift)
            {
                start = start.Snap45(B);
            }
            return this with { A = start };
        }

        Point end = B + offset;
        if (shift)
        {
            end = end.Snap45(A);
        }
        return this with { B = end };
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Arrow(A, B, Color);
    }

    public override Rect Extent(MarkSizes sizes)
    {
        return base.Extent(sizes).Inflated((int)MathF.Ceiling(sizes.ArrowHead));
    }

    public override void AddPaintedAreas(List<Rect> into, MarkSizes sizes)
    {
        int n = Math.Clamp((int)(Math.Sqrt(LengthSquared) / PieceLength), 1, MaxPieces);
        Span<Point> points = stackalloc Point[n + 1];
        for (int i = 0; i <= n; i++)
        {
            points[i] = Point.Interpolate(A, B, i, n);
        }
        AddSegments(into, points, Pad(sizes));
        into.Add(Rect.Around(B, (int)MathF.Ceiling(sizes.ArrowHead) + Pad(sizes)));
    }
}

/// <summary>
/// Represents a freehand line drawn on a screenshot by dragging the pen.
/// </summary>
public record PenMark : Mark
{
    public Polyline? Path { get; init; }

    public int Samples
    {
        get
        {
            return Path?.Count ?? 0;
        }
    }

    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Pen;
        }
    }

    public override bool HasExtent
    {
        get
        {
            return base.HasExtent || Samples > 1;
        }
    }

    public override bool CanMoveAsImage
    {
        get
        {
            return true;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        if (Path is not { Count: > 0 } path || !Bounds.Inflated(band).Contains(p))
        {
            return false;
        }
        long b2 = (long)band * band;
        if (path.Count == 1)
        {
            return SegDist2(p, path[0], path[0]) <= b2;
        }
        for (int i = 1; i < path.Count; i++)
        {
            if (SegDist2(p, path[i - 1], path[i]) <= b2)
            {
                return true;
            }
        }
        return false;
    }

    public override Mark Dragged(Grip g, Point offset, bool shift, int min)
    {
        Mark moved = base.Dragged(g, offset, shift, min);
        return (PenMark)moved with { Path = Path?.Remapped(Bounds, moved.Bounds) };
    }

    public override void Draw(MarkPainter painter)
    {
        if (Path is { } path)
        {
            painter.Stroke(path, Color);
        }
    }

    public override Rect Extent(MarkSizes sizes)
    {
        return base.Extent(sizes).Inflated(Math.Max(Bounds.Width, Bounds.Height) / 6);
    }
}

/// <summary>
/// Represents a wide freehand stroke that tints the screenshot under it, like a highlighter pen.
/// </summary>
public sealed record HighlighterMark : PenMark
{
    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Highlighter;
        }
    }

    public override bool CanMoveAsImage
    {
        get
        {
            return false;
        }
    }

    public override void Draw(MarkPainter painter)
    {
        if (Path is { } path)
        {
            painter.Highlight(path, Color);
        }
    }

    public override Rect Extent(MarkSizes sizes)
    {
        // Unlike a pen line, highlighter ink is cut off at the box around the points, so it needs
        // no room for the curve to bow out.
        return Bounds.Inflated(Shapes.InkPad(sizes.HighlighterWidth));
    }
}

/// <summary>
/// Represents a rectangular outline drawn around part of a screenshot.
/// </summary>
public sealed record RectangleMark : Mark
{
    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Rectangle;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        return Bounds.Inflated(band).Contains(p) && !Bounds.Inflated(-band).Contains(p);
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Box(Bounds, Color);
    }

    public override void AddPaintedAreas(List<Rect> into, MarkSizes sizes)
    {
        AddFrame(into, Bounds, Pad(sizes));
    }
}

/// <summary>
/// Represents an ellipse outline drawn around part of a screenshot.
/// </summary>
public sealed record EllipseMark : Mark
{
    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Ellipse;
        }
    }

    public override bool CanMoveAsImage
    {
        get
        {
            return true;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        Rect r = Bounds;
        if (!r.Inflated(band).Contains(p))
        {
            return false;
        }
        if (Thin(r))
        {
            return true;
        }
        float minor = Math.Min(r.Width, r.Height) * 0.5f;
        return MathF.Abs(MathF.Sqrt(Radial2(r, p)) - 1f) * minor <= band;
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Ellipse(Bounds, Color);
    }

    public override void AddPaintedAreas(List<Rect> into, MarkSizes sizes)
    {
        Rect r = Bounds;
        double rx = r.Width / 2.0;
        double ry = r.Height / 2.0;
        double cx = r.Left + rx;
        double cy = r.Top + ry;
        int n = Math.Clamp((int)(Math.PI * (rx + ry) / PieceLength), 8, MaxPieces);
        int sag = (int)Math.Ceiling(Math.Max(rx, ry) * (1 - Math.Cos(Math.PI / n)));
        Span<Point> points = stackalloc Point[n + 1];
        for (int i = 0; i <= n; i++)
        {
            double t = 2 * Math.PI * i / n;
            points[i] = new Point((int)Math.Round(cx + rx * Math.Cos(t)), (int)Math.Round(cy + ry * Math.Sin(t)));
        }
        AddSegments(into, points, Pad(sizes) + sag);
    }
}

/// <summary>
/// Represents part of a screenshot hidden by pixelating it into large blocks.
/// </summary>
public sealed record MosaicMark : Mark
{
    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Mosaic;
        }
    }

    public override bool TakesColor
    {
        get
        {
            return false;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        return InBox(p);
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Pixelate(Bounds);
    }
}

/// <summary>
/// Represents part of a screenshot hidden behind a solid bar of color.
/// </summary>
public sealed record BlackoutMark : Mark
{
    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Blackout;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        return InBox(p);
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Fill(Bounds, Color);
    }
}

/// <summary>
/// Represents a box of text typed onto a screenshot.
/// </summary>
public sealed record TextMark : Mark
{
    public string? Text { get; init; }

    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Text;
        }
    }

    public override TextMark WithBounds(Rect b)
    {
        return (TextMark)base.WithBounds(b);
    }

    public override bool IsHit(Point p, int band)
    {
        return InBox(p);
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Text(Text, Bounds, Color);
    }
}

/// <summary>
/// Represents a numbered circle marking one step in a sequence on a screenshot.
/// </summary>
public sealed record StepMark : Mark
{
    private static readonly Grip[] Corners = [Grip.NW, Grip.NE, Grip.SE, Grip.SW];

    public override MarkKind Kind
    {
        get
        {
            return MarkKind.Step;
        }
    }

    public override Grip[] Grips
    {
        get
        {
            return Corners;
        }
    }

    protected override bool AspectLocked
    {
        get
        {
            return true;
        }
    }

    public override bool IsHit(Point p, int band)
    {
        Rect r = Bounds;
        if (Thin(r))
        {
            return InBox(p);
        }
        else
        {
            return Radial2(r, p) <= 1f;
        }
    }

    public override void Draw(MarkPainter painter)
    {
        painter.Disc(Bounds, Color);
    }
}

/// <summary>
/// Specifies the kind of mark drawn on a screenshot, such as an arrow or a rectangle.
/// </summary>
public enum MarkKind
{
    Arrow,
    Rectangle,
    Mosaic,
    Text,
    Pen,
    Ellipse,
    Blackout,
    Step,
    Highlighter,
}

/// <summary>
/// Stores the line widths and sizes used to draw marks at one scale.
/// </summary>
public readonly struct MarkSizes
{
    public float StrokeWidth { get; init; }

    public float HighlighterWidth { get; init; }

    public float ArrowHead { get; init; }

    public float HintWidth { get; init; }

    public int BadgeRadius { get; init; }

    public int MinSize { get; init; }

    public int RaySpacing { get; init; }

    public int RedactBlock { get; init; }
}
