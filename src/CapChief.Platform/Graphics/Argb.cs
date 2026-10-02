namespace CapChief.Graphics;

/// <summary>
/// Provides helpers for converting and blending 32-bit ARGB color values.
/// </summary>
public static class Argb
{
    public static uint SwapRedBlue(uint c)
    {
        return ((c & 0xFF) << 16) | (c & 0xFF00) | ((c >> 16) & 0xFF);
    }

    public static uint ToArgb(uint colorRef)
    {
        return 0xFF000000u | SwapRedBlue(colorRef);
    }

    public static uint WhiteWash(byte a)
    {
        return ((uint)a << 24) | 0x00FFFFFFu;
    }

    public static uint Over(uint src, uint dst)
    {
        int a = (int)(src >> 24);
        if (a == 0)
        {
            return dst;
        }
        if (a == 255)
        {
            return src;
        }

        uint Mix(int sh)
        {
            int s = (int)((src >> sh) & 0xFF);
            int d = (int)((dst >> sh) & 0xFF);
            return (uint)(d + (s - d) * a / 255) << sh;
        }
        return 0xFF000000u | Mix(16) | Mix(8) | Mix(0);
    }
}
