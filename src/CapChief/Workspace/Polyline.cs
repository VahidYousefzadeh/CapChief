namespace CapChief.Workspace;

/// <summary>
/// Represents the sequence of points that make up a freehand pen stroke.
/// </summary>
public sealed class Polyline : IEquatable<Polyline>
{
    private readonly Point[] _points;

    public Polyline(params Point[] points)
    {
        _points = [.. points];
    }

    public int Count
    {
        get
        {
            return _points.Length;
        }
    }

    public Point this[int i]
    {
        get
        {
            return _points[i];
        }
    }

    public ReadOnlySpan<Point> Points
    {
        get
        {
            return _points;
        }
    }

    public Polyline Add(Point p)
    {
        return new([.. _points, p]);
    }

    public Polyline AddRange(ReadOnlySpan<Point> points)
    {
        return new([.. _points, .. points]);
    }

    public Polyline Take(int count)
    {
        return new(_points[..Math.Clamp(count, 0, _points.Length)]);
    }

    public Rect Box(int from)
    {
        if (from < 0 || from >= _points.Length)
        {
            return default;
        }
        int l = _points[from].X;
        int t = _points[from].Y;
        int r = l;
        int b = t;
        foreach (Point p in _points.AsSpan(from + 1))
        {
            l = Math.Min(l, p.X);
            r = Math.Max(r, p.X);
            t = Math.Min(t, p.Y);
            b = Math.Max(b, p.Y);
        }
        return new() { Left = l, Top = t, Right = r, Bottom = b };
    }

    public Polyline Remapped(Rect from, Rect to)
    {
        double sx;
        if (from.Width > 0)
        {
            sx = (double)to.Width / from.Width;
        }
        else
        {
            sx = 1.0;
        }
        double sy;
        if (from.Height > 0)
        {
            sy = (double)to.Height / from.Height;
        }
        else
        {
            sy = 1.0;
        }
        var dst = new Point[_points.Length];
        for (int i = 0; i < _points.Length; i++)
        {
            dst[i] = new Point(to.Left + (int)Math.Round((_points[i].X - from.Left) * sx), to.Top + (int)Math.Round((_points[i].Y - from.Top) * sy));
        }
        return new(dst);
    }

    public bool Equals(Polyline? other)
    {
        return other is not null && _points.AsSpan().SequenceEqual(other._points);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Polyline);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (Point p in _points)
        {
            hash.Add(p.X);
            hash.Add(p.Y);
        }
        return hash.ToHashCode();
    }
}
