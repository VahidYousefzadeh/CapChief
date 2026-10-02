namespace CapChief.Shell;

/// <summary>
/// Provides methods for drawing the clock icon used for delayed captures.
/// </summary>
public static class ClockGlyph
{
    private const float Rim = 0.9f, HandUp = 0.52f, HandOutX = 0.42f, HandOutY = 0.3f;

    public static void Draw(DrawContext d, float cx, float cy, float s, in StrokeStyle stroke)
    {
        float r = s * Rim;
        d.StrokeEllipse(cx - r, cy - r, r * 2, r * 2, stroke);
        d.Line(cx, cy - r * HandUp, cx, cy, stroke);
        d.Line(cx, cy, cx + r * HandOutX, cy + r * HandOutY, stroke);
    }

    public static void Draw(DeviceContext ink, float cx, float cy, float s, uint argb, float width)
    {
        float r = s * Rim;
        ink.Ring(argb, width, cx - r, cy - r, r * 2, r * 2);
        ink.Line(argb, width, cx, cy - r * HandUp, cx, cy);
        ink.Line(argb, width, cx, cy, cx + r * HandOutX, cy + r * HandOutY);
    }
}
