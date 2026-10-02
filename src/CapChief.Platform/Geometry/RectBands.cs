namespace CapChief.Geometry;

/// <summary>
/// Provides methods for splitting the rectangles that describe a changed screen area into bands.
/// </summary>
public static class RectBands
{
    public const int OutsideBandCount = 4;

    public const int ChangeBandCount = 4;

    public static int Outside(Rect r, Rect h, Span<Rect> into)
    {
        if (into.Length < OutsideBandCount)
        {
            throw new ArgumentException("Outside needs four bands", nameof(into));
        }
        if (h.IsEmpty)
        {
            into[0] = r;
            return 1;
        }
        into[0] = r with { Bottom = h.Top };
        into[1] = r with { Top = h.Bottom };
        into[2] = h with { Left = r.Left, Right = h.Left };
        into[3] = h with { Left = h.Right, Right = r.Right };
        return OutsideBandCount;
    }

    public static int ChangeBands(Rect a, Rect b, Span<Rect> into)
    {
        bool ea = a.IsEmpty;
        bool eb = b.IsEmpty;
        if (ea && eb)
        {
            return 0;
        }
        if (ea)
        {
            into[0] = b;
            return 1;
        }
        if (eb)
        {
            into[0] = a;
            return 1;
        }

        Rect u = Rect.Union(a, b);
        Rect i = Rect.Intersect(a, b);
        if (i.IsEmpty)
        {
            into[0] = a;
            into[1] = b;
            return 2;
        }

        int n = 0;
        if (i.Top > u.Top)
        {
            into[n++] = u with { Bottom = i.Top };
        }
        if (i.Bottom < u.Bottom)
        {
            into[n++] = u with { Top = i.Bottom };
        }
        if (i.Left > u.Left)
        {
            into[n++] = i with { Left = u.Left, Right = i.Left };
        }
        if (i.Right < u.Right)
        {
            into[n++] = i with { Left = i.Right, Right = u.Right };
        }
        return n;
    }
}
