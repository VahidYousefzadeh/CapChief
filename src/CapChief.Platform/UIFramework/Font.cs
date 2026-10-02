namespace CapChief.UIFramework;

/// <summary>
/// Represents a typeface, size, and weight used to draw text.
/// </summary>
public readonly struct Font
{
    private readonly Length _size;
    private readonly int _weight;
    private readonly FontFace _face;
    private readonly bool _given;

    private Font(Length size, int weight, FontFace face)
    {
        _size = size;
        _weight = weight;
        _face = face;
        _given = true;
    }

    public bool IsUnset
    {
        get
        {
            return !_given;
        }
    }

    public int Weight
    {
        get
        {
            return _weight;
        }
    }

    public FontFace Face
    {
        get
        {
            return _face;
        }
    }

    public static Font Of(Length size, int weight, FontFace face)
    {
        return new(size, weight, face);
    }

    public int Px(float scale)
    {
        return _size.Px(scale, 0);
    }
}

/// <summary>
/// Specifies the typeface and rendering style used to draw text.
/// </summary>
public enum FontFace
{
    SharpSemibold,
    Smooth,
    SmoothSemibold,
}
