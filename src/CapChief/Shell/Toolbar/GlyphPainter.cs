namespace CapChief.Shell;

/// <summary>
/// Draws the icon a toolbar button or an About box row shows, sized to fit it.
/// </summary>
public static class GlyphPainter
{
    private const float Grouped = 0.92f;

    private const float Full = 1f;

    private const float BaseShare = 0.24f;

    private const float CrossShare = 0.66f, ArrowShare = 0.7f, PenShare = 0.693f;

    private const float LinkShare = 0.91f, LinkOutShare = 0.46f;

    private const float BoxShare = 0.81f, EllipseShare = 0.81f, TextShare = 0.723f;

    private const float MosaicShare = 0.788f, BarShare = 0.84f, SwatchShare = 0.858f;

    private const float PlayShare = 0.74f, PauseShare = 0.66f;

    public static void Draw(DrawContext d, Glyph glyph, Rect r, in GlyphState state)
    {
        float cx = r.Left + r.Width * 0.5f;
        float cy = r.Top + r.Height * 0.5f;

        switch (glyph)
        {
            case Glyph.Camera:
                ActionGlyphs.Camera(d, cx, cy, Sized(r, Full));
                break;
            case Glyph.Clock:
                ClockGlyph.Draw(d, cx, cy, Sized(r, Full), GlyphStyle.IconStroke(d));
                break;
            case Glyph.Record:
                ActionGlyphs.GifMark(d, r);
                break;
            case Glyph.Close:
                ActionGlyphs.Cross(d, cx, cy, Sized(r, CrossShare));
                break;
            case Glyph.Arrow:
                ToolGlyphs.Arrow(d, cx, cy, Sized(r, ArrowShare));
                break;
            case Glyph.Pen:
                ToolGlyphs.Pen(d, cx, cy, Sized(r, PenShare));
                break;
            case Glyph.Rectangle:
                ToolGlyphs.Box(d, cx, cy, Sized(r, BoxShare));
                break;
            case Glyph.Ellipse:
                ToolGlyphs.Ellipse(d, cx, cy, Sized(r, EllipseShare));
                break;
            case Glyph.Text:
                ToolGlyphs.Text(d, cx, cy, Sized(r, TextShare));
                break;
            case Glyph.Step:
                ToolGlyphs.Step(d, r, state.Step);
                break;
            case Glyph.Mosaic:
                ToolGlyphs.Mosaic(d, cx, cy, Sized(r, MosaicShare));
                break;
            case Glyph.Bar:
                ToolGlyphs.Bar(d, cx, cy, Sized(r, BarShare));
                break;
            case Glyph.Swatch:
                ToolGlyphs.Swatch(d, cx, cy, Sized(r, SwatchShare), state.Swatch);
                break;
            case Glyph.Save:
                ActionGlyphs.Save(d, cx, cy, Sized(r, Grouped));
                break;
            case Glyph.Copy:
                ActionGlyphs.Copy(d, cx, cy, Sized(r, Grouped));
                break;
            case Glyph.Trash:
                ActionGlyphs.Trash(d, cx, cy, Sized(r, Grouped));
                break;
            case Glyph.History:
                ActionGlyphs.History(d, cx, cy, Sized(r, Grouped));
                break;
            case Glyph.Bookmark:
                ActionGlyphs.Bookmark(d, cx, cy, Sized(r, Grouped));
                break;
            case Glyph.Play:
                ActionGlyphs.Play(d, cx, cy, Sized(r, PlayShare));
                break;
            case Glyph.Pause:
                ActionGlyphs.Pause(d, cx, cy, Sized(r, PauseShare));
                break;
            case Glyph.Speed:
                d.CenteredText(state.Label, r, GlyphStyle.Foreground, Theme.GifMark);
                break;
            case Glyph.Globe:
                LinkGlyphs.Globe(d, cx, cy, Sized(r, LinkShare));
                break;
            case Glyph.Code:
                LinkGlyphs.Code(d, cx, cy, Sized(r, LinkShare));
                break;
            case Glyph.Shield:
                LinkGlyphs.Shield(d, cx, cy, Sized(r, LinkShare));
                break;
            case Glyph.LinkOut:
                LinkGlyphs.LinkOut(d, cx, cy, Sized(r, LinkOutShare));
                break;
        }
    }

    public static Glyph GlyphOf(MarkKind kind)
    {
        switch (kind)
        {
            case MarkKind.Arrow:
                return Glyph.Arrow;
            case MarkKind.Pen:
                return Glyph.Pen;
            case MarkKind.Rectangle:
                return Glyph.Rectangle;
            case MarkKind.Ellipse:
                return Glyph.Ellipse;
            case MarkKind.Mosaic:
                return Glyph.Mosaic;
            case MarkKind.Blackout:
                return Glyph.Bar;
            case MarkKind.Text:
                return Glyph.Text;
            case MarkKind.Step:
                return Glyph.Step;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static float Sized(Rect r, float share)
    {
        return r.Height * BaseShare * share;
    }
}

/// <summary>
/// Specifies which icon a toolbar button or an About box row displays, such as save or pen.
/// </summary>
public enum Glyph
{
    None,
    Camera,
    Clock,
    Record,
    Close,
    Arrow,
    Rectangle,
    Text,
    Step,
    Mosaic,
    Bar,
    Swatch,
    Save,
    Copy,
    Pen,
    Ellipse,
    Trash,
    History,
    Bookmark,
    Play,
    Pause,
    Speed,
    Globe,
    Code,
    Shield,
    LinkOut,
}

/// <summary>
/// Represents the variable parts of an icon, such as its step number or color.
/// </summary>
public readonly record struct GlyphState(int Step, Color Swatch, string Label);
