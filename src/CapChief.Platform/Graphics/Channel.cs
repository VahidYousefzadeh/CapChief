namespace CapChief.Graphics;

/// <summary>
/// Provides helpers for clamping a floating-point value to a single 8-bit color channel.
/// </summary>
public static class Channel
{
    public static byte Clamp(float v)
    {
        if (v <= 0f)
        {
            return 0;
        }
        else if (v >= 255f)
        {
            return 255;
        }
        else
        {
            return (byte)(v + 0.5f);
        }
    }
}
