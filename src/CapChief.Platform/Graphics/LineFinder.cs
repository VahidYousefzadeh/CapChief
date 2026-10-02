using System.Runtime.InteropServices;

namespace CapChief.Graphics;

/// <summary>
/// Finds straight horizontal and vertical lines in a picture, such as the borders of buttons and
/// windows.
/// </summary>
public sealed class LineFinder
{
    private const int Contrast = 8;

    private const float MinLengthDip = 20f, CornerReachDip = 10f, BendLengthDip = 8f;

    private readonly byte[] _bgra;
    private readonly int _width, _height;
    private readonly int _minLength, _cornerReach, _bendLength, _thickness;

    public LineFinder(byte[] bgra, int width, int height, float scale)
    {
        _bgra = bgra;
        _width = width;
        _height = height;
        _minLength = (int)MathF.Round(MinLengthDip * scale);
        _cornerReach = (int)MathF.Round(CornerReachDip * scale);
        _bendLength = (int)MathF.Round(BendLengthDip * scale);
        _thickness = Math.Max(1, (int)MathF.Ceiling(scale));
    }

    private ReadOnlySpan<uint> Pixels
    {
        get
        {
            return MemoryMarshal.Cast<byte, uint>(_bgra);
        }
    }

    public static LineFinder? Of(Surface picture, float scale)
    {
        if (picture.ReadPixels(picture.Bounds) is { } bgra)
        {
            return new LineFinder(bgra, picture.Width, picture.Height, scale);
        }
        else
        {
            return null;
        }
    }

    public int? NearestLine(Edges side, int at, int from, int to, int reach)
    {
        bool vertical = side is Edges.West or Edges.East;
        int inward;
        if (side is Edges.West or Edges.North)
        {
            inward = 1;
        }
        else
        {
            inward = -1;
        }
        int across = LengthAcross(vertical);
        int along = LengthAlong(vertical);
        from = Math.Max(0, from);
        to = Math.Min(along, to);

        int margin = 4 * _thickness;
        int lo = Math.Max(1, at - reach - margin);
        int hi = Math.Min(across - 1, at + reach + margin);
        if (to <= from || hi < lo)
        {
            return null;
        }

        Span<int> crossings = stackalloc int[hi - lo + 1];
        CountCrossings(vertical, lo, hi, from, to, crossings);
        int overlapNeeded = Math.Min(_minLength, to - from);
        Span<bool> lines = stackalloc bool[hi - lo + 1];
        Span<bool> fits = stackalloc bool[hi - lo + 1];
        for (int b = lo; b <= hi; b++)
        {
            if (crossings[b - lo] >= overlapNeeded)
            {
                (lines[b - lo], fits[b - lo]) = LineAt(vertical, b, from, to, overlapNeeded, inward);
            }
        }
        return Nearest(fits, lo, hi, at, reach, inward) ?? Nearest(lines, lo, hi, at, reach, inward);
    }

    private int? Nearest(ReadOnlySpan<bool> lines, int lo, int hi, int at, int reach, int inward)
    {
        int? nearest = null;
        int nearestDistance = reach + 1;
        for (int b = lo; b <= hi; b++)
        {
            if (!lines[b - lo])
            {
                continue;
            }
            int first = b;
            int last = b;
            for (int next = b + 1; next <= hi && next - last <= _thickness; next++)
            {
                if (lines[next - lo])
                {
                    last = next;
                }
            }
            b = last;

            int distance;
            if (at < first)
            {
                distance = first - at;
            }
            else if (at > last)
            {
                distance = at - last;
            }
            else
            {
                distance = 0;
            }
            if (distance >= nearestDistance)
            {
                continue;
            }
            if (inward > 0)
            {
                nearest = first;
            }
            else
            {
                nearest = last;
            }
            nearestDistance = distance;
        }
        return nearest;
    }

    private void CountCrossings(bool vertical, int lo, int hi, int from, int to, Span<int> crossings)
    {
        ReadOnlySpan<uint> pixels = Pixels;
        if (vertical)
        {
            for (int t = from; t < to; t++)
            {
                ReadOnlySpan<uint> row = pixels.Slice(t * _width, _width);
                for (int b = lo; b <= hi; b++)
                {
                    if (Differ(row[b - 1], row[b]))
                    {
                        crossings[b - lo]++;
                    }
                }
            }
            return;
        }
        for (int b = lo; b <= hi; b++)
        {
            ReadOnlySpan<uint> above = pixels.Slice((b - 1) * _width, _width);
            ReadOnlySpan<uint> below = pixels.Slice(b * _width, _width);
            for (int t = from; t < to; t++)
            {
                if (Differ(above[t], below[t]))
                {
                    crossings[b - lo]++;
                }
            }
        }
    }

