namespace CapChief.Shell;

/// <summary>
/// Draws the icons for the toolbar's action buttons, such as save and copy.
/// </summary>
public static class ActionGlyphs
{
    private static readonly PointF[] Tri = new PointF[3];

    public static void Camera(DrawContext d, float cx, float cy, float s)
    {
        float bw = s * 2f;
        float bh = s * 1.46f;
        float bx = cx - bw * 0.5f;
        float by = cy - bh * 0.5f + s * 0.12f;
        float hw = s * 0.72f;
        float hh = s * 0.34f;

        d.FillRound(bx + s * 0.16f, by - hh + s * 0.06f, hw, hh * 2f, hh * 0.5f, GlyphStyle.Foreground);

        using Outline p = d.NewFigure(winding: false);
        p.RoundRect(bx, by, bw, bh, s * 0.38f);

        float lr = s * 0.44f;
        p.Ellipse(cx - lr, by + bh * 0.5f - lr, lr * 2, lr * 2);
        float dr = s * 0.11f;
        p.Ellipse(bx + bw - s * 0.46f, by + s * 0.24f, dr * 2, dr * 2);

        d.Fill(p, GlyphStyle.Foreground);
    }

    public static void GifMark(DrawContext d, Rect r)
    {
        const string mark = "GIF";
        float cx = r.Left + r.Width * 0.5f;
        float cy = r.Top + r.Height * 0.5f;
        float w = r.Height * 0.62f;
        float h = r.Height * 0.42f;
        float scale = d.Scale;

        if (d.CenteredTextInk(mark, r, Theme.GifMark, out Rect box))
        {
            cx = (box.Left + box.Right) * 0.5f;
            cy = (box.Top + box.Bottom) * 0.5f;
            float pad = Theme.GifMarkPad.Exact(scale);
            w = MathF.Max(w, box.Width + pad);
            h = MathF.Max(h, box.Height + pad);
        }

        using Outline p = d.NewFigure(winding: false);
        p.RoundRect(cx - w * 0.5f, cy - h * 0.5f, w, h, MathF.Min(Theme.GifMarkRadius.Exact(scale), h * 0.34f));
        d.Stroke(p, GlyphStyle.IconStroke(d));
        d.CenteredText(mark, r, GlyphStyle.Foreground, Theme.GifMark);
    }

    public static void Cross(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        d.Line(cx - s, cy - s, cx + s, cy + s, stroke);
        d.Line(cx + s, cy - s, cx - s, cy + s, stroke);
    }

    public static void Save(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float tip = cy + s * 0.477f;
        d.Line(cx, cy - s * 0.841f, cx, tip, stroke);
        d.Line(cx - s * 0.43f, cy - s * 0.05f, cx, tip, stroke);
        d.Line(cx + s * 0.43f, cy - s * 0.05f, cx, tip, stroke);

        float wall = cx + s * 0.8f;
        float lip = cy + s * 0.341f;
        float floor = cy + s * 0.841f;
        using Outline p = d.NewFigure(winding: true);
        p.Line(cx - s * 0.8f, lip, cx - s * 0.8f, floor);
        p.Line(cx - s * 0.8f, floor, wall, floor);
        p.Line(wall, floor, wall, lip);
        d.Stroke(p, stroke);
    }

    public static void Copy(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float side = s * 1.15f;
        float off = s * 0.55f;
        float r = s * 0.27f;
        float dd = r * 2;
        float fx = cx - s * 0.3f;
        float fy = cy - s * 0.3f;
        float bx = fx - off;
        float by = fy - off;

        using (Outline f = d.NewFigure(winding: true))
        {
            f.RoundRect(fx, fy, side, side, r);
            d.Stroke(f, stroke);
        }

        using Outline p = d.NewFigure(winding: true);
        p.Line(bx + side, fy, bx + side, by + r);
        p.Arc(bx + side - dd, by, dd, dd, 0, -90);
        p.Line(bx + side - r, by, bx + r, by);
        p.Arc(bx, by, dd, dd, 270, -90);
        p.Line(bx, by + r, bx, by + side - r);
        p.Arc(bx, by + side - dd, dd, dd, 180, -90);
        p.Line(bx + r, by + side, fx, by + side);
        d.Stroke(p, stroke);
    }

