namespace CapChief.Input;

/// <summary>
/// Represents the area of the screen a capture keeps, never smaller than a minimum size.
/// </summary>
public readonly record struct Crop
{
    public const int MinSize = 10;

    private Crop(Rect bounds)
    {
        Bounds = bounds;
    }

    public static implicit operator Rect(Crop crop)
    {
        return crop.Bounds;
    }

    public Rect Bounds { get; }

    public int Width
    {
        get
        {
            return Bounds.Width;
        }
    }

    public int Height
    {
        get
        {
            return Bounds.Height;
        }
    }

    public Point TopLeft
    {
        get
        {
            return Bounds.TopLeft;
        }
    }

    public Point Center
    {
        get
        {
            return Bounds.Center;
        }
    }

    public static bool TryCreate(Rect bounds, out Crop crop)
    {
        bool bigEnough = bounds.Width >= MinSize && bounds.Height >= MinSize;
        if (bigEnough)
        {
            crop = new Crop(bounds);
        }
        else
        {
            crop = default;
        }
        return bigEnough;
    }

    public bool Contains(Point p)
    {
        return Bounds.Contains(p);
    }

    public Grip GripAt(Point p, int band, int gripRadius)
    {
        foreach (Grip g in RectGrips.All)
        {
            if (Bounds.GripRect(g, gripRadius).Inflated(RectGrips.HitMargin).Contains(p))
            {
                return g;
            }
        }
        if (Bounds.Inflated(band).Contains(p) && !Bounds.Inflated(-band).Contains(p))
        {
            return Grip.Move;
        }
        else
        {
            return Grip.None;
        }
    }

    public Point OffsetTo(Crop to)
    {
        if (to.Width == Width && to.Height == Height)
        {
            return to.TopLeft - TopLeft;
        }
        else
        {
            return default;
        }
    }

    public Crop Dragged(Grip g, Point offset, bool shift, Rect bounds)
    {
        if (g == Grip.Move)
        {
            Point moveBy;
            if (shift)
            {
                moveBy = offset.AxisLocked();
            }
            else
            {
                moveBy = offset;
            }
            return new Crop(Rect.Within(Bounds.Moved(moveBy), bounds));
        }
        Edges e = g.EdgesOf();
        Rect r = Bounds.Resized(e, offset, MinSize, bounds);
        if (shift)
        {
            r = r.LockedTo(Bounds, e, MinSize, bounds);
        }
        if (TryCreate(r, out Crop dragged))
        {
            return dragged;
        }
        else
        {
            return this;
        }
    }

    public Crop SnapTo(LineFinder lines, Edges edges, int reach)
    {
        Rect r = Bounds;
        Rect snapped = r;
        if (edges.IsWest())
        {
            snapped.Left = lines.NearestLine(Edges.West, r.Left, r.Top, r.Bottom, reach) ?? r.Left;
        }
        if (edges.IsEast())
        {
            snapped.Right = lines.NearestLine(Edges.East, r.Right, r.Top, r.Bottom, reach) ?? r.Right;
        }
        if (edges.IsNorth())
        {
            snapped.Top = lines.NearestLine(Edges.North, r.Top, r.Left, r.Right, reach) ?? r.Top;
        }
        if (edges.IsSouth())
        {
            snapped.Bottom = lines.NearestLine(Edges.South, r.Bottom, r.Left, r.Right, reach) ?? r.Bottom;
        }
        if (TryCreate(snapped, out Crop crop))
        {
            return crop;
        }
        else
        {
            return this;
        }
    }
}
