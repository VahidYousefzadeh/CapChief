namespace CapChief.UIFramework;

/// <summary>
/// Represents the roundness of a control's corners as a fraction of its height.
/// </summary>
public readonly struct CornerRadius
{
    private readonly float _fraction;

    private CornerRadius(float fraction)
    {
        _fraction = fraction;
    }

    public static CornerRadius None
    {
        get
        {
            return default;
        }
    }

    public static CornerRadius OfHeight(float fraction)
    {
        return new(fraction);
    }

    public float In(Rect r)
    {
        return r.Height * _fraction;
    }
}
