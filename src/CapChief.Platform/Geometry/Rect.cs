using System.Runtime.InteropServices;

namespace CapChief.Geometry;

/// <summary>
/// Represents a rectangle defined by its left, top, right and bottom coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public record struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
    public readonly int Width
    {
        get
        {
            return Right - Left;
        }
    }

    public readonly int Height
    {
        get
        {
            return Bottom - Top;
        }
    }

    public readonly bool IsEmpty
    {
        get
        {
            return Width <= 0 || Height <= 0;
        }
    }

    public readonly Point Center
    {
        get
        {
            return new(Left + Width / 2, Top + Height / 2);
        }
    }

    public readonly Point TopLeft
    {
        get
        {
            return new(Left, Top);
        }
    }

    public readonly Size Size
    {
        get
        {
            return new(Width, Height);
        }
    }

    private static Rect Unbounded
    {
        get
        {
            return new()
            {
                Left = int.MinValue,
                Top = int.MinValue,
                Right = int.MaxValue,
                Bottom = int.MaxValue,
            };
        }
    }

    public readonly bool Contains(Point p)
    {
        return p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom;
    }

    public readonly bool Overlaps(Rect o)
    {
        return Left < o.Right && o.Left < Right && Top < o.Bottom && o.Top < Bottom;
    }

    public readonly Rect Inflated(int d)
    {
        return new() { Left = Left - d, Top = Top - d, Right = Right + d, Bottom = Bottom + d };
    }

    public readonly Rect Moved(Point offset)
    {
        return new()
        {
            Left = Left + offset.X,
            Top = Top + offset.Y,
            Right = Right + offset.X,
            Bottom = Bottom + offset.Y,
        };
    }

    public static Rect At(int x, int y, int width, int height)
    {
        return new() { Left = x, Top = y, Right = x + width, Bottom = y + height };
    }

    public static Rect FromCorners(int x1, int y1, int x2, int y2)
    {
        return new()
        {
            Left = Math.Min(x1, x2),
            Top = Math.Min(y1, y2),
            Right = Math.Max(x1, x2),
            Bottom = Math.Max(y1, y2),
        };
    }

    public static Rect Union(Rect a, Rect b)
    {
        return new()
        {
            Left = Math.Min(a.Left, b.Left),
            Top = Math.Min(a.Top, b.Top),
            Right = Math.Max(a.Right, b.Right),
            Bottom = Math.Max(a.Bottom, b.Bottom),
        };
    }

    public static Rect UnionIgnoringEmpty(Rect a, Rect b)
    {
        if (a.IsEmpty)
        {
            return b;
        }
        else if (b.IsEmpty)
        {
            return a;
        }
        else
        {
            return Union(a, b);
        }
    }

    public static Rect Intersect(Rect a, Rect b)
    {
        return new()
        {
            Left = Math.Max(a.Left, b.Left),
            Top = Math.Max(a.Top, b.Top),
            Right = Math.Min(a.Right, b.Right),
            Bottom = Math.Min(a.Bottom, b.Bottom),
        };
    }

    public static Rect Around(Point c, int r)
    {
        return new() { Left = c.X - r, Top = c.Y - r, Right = c.X + r, Bottom = c.Y + r };
    }

    public static Rect SmallestContaining(ReadOnlySpan<Rect> rects, Point p)
    {
        Rect best = default;
        long bestArea = long.MaxValue;
        foreach (Rect r in rects)
        {
            if (!r.Contains(p))
            {
                continue;
            }
            long area = (long)r.Width * r.Height;
            if (area >= bestArea)
            {
                continue;
            }
            best = r;
            bestArea = area;
        }
        return best;
    }

    public static Rect Within(Rect box, Rect bounds)
    {
        int x = Math.Clamp(box.Left, bounds.Left, Math.Max(bounds.Left, bounds.Right - box.Width));
        int y = Math.Clamp(box.Top, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - box.Height));
        return At(x, y, box.Width, box.Height);
    }

    public readonly Point AnchorOf(Edges e)
    {
        int x;
        if (e.IsWest())
        {
            x = Left;
        }
        else if (e.IsEast())
        {
            x = Right;
        }
        else
        {
            x = (Left + Right) / 2;
        }

        int y;
        if (e.IsNorth())
        {
            y = Top;
        }
        else if (e.IsSouth())
        {
            y = Bottom;
        }
        else
        {
            y = (Top + Bottom) / 2;
        }

        return new(x, y);
    }

    public readonly Rect Resized(Edges e, Point offset, int min, Rect? bounds)
    {
        Rect r = this;
        Rect b = bounds ?? Unbounded;
        if (e.IsWest())
        {
            r.Left = Math.Clamp(r.Left + offset.X, b.Left, Math.Max(b.Left, r.Right - min));
        }
        if (e.IsEast())
        {
            r.Right = Math.Clamp(r.Right + offset.X, Math.Min(b.Right, r.Left + min), b.Right);
        }
        if (e.IsNorth())
        {
            r.Top = Math.Clamp(r.Top + offset.Y, b.Top, Math.Max(b.Top, r.Bottom - min));
        }
        if (e.IsSouth())
        {
            r.Bottom = Math.Clamp(r.Bottom + offset.Y, Math.Min(b.Bottom, r.Top + min), b.Bottom);
        }
        return r;
    }

    public readonly Rect LockedTo(Rect orig, Edges e, int min, Rect? fit)
    {
        Rect r = this;
        if (orig.IsEmpty)
        {
            return r;
        }

        bool west = e.IsWest();
        bool east = e.IsEast();
        bool north = e.IsNorth();
        bool south = e.IsSouth();
        bool horiz = west || east;
        bool vert = north || south;

        double s;
        if (horiz && vert)
        {
            s = (r.Width * (double)orig.Width + r.Height * (double)orig.Height)
                / ((double)orig.Width * orig.Width + (double)orig.Height * orig.Height);
        }
        else if (horiz)
        {
            s = r.Width / (double)orig.Width;
        }
        else
        {
            s = r.Height / (double)orig.Height;
        }

        double lo = Math.Max(min / (double)orig.Width, min / (double)orig.Height);
        if (s < lo)
        {
            s = lo;
        }

        if (fit is { } f)
        {
            int spanX = Room(west, east, r.Left, r.Right, f.Left, f.Right);
            int spanY = Room(north, south, r.Top, r.Bottom, f.Top, f.Bottom);
            double hi = Math.Min(spanX / (double)orig.Width, spanY / (double)orig.Height);
            if (s > hi)
            {
                s = Math.Max(hi, lo);
            }
        }

        int w = (int)Math.Round(orig.Width * s);
        int h = (int)Math.Round(orig.Height * s);
        (r.Left, r.Right) = Placed(west, east, r.Left, r.Right, w);
        (r.Top, r.Bottom) = Placed(north, south, r.Top, r.Bottom, h);
        return r;
    }

    private static int Room(bool low, bool high, int lo, int hi, int fitLo, int fitHi)
    {
        if (low)
        {
            return hi - fitLo;
        }
        else if (high)
        {
            return fitHi - lo;
        }
        else
        {
            int mid = (lo + hi) / 2;
            return 2 * Math.Min(mid - fitLo, fitHi - mid);
        }
    }

    private static (int Lo, int Hi) Placed(bool low, bool high, int lo, int hi, int size)
    {
        if (low)
        {
            return (hi - size, hi);
        }
        else if (high)
        {
            return (lo, lo + size);
        }
        else
        {
            int mid = (lo + hi) / 2;
            int start = mid - size / 2;
            return (start, start + size);
        }
    }
}
