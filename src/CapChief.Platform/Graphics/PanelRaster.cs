namespace CapChief.Graphics;

/// <summary>
/// Provides methods for rendering rounded rectangles with flat or gradient fills into a bitmap.
/// </summary>
public static class PanelRaster
{
    public static uint Between(uint topArgb, uint bottomArgb, float v)
    {
        v = Math.Clamp(v, 0f, 1f);
        byte r = Channel.Clamp(Mix(R(topArgb), R(bottomArgb), v));
        byte g = Channel.Clamp(Mix(G(topArgb), G(bottomArgb), v));
        byte b = Channel.Clamp(Mix(B(topArgb), B(bottomArgb), v));
        return 0xFF000000u | (uint)r << 16 | (uint)g << 8 | b;
    }

    public static void Flat(Pixmap target, uint colorRef, float radius)
    {
        Span<byte> dst = target.Pixels;
        int w = target.Width;
        int h = target.Height;
        int pixelsPerRow = target.PixelsPerRow;
        float hw = w * 0.5f;
        float hh = h * 0.5f;
        float rad = MathF.Min(radius, MathF.Min(hw, hh));
        int cr = (int)(colorRef & 0xFF);
        int cg = (int)((colorRef >> 8) & 0xFF);
        int cb = (int)((colorRef >> 16) & 0xFF);

        for (int y = 0; y < h; y++)
        {
            float py = y + 0.5f - hh;
            int drow = y * pixelsPerRow * 4;
            for (int x = 0; x < w; x++)
            {
                int a = (int)(Math.Clamp(0.5f - Sd(x + 0.5f - hw, py, hw, hh, rad), 0f, 1f) * 255f + 0.5f);
                int q = drow + x * 4;
                dst[q] = (byte)(cb * a / 255);
                dst[q + 1] = (byte)(cg * a / 255);
                dst[q + 2] = (byte)(cr * a / 255);
                dst[q + 3] = (byte)a;
            }
        }
    }

    public static void Gradient(Pixmap target, int margin, float radius, float rimWidth, in PanelLook look)
    {
        Span<byte> dst = target.Pixels;
        int w = target.Width;
        int h = target.Height;
        int pixelsPerRow = target.PixelsPerRow;
        float hw = (w - 2 * margin) * 0.5f;
        float hh = (h - 2 * margin) * 0.5f;
        float rad = MathF.Min(radius, MathF.Min(hw, hh));
        float cx = w * 0.5f;
        float cy = h * 0.5f;
        float topR = R(look.TopArgb);
        float topG = G(look.TopArgb);
        float topB = B(look.TopArgb);
        float botR = R(look.BottomArgb);
        float botG = G(look.BottomArgb);
        float botB = B(look.BottomArgb);

        for (int y = 0; y < h; y++)
        {
            float py = y + 0.5f - cy;
            int drow = y * pixelsPerRow * 4;
            float v = Math.Clamp((py + hh) / (2f * hh), 0f, 1f);
            float r = Mix(topR, botR, v);
            float g = Mix(topG, botG, v);
            float b = Mix(topB, botB, v);
            float rimA0 = look.RimTop + (look.RimBottom - look.RimTop) * v;

            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f - cx;
                int q = drow + x * 4;

                float sd = Sd(px, py, hw, hh, rad);
                float cov = Math.Clamp(0.5f - sd, 0f, 1f);
                if (cov <= 0f)
                {
                    dst[q] = 0;
                    dst[q + 1] = 0;
                    dst[q + 2] = 0;
                    dst[q + 3] = 0;
                    continue;
                }

                float rimA = (cov - Math.Clamp(0.5f - (sd + rimWidth), 0f, 1f)) * rimA0;
                dst[q] = Channel.Clamp((b + (255f - b) * rimA) * cov);
                dst[q + 1] = Channel.Clamp((g + (255f - g) * rimA) * cov);
                dst[q + 2] = Channel.Clamp((r + (255f - r) * rimA) * cov);
                dst[q + 3] = (byte)(cov * 255f + 0.5f);
            }
        }
    }

    private static float Mix(float from, float to, float v)
    {
        return from + (to - from) * v;
    }

    private static float R(uint argb)
    {
        return (argb >> 16) & 0xFF;
    }

    private static float G(uint argb)
    {
        return (argb >> 8) & 0xFF;
    }

    private static float B(uint argb)
    {
        return argb & 0xFF;
    }

    private static float Sd(float px, float py, float hw, float hh, float r)
    {
        float qx = MathF.Abs(px) - (hw - r);
        float qy = MathF.Abs(py) - (hh - r);
        float ax = MathF.Max(qx, 0f);
        float ay = MathF.Max(qy, 0f);
        return MathF.Sqrt(ax * ax + ay * ay) + MathF.Min(MathF.Max(qx, qy), 0f) - r;
    }
}

/// <summary>
/// Represents the gradient colors and edge highlight settings of a rounded rectangle.
/// </summary>
public readonly struct PanelLook
{
    public uint TopArgb { get; init; }

    public uint BottomArgb { get; init; }

    public float RimTop { get; init; }

    public float RimBottom { get; init; }
}