    public static void Trash(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float lid = cy - s * 0.545f;
        d.Line(cx - s * 0.84f, lid, cx + s * 0.84f, lid, stroke);

        float hx = s * 0.32f;
        float hTop = cy - s * 0.886f;
        float hBot = cy - s * 0.614f;
        float hr = s * 0.2f;
        using (Outline h = d.NewFigure(winding: true))
        {
            h.Line(cx - hx, hBot, cx - hx, hTop + hr);
            h.Arc(cx - hx, hTop, hr * 2, hr * 2, 180, 90);
            h.Line(cx - hx + hr, hTop, cx + hx - hr, hTop);
            h.Arc(cx + hx - hr * 2, hTop, hr * 2, hr * 2, 270, 90);
            h.Line(cx + hx, hTop + hr, cx + hx, hBot);
            d.Stroke(h, stroke);
        }

        float tw = s * 0.636f;
        float bw = s * 0.523f;
        float top = cy - s * 0.43f;
        float bot = cy + s * 0.886f;
        float br = s * 0.22f;
        float bd = br * 2;
        using Outline c = d.NewFigure(winding: true);
        c.Line(cx - tw, top, cx - bw, bot - br);
        c.Arc(cx - bw, bot - bd, bd, bd, 180, -90);
        c.Line(cx - bw + br, bot, cx + bw - br, bot);
        c.Arc(cx + bw - bd, bot - bd, bd, bd, 90, -90);
        c.Line(cx + bw, bot - br, cx + tw, top);
        d.Stroke(c, stroke);
    }

    public static void History(DrawContext d, float cx, float cy, float s)
    {
        StrokeStyle stroke = GlyphStyle.IconStroke(d);
        float w = s * 1.147f;
        float h = s * 1.6f;
        float r = s * 0.24f;
        float dd = r * 2;
        float off = s * 0.4f;
        float notch = s * 0.4f;
        float left = cx - s * 0.773f;
        float top = cy - s * 0.6f;
        float right = left + w;
        float bottom = top + h;
        float mid = left + w * 0.5f;

        using (Outline front = d.NewFigure(winding: true))
        {
            front.Arc(left, top, dd, dd, 180, 90);
            front.Arc(right - dd, top, dd, dd, 270, 90);
            front.Line(right, top + r, right, bottom);
            front.Line(right, bottom, mid, bottom - notch);
            front.Line(mid, bottom - notch, left, bottom);
            front.Close();
            d.Stroke(front, stroke);
        }

        float backTop = top - off;
        float backRight = right + off;
        float backBottom = bottom - off;
        using Outline back = d.NewFigure(winding: true);
        back.Line(left + off, top, left + off, backTop + r);
        back.Arc(left + off, backTop, dd, dd, 180, 90);
        back.Arc(backRight - dd, backTop, dd, dd, 270, 90);
        back.Line(backRight, backTop + r, backRight, backBottom);
        back.Line(backRight, backBottom, right, backBottom - notch * off / (w * 0.5f));
        d.Stroke(back, stroke);
    }

    public static void Bookmark(DrawContext d, float cx, float cy, float s)
    {
        float hw = s * 0.666f;
        float top = cy - s * 0.932f;
        float bottom = cy + s * 0.932f;
        float r = s * 0.266f;
        float dd = r * 2;
        float notch = bottom - s * 0.48f;

        using Outline p = d.NewFigure(winding: true);
        p.Arc(cx - hw, top, dd, dd, 180, 90);
        p.Arc(cx + hw - dd, top, dd, dd, 270, 90);
        p.Line(cx + hw, top + r, cx + hw, bottom);
        p.Line(cx + hw, bottom, cx, notch);
        p.Line(cx, notch, cx - hw, bottom);
        p.Close();
        d.Stroke(p, GlyphStyle.IconStroke(d));
    }

    public static void Play(DrawContext d, float cx, float cy, float s)
    {
        float x = cx + s * 0.2f;
        Tri[0] = new PointF(x - s, cy - s);
        Tri[1] = new PointF(x + s, cy);
        Tri[2] = new PointF(x - s, cy + s);

        using Outline p = d.NewFigure(winding: false);
        p.Lines(Tri, Tri.Length);
        p.Close();
        d.Fill(p, GlyphStyle.Foreground);
    }

    public static void Pause(DrawContext d, float cx, float cy, float s)
    {
        float w = s * 0.62f;
        float gap = s * 0.42f;
        float rad = MathF.Min(w * 0.4f, Theme.PauseRadius.Exact(d.Scale));
        d.FillRound(cx - gap - w, cy - s, w, s * 2, rad, GlyphStyle.Foreground);
        d.FillRound(cx + gap, cy - s, w, s * 2, rad, GlyphStyle.Foreground);
    }
}
