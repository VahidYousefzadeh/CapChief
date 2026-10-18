namespace CapChief.Shell;

/// <summary>
/// Draws the icons of the About box's links: a globe for the app's page, angle brackets for its source
/// code, a shield for its privacy policy, and the small arrow that marks a link as opening the browser.
/// </summary>
public static class LinkGlyphs
{
    private static readonly Color Faint = Color.FromArgb(Palette.KeyTextArgb);

    private static readonly PointF[] ShieldEdge = new PointF[6];

    public static void Globe(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float meridian = s * 0.46f;
        d.StrokeEllipse(cx - s, cy - s, s * 2f, s * 2f, stroke);
        d.StrokeEllipse(cx - meridian, cy - s, meridian * 2f, s * 2f, stroke);
        d.Line(cx - s, cy, cx + s, cy, stroke);
    }

    public static void Code(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float tip = s * 1.14f;
        float back = s * 0.5f;
        float arm = s * 0.71f;
        d.Line(cx - back, cy - arm, cx - tip, cy, stroke);
        d.Line(cx - tip, cy, cx - back, cy + arm, stroke);
        d.Line(cx + back, cy - arm, cx + tip, cy, stroke);
        d.Line(cx + tip, cy, cx + back, cy + arm, stroke);
        d.Line(cx + s * 0.23f, cy - s * 0.93f, cx - s * 0.23f, cy + s * 0.93f, stroke);
    }

    public static void Shield(DrawContext d, float cx, float cy, float s)
    {
        float side = s * 0.93f;
        ShieldEdge[0] = new PointF(cx, cy - s * 1.07f);
        ShieldEdge[1] = new PointF(cx + side, cy - s * 0.71f);
        ShieldEdge[2] = new PointF(cx + side, cy + s * 0.07f);
        ShieldEdge[3] = new PointF(cx, cy + s * 1.07f);
        ShieldEdge[4] = new PointF(cx - side, cy + s * 0.07f);
        ShieldEdge[5] = new PointF(cx - side, cy - s * 0.71f);

        using Outline p = d.NewFigure(winding: false);
        p.Lines(ShieldEdge, ShieldEdge.Length);
        p.Close();
        d.Stroke(p, GlyphStyle.IconStroke(d));
    }

    public static void LinkOut(DrawContext d, float cx, float cy, float s)
    {
        var stroke = new StrokeStyle(Faint, Theme.IconPen.Exact(d.Scale));
        float head = s * 0.29f;
        d.Line(cx - s, cy + s, cx + s, cy - s, stroke);
        d.Line(cx - head, cy - s, cx + s, cy - s, stroke);
        d.Line(cx + s, cy - s, cx + s, cy + head, stroke);
    }
}
