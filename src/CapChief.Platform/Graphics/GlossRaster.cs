namespace CapChief.Graphics;

/// <summary>
/// Provides methods for rendering a moving highlight across the pixels of a bitmap.
/// </summary>
public static class GlossRaster
{
    public static void Sweep(Pixmap target, int margin, float u, in GlossLook look)
    {
        Span<byte> dst = target.Pixels;
        int w = target.Width;
        int h = target.Height;
        int pixelsPerRow = target.PixelsPerRow;
        float half = MathF.Max(1f, look.Half * h);
        float span = w + look.Lean * h;
        float center = -half + u * (span + 2f * half);
        float barH = MathF.Max(1f, h - 2 * margin);

        for (int y = 0; y < h; y++)
        {
            int drow = y * pixelsPerRow * 4;
            float lean = look.Lean * (h - y);
            float down = Math.Clamp((y - margin) / barH, 0f, 1f);
            float tilt = 1f - look.Tilt * down;

            for (int x = 0; x < w; x++)
            {
                float t = (x + lean - center) / half;
                if (t <= -1f || t >= 1f)
                {
                    continue;
                }
                float e = 1f - t * t;
                float gain = look.Gain * e * e * tilt;
                float spec = look.Spec * MathF.Exp(-t * t / (look.Core * look.Core));

                int q = drow + x * 4;
                float a = dst[q + 3];
                for (int c = 0; c < 3; c++)
                {
                    float lit = dst[q + c] * (1f + gain);
                    lit += (a - lit) * spec;
                    dst[q + c] = Channel.Clamp(MathF.Min(lit, a));
                }
            }
        }
    }
}

/// <summary>
/// Represents the shape, brightness and angle settings of a rendered highlight.
/// </summary>
public readonly struct GlossLook
{
    public float Lean { get; init; }

    public float Half { get; init; }

    public float Gain { get; init; }

    public float Spec { get; init; }

    public float Core { get; init; }

    public float Tilt { get; init; }
}
