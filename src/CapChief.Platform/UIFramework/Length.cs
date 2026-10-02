namespace CapChief.UIFramework;

/// <summary>
/// Represents a size in device-independent pixels that resolves to physical pixels at a display
/// scale.
/// </summary>
public readonly struct Length
{
    private readonly float _dip;
    private readonly float _floor;
    private readonly bool _given;

    private Length(float dip, float floor)
    {
        _dip = dip;
        _floor = floor;
        _given = true;
    }

    public static Length operator -(Length l)
    {
        if (l._given)
        {
            return new(-l._dip, -l._floor);
        }
        else
        {
            return l;
        }
    }

    public static Length Auto
    {
        get
        {
            return default;
        }
    }

    public bool IsAuto
    {
        get
        {
            return !_given;
        }
    }

    public static Length Of(float dip)
    {
        return new(dip, 0f);
    }

    public static Length AtLeast(float floorPx, float dip)
    {
        return new(dip, floorPx);
    }

    public int Px(float scale)
    {
        return (int)Floored(MathF.Round(_dip * scale));
    }

    public int Px(float scale, int whenAuto)
    {
        if (_given)
        {
            return Px(scale);
        }
        else
        {
            return whenAuto;
        }
    }

    public float Exact(float scale)
    {
        return Floored(_dip * scale);
    }

    private float Floored(float px)
    {
        if (_floor > 0f)
        {
            return MathF.Max(_floor, px);
        }
        else if (_floor < 0f)
        {
            return MathF.Min(_floor, px);
        }
        else
        {
            return px;
        }
    }
}