    private (bool Found, bool Fits) LineAt(bool vertical, int b, int from, int to, int overlapNeeded, int inward)
    {
        int along = LengthAlong(vertical);
        bool found = false;
        for (int t = from; t < to; t++)
        {
            if (!Crosses(vertical, b, t))
            {
                continue;
            }
            (int start, int end) = RunThrough(vertical, b, t, 0, along);
            t = end - 1;
            if (end - start < _minLength || Math.Min(end, to) - Math.Max(start, from) < overlapNeeded)
            {
                continue;
            }
            int bend = Bend(vertical, b, start, end);
            if (bend == 0 || bend == inward)
            {
                return (true, true);
            }
            found = true;
        }
        return (found, false);
    }

    private int Bend(bool vertical, int b, int start, int end)
    {
        int first = BendNear(vertical, b, start - _cornerReach, start + 1);
        int last = BendNear(vertical, b, end - 1, end + _cornerReach);
        if (first == 0)
        {
            return last;
        }
        if (last == 0 || last == first)
        {
            return first;
        }
        else
        {
            return 0;
        }
    }

    private int BendNear(bool vertical, int b, int from, int to)
    {
        int along = LengthAlong(vertical);
        int across = LengthAcross(vertical);
        int left = Math.Max(0, b - _cornerReach - _bendLength);
        int right = Math.Min(across, b + _cornerReach + _bendLength);
        int bend = 0;
        int nearest = _cornerReach + 1;
        for (int y = Math.Max(1, from); y <= Math.Min(along - 1, to); y++)
        {
            for (int x = left; x < right; x++)
            {
                if (!Crosses(!vertical, y, x))
                {
                    continue;
                }
                (int start, int end) = RunThrough(!vertical, y, x, left, right);
                x = end - 1;
                if (end - Math.Max(start, b) >= _bendLength)
                {
                    Weigh(Math.Abs(start - b), 1, ref nearest, ref bend);
                }
                if (Math.Min(end, b) - start >= _bendLength)
                {
                    Weigh(Math.Abs(end - b), -1, ref nearest, ref bend);
                }
            }
        }
        return bend;
    }

    private static void Weigh(int distance, int direction, ref int nearest, ref int bend)
    {
        if (distance < nearest)
        {
            nearest = distance;
            bend = direction;
        }
        else if (distance == nearest && direction != bend)
        {
            bend = 0;
        }
    }

    private (int Start, int End) RunThrough(bool vertical, int b, int t, int from, int to)
    {
        int start = t;
        int end = t + 1;
        while (start > from && Linked(vertical, b, start))
        {
            start--;
        }
        while (end < to && Linked(vertical, b, end))
        {
            end++;
        }
        return (start, end);
    }

    private bool Linked(bool vertical, int b, int t)
    {
        if (!Crosses(vertical, b, t - 1) || !Crosses(vertical, b, t))
        {
            return false;
        }
        ReadOnlySpan<uint> pixels = Pixels;
        int i = Index(vertical, b, t);
        int stepAcross = StepAcross(vertical);
        int stepAlong = StepAlong(vertical);
        return !Differ(pixels[i], pixels[i - stepAlong]) || !Differ(pixels[i - stepAcross], pixels[i - stepAcross - stepAlong]);
    }

    private bool Crosses(bool vertical, int b, int t)
    {
        ReadOnlySpan<uint> pixels = Pixels;
        int i = Index(vertical, b, t);
        int step = StepAcross(vertical);
        return Differ(pixels[i], pixels[i - step]);
    }

    private int Index(bool vertical, int b, int t)
    {
        return t * StepAlong(vertical) + b * StepAcross(vertical);
    }

    private int LengthAlong(bool vertical)
    {
        if (vertical)
        {
            return _height;
        }
        else
        {
            return _width;
        }
    }

    private int LengthAcross(bool vertical)
    {
        if (vertical)
        {
            return _width;
        }
        else
        {
            return _height;
        }
    }

    private int StepAcross(bool vertical)
    {
        if (vertical)
        {
            return 1;
        }
        else
        {
            return _width;
        }
    }

    private int StepAlong(bool vertical)
    {
        if (vertical)
        {
            return _width;
        }
        else
        {
            return 1;
        }
    }

    private static bool Differ(uint a, uint b)
    {
        if (((a ^ b) & 0xFFFFFF) == 0)
        {
            return false;
        }
        int blue = Math.Abs((int)(a & 0xFF) - (int)(b & 0xFF));
        int green = Math.Abs((int)(a >> 8 & 0xFF) - (int)(b >> 8 & 0xFF));
        int red = Math.Abs((int)(a >> 16 & 0xFF) - (int)(b >> 16 & 0xFF));
        return blue >= Contrast || green >= Contrast || red >= Contrast;
    }
}
