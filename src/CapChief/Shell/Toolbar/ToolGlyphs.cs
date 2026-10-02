namespace CapChief.Shell;

/// <summary>
/// Draws the icons for the mark tools, such as arrow, pen and text.
/// </summary>
public static class ToolGlyphs
{
    public static void Arrow(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float x1 = cx - s;
        float y1 = cy + s;
        float x2 = cx + s;
        float y2 = cy - s;
        d.Line(x1, y1, x2, y2, stroke);
        float ang = MathF.Atan2(y2 - y1, x2 - x1);
        float hl = s * 0.95f;
        float spread = MathF.PI / 4;
        d.Line(x2, y2, x2 - hl * MathF.Cos(ang - spread), y2 - hl * MathF.Sin(ang - spread), stroke);
        d.Line(x2, y2, x2 - hl * MathF.Cos(ang + spread), y2 - hl * MathF.Sin(ang + spread), stroke);
    }

    public static void Box(DrawContext d, float cx, float cy, float s)
    {
        using Outline p = d.NewFigure(winding: true);
        p.RoundRect(cx - s, cy - s, s * 2, s * 2, s * 0.35f);
        d.Stroke(p, GlyphStyle.IconStroke(d));
    }

    public static void Ellipse(DrawContext d, float cx, float cy, float s)
    {
        d.StrokeEllipse(cx - s, cy - s, s * 2, s * 2, GlyphStyle.IconStroke(d));
    }

    public static void Pen(DrawContext d, float cx, float cy, float s)
    {
        float x0 = cx - s;
        float y0 = cy + s;
        float dx = s * 2;
        float dy = -s * 2;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        float ax = -dy / len * s * 0.442f;
        float ay = dx / len * s * 0.442f;

        float pointX = x0 + dx * 0.275f;
        float pointY = y0 + dy * 0.275f;
        float bodyX = x0 + dx * 0.3375f;
        float bodyY = y0 + dy * 0.3375f;
        float bodyEndX = x0 + dx * 0.8f;
        float bodyEndY = y0 + dy * 0.8f;
        float eraserX = x0 + dx * 0.8625f;
        float eraserY = y0 + dy * 0.8625f;
        float endX = x0 + dx;
        float endY = y0 + dy;

        using Outline p = d.NewFigure(winding: true);
        p.Line(x0, y0, pointX + ax, pointY + ay);
        p.Line(pointX + ax, pointY + ay, pointX - ax, pointY - ay);
        p.Close();
        p.Line(bodyX + ax, bodyY + ay, bodyEndX + ax, bodyEndY + ay);
        p.Line(bodyEndX - ax, bodyEndY - ay, bodyX - ax, bodyY - ay);
        p.Close();
        p.Line(eraserX + ax, eraserY + ay, endX + ax, endY + ay);
        p.Line(endX - ax, endY - ay, eraserX - ax, eraserY - ay);
        p.Close();
        d.Fill(p, GlyphStyle.Foreground);
    }

    public static void Text(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float bar = cy - s;
        float arm = s * 0.92f;
        d.Line(cx - arm, bar, cx + arm, bar, stroke);
        d.Line(cx - arm, bar, cx - arm, cy - s * 0.77f, stroke);
        d.Line(cx + arm, bar, cx + arm, cy - s * 0.77f, stroke);
        d.Line(cx, bar, cx, cy + s, stroke);
    }

    public static void Step(DrawContext d, Rect r, int step)
    {
        string n = step.ToString();
        float dia = MathF.Round(r.Height * 0.452f);
        float cx = r.Left + r.Width * 0.5f;
        float cy = r.Top + r.Height * 0.5f;

        if (d.CenteredTextInk(n, r, Theme.StepBadge, out Rect ink))
        {
            cx = (ink.Left + ink.Right) * 0.5f;
            cy = (ink.Top + ink.Bottom) * 0.5f;
            dia = MathF.Max(dia, ink.Width + Theme.StepBadgePad.Exact(d.Scale));
        }

        d.FillEllipse(cx - dia * 0.5f, cy - dia * 0.5f, dia, dia, GlyphStyle.Foreground);
        d.CenteredText(n, r, GlyphStyle.PanelMiddle, Theme.StepBadge);
    }

    public static void Mosaic(DrawContext d, float cx, float cy, float s)
    {
        float cell = s * 2f / 3f;
        float gap = Theme.MosaicGap.Exact(d.Scale);
        for (int gy = 0; gy < 3; gy++)
        {
            for (int gx = gy & 1; gx < 3; gx += 2)
            {
                d.Fill(cx - s + gx * cell, cy - s + gy * cell, cell - gap, cell - gap, GlyphStyle.Foreground);
            }
        }
    }

    public static void Bar(DrawContext d, float cx, float cy, float s)
    {
        float h = s * 0.5f;
        d.FillRound(cx - s, cy - h, s * 2, h * 2, Theme.RedactRadius.Exact(d.Scale), GlyphStyle.Foreground);
    }

    public static void Swatch(DrawContext d, float cx, float cy, float s, Color c)
    {
        float r = s * 0.67f;
        float tip = cy - s;
        float circleY = cy + s - r;
        float edge = MathF.Acos(r / (circleY - tip)) * 180f / MathF.PI;
        float start = 270f + edge;
        float rad = start * MathF.PI / 180f;

        using Outline p = d.NewFigure(winding: true);
        p.Line(cx, tip, cx + r * MathF.Cos(rad), circleY + r * MathF.Sin(rad));
        p.Arc(cx - r, circleY - r, r * 2, r * 2, start, 360f - edge * 2);
        p.Close();
        d.Fill(p, c);
    }
}
