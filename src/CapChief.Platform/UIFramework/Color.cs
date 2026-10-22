namespace CapChief.UIFramework;

/// <summary>
/// Represents a color as alpha, red, green, and blue components.
/// </summary>
public readonly record struct Color
{
    private const int LumaR = 299, LumaG = 587, LumaB = 114;

    private const int LumaMidpoint = 128 * (LumaR + LumaG + LumaB);

    private readonly uint _argb;

    private Color(uint argb)
    {
        _argb = argb;
    }

    public uint Argb
    {
        get
        {
            return _argb;
        }
    }

    public uint ColorRef
    {
        get
        {
            return CapChief.Graphics.Argb.SwapRedBlue(_argb);
        }
    }

    public bool IsTransparent
    {
        get
        {
            return (_argb >> 24) == 0;
        }
    }

    public static Color FromArgb(uint argb)
    {
        return new(argb);
    }

    public Color Lightened(float share)
    {
        uint argb = _argb & 0xFF000000u;
        for (int shift = 0; shift < 24; shift += 8)
        {
            int c = (int)((_argb >> shift) & 0xFF);
            argb |= (uint)(c + (int)MathF.Round((255 - c) * share)) << shift;
        }
        return FromArgb(argb);
    }

    public Color ContrastingTextColor()
    {
        int r = (int)((_argb >> 16) & 0xFF);
        int g = (int)((_argb >> 8) & 0xFF);
        int b = (int)(_argb & 0xFF);
        uint argb;
        if (LumaR * r + LumaG * g + LumaB * b >= LumaMidpoint)
        {
            argb = 0xFF000000u;
        }
        else
        {
            argb = 0xFFFFFFFFu;
        }
        return FromArgb(argb);
    }
}
