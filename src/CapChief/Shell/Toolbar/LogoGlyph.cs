namespace CapChief.Shell;

/// <summary>
/// Draws the app's logo with the shapes and colors of icon.svg: a green rounded square with four
/// viewfinder corners around a camera. The numbers below are icon.svg's, in its 1024-unit square.
/// </summary>
public static class LogoGlyph
{
    private const float Units = 1024f;

    private static readonly Color Ground = Color.FromArgb(0xFF139268u);
    private static readonly Color Light = Color.FromArgb(0xFF9FEFCDu);
    private static readonly Color Dark = Color.FromArgb(0xFF014F35u);

    private static readonly PointF[] Hump = new PointF[10];

    public static void Draw(DrawContext d, float x, float y, float side)
    {
        float s = side / Units;
        d.FillRound(x, y, side, side, 244.82f * s, Ground);
        Corner(d, x, y, s, false, false);
        Corner(d, x, y, s, true, false);
        Corner(d, x, y, s, false, true);
        Corner(d, x, y, s, true, true);
        Camera(d, x, y, s);
        d.FillEllipse(x + 401f * s, y + 413.95f * s, 221.86f * s, 221.86f * s, Light);
    }

    /// <summary>
    /// Fills one viewfinder corner: the top left one, or its mirror image across the middle.
    /// </summary>
    private static void Corner(DrawContext d, float x, float y, float s, bool mirrorX, bool mirrorY)
    {
        using Outline p = d.NewFigure(winding: true);
        Arc(p, x, y, s, mirrorX, mirrorY, 215.63f, 215.63f, 71.08f, 180f, 90f);
        Arc(p, x, y, s, mirrorX, mirrorY, 345.75f, 169.515f, 24.965f, 270f, 180f);
        Arc(p, x, y, s, mirrorX, mirrorY, 226.55f, 226.55f, 32.07f, 270f, -90f);
        Arc(p, x, y, s, mirrorX, mirrorY, 169.515f, 334.39f, 24.965f, 0f, 180f);
        p.Close();
        d.Fill(p, Light);
    }

    private static void Arc(in Outline p, float x, float y, float s, bool mirrorX, bool mirrorY, float cx, float cy, float r, float start, float sweep)
    {
        if (mirrorX)
        {
            cx = Units - cx;
            start = 180f - start;
            sweep = -sweep;
        }
        if (mirrorY)
        {
            cy = Units - cy;
            start = -start;
            sweep = -sweep;
        }
        p.Arc(x + (cx - r) * s, y + (cy - r) * s, r * 2f * s, r * 2f * s, start, sweep);
    }

    /// <summary>
    /// Fills the camera body and the hump on its top as one shape.
    /// </summary>
    private static void Camera(DrawContext d, float x, float y, float s)
    {
        Hump[0] = new PointF(x + 377.93f * s, y + 380f * s);
        Hump[1] = new PointF(x + 377.93f * s, y + 350.44f * s);
        Hump[2] = new PointF(x + 399.98f * s, y + 315.41f * s);
        Hump[3] = new PointF(x + 418f * s, y + 297f * s);
        Hump[4] = new PointF(x + 448.47f * s, y + 288.65f * s);
        Hump[5] = new PointF(x + 575.53f * s, y + 288.65f * s);
        Hump[6] = new PointF(x + 606f * s, y + 297f * s);
        Hump[7] = new PointF(x + 624.02f * s, y + 315.41f * s);
        Hump[8] = new PointF(x + 646.07f * s, y + 350.44f * s);
        Hump[9] = new PointF(x + 646.07f * s, y + 380f * s);

        using Outline p = d.NewFigure(winding: true);
        p.RoundRect(x + 255.12f * s, y + 350.44f * s, 513.49f * s, 347.38f * s, 63.18f * s);
        p.Lines(Hump, Hump.Length);
        p.Close();
        d.Fill(p, Dark);
    }
}
