namespace CapChief.UIFramework;

/// <summary>
/// Describes the thickness of the four edges of a margin or padding.
/// </summary>
public readonly struct Thickness
{
    private readonly Length _left;
    private readonly Length _top;
    private readonly Length _right;
    private readonly Length _bottom;

    private Thickness(Length left, Length top, Length right, Length bottom)
    {
        _left = left;
        _top = top;
        _right = right;
        _bottom = bottom;
    }

    public static Thickness All(Length v)
    {
        return new(v, v, v, v);
    }

    public static Thickness Sides(Length v)
    {
        return new(v, Length.Auto, v, Length.Auto);
    }

    public static Thickness Ends(Length v)
    {
        return new(Length.Auto, v, Length.Auto, v);
    }

    public static Thickness AtLeft(Length v)
    {
        return new(v, Length.Auto, Length.Auto, Length.Auto);
    }

    public Size Grow(Size s, float scale)
    {
        return new(s.Width + Width(scale), s.Height + Height(scale));
    }

    public Size Shrink(Size s, float scale)
    {
        return new(Math.Max(0, s.Width - Width(scale)), Math.Max(0, s.Height - Height(scale)));
    }

    public int Width(float scale)
    {
        return _left.Px(scale, 0) + _right.Px(scale, 0);
    }

    public int Height(float scale)
    {
        return _top.Px(scale, 0) + _bottom.Px(scale, 0);
    }

    public Rect Shrink(Rect r, float scale)
    {
        return new()
        {
            Left = r.Left + _left.Px(scale, 0),
            Top = r.Top + _top.Px(scale, 0),
            Right = r.Right - _right.Px(scale, 0),
            Bottom = r.Bottom - _bottom.Px(scale, 0),
        };
    }
}
