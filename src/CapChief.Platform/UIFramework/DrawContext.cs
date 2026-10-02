namespace CapChief.UIFramework;

/// <summary>
/// Provides methods for drawing shapes, paths, and text onto a form's surface.
/// </summary>
public sealed class DrawContext
{
    private readonly BackBuffer _surface;
    private readonly TextRuler _ruler = new();
    private readonly Dictionary<(int Px, int Weight, FontFace Face), TextFont> _fonts = new();

    private Brush? _brush;
    private Pen? _pen;

    public DrawContext(BackBuffer surface)
    {
        _surface = surface;
    }

    public float Scale { get; set; } = 1f;

    public Pixmap Pixels
    {
        get
        {
            _surface.Flush();
            return new(_surface.Pixels, _surface.Width, _surface.Height, _surface.Width);
        }
    }

    public void Fill(Rect r, Color color, CornerRadius radius)
    {
        if (color.IsTransparent || r.IsEmpty)
        {
            return;
        }
        float rad = radius.In(r);
        if (rad <= 0f)
        {
            _surface.Sheet.Block(BrushFor(color), r.Left, r.Top, r.Width, r.Height);
        }
        else
        {
            _surface.Sheet.Round(BrushFor(color), r.Left, r.Top, r.Width, r.Height, rad);
        }
    }

    public void Fill(float x, float y, float w, float h, Color color)
    {
        if (!color.IsTransparent)
        {
            _surface.Sheet.Block(BrushFor(color), x, y, w, h);
        }
    }

    public void FillRound(float x, float y, float w, float h, float radius, Color color)
    {
        if (!color.IsTransparent)
        {
            _surface.Sheet.Round(BrushFor(color), x, y, w, h, radius);
        }
    }

    public void FillEllipse(float x, float y, float w, float h, Color color)
    {
        if (!color.IsTransparent)
        {
            _surface.Sheet.Disc(BrushFor(color), x, y, w, h);
        }
    }

    public void StrokeEllipse(float x, float y, float w, float h, in StrokeStyle stroke)
    {
        _surface.Sheet.Ring(PenFor(stroke), x, y, w, h);
    }

    public void Line(float x1, float y1, float x2, float y2, in StrokeStyle stroke)
    {
        _surface.Sheet.Line(PenFor(stroke), x1, y1, x2, y2);
    }

    public Outline NewFigure(bool winding)
    {
        return new(winding);
    }

    public void Fill(in Outline figure, Color color)
    {
        _surface.Sheet.Fill(BrushFor(color), figure);
    }

    public void Stroke(in Outline figure, in StrokeStyle stroke)
    {
        _surface.Sheet.Trace(PenFor(stroke), figure);
    }

    public void Text(string s, Rect r, Color color, Font font)
    {
        _surface.Text(s, r, color, Face(font));
    }

    public void DrawImage(Surface image, Rect at)
    {
        _surface.Image(image, at);
    }

    public void FadeToBackground(Rect r, float keep)
    {
        _surface.FadeToBase(r, keep);
    }

    public Size Measure(string s, Font font)
    {
        if (s.Length == 0)
        {
            return default;
        }
        TextFont face = Face(font);
        if (face.Exists)
        {
            return _ruler.Measure(s, face);
        }
        else
        {
            return default;
        }
    }

    public void CenteredText(string s, Rect r, Color color, Font font)
    {
        _surface.CenteredText(s, r, color, Face(font));
    }

    public bool CenteredTextInk(string s, Rect r, Font font, out Rect ink)
    {
        return CapChief.Graphics.CenteredText.InkRect(_ruler, r, s, Face(font), out ink);
    }

    public void Free()
    {
        _brush?.Free();
        _brush = null;
        _pen?.Free();
        _pen = null;
        foreach (TextFont font in _fonts.Values)
        {
            font.Free();
        }
        _fonts.Clear();
        _ruler.Free();
    }

    private Brush BrushFor(Color color)
    {
        _brush ??= new Brush(0);
        _brush.Color = color.Argb;
        return _brush;
    }

    private Pen PenFor(in StrokeStyle stroke)
    {
        _pen ??= new Pen(0, 1f);
        _pen.Color = stroke.Color.Argb;
        _pen.Width = stroke.Width;
        return _pen;
    }

    private TextFont Face(Font font)
    {
        int px = font.Px(Scale);
        if (px <= 0)
        {
            return default;
        }

        var key = (px, font.Weight, font.Face);
        if (_fonts.TryGetValue(key, out TextFont found))
        {
            return found;
        }

        TextFont made;
        switch (font.Face)
        {
            case FontFace.SharpSemibold:
                made = TextFont.SharpSemibold(px, font.Weight);
                break;
            case FontFace.Smooth:
                made = TextFont.Smooth(px, font.Weight);
                break;
            default:
                made = TextFont.SmoothSemibold(px, font.Weight);
                break;
        }
        _fonts[key] = made;
        return made;
    }
}
