using System.Runtime.InteropServices;

namespace CapChief.Geometry;

/// <summary>
/// Represents a location on the screen, or a movement across it, as integer X and Y coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Point(int X, int Y)
{
    public static Point operator +(Point a, Point b)
    {
        return new(a.X + b.X, a.Y + b.Y);
    }

    public static Point operator -(Point a, Point b)
    {
        return new(a.X - b.X, a.Y - b.Y);
    }

    public static Point operator -(Point p)
    {
        return new(-p.X, -p.Y);
    }

    public static Point Interpolate(Point from, Point to, int step, int steps)
    {
        return new(from.X + (to.X - from.X) * step / steps, from.Y + (to.Y - from.Y) * step / steps);
    }

    public Point AxisLocked()
    {
        if (Math.Abs(X) >= Math.Abs(Y))
        {
            return new(X, 0);
        }
        else
        {
            return new(0, Y);
        }
    }

    public Point Snap45(Point anchor)
    {
        int dx = X - anchor.X;
        int dy = Y - anchor.Y;
        double step = Math.PI / 4;
        double ang = Math.Round(Math.Atan2(dy, dx) / step) * step;
        double len = Math.Sqrt((double)dx * dx + (double)dy * dy);
        return new(anchor.X + (int)Math.Round(len * Math.Cos(ang)), anchor.Y + (int)Math.Round(len * Math.Sin(ang)));
    }
}
