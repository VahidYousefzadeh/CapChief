namespace CapChief.Shell;

/// <summary>
/// Places and draws text labels, such as the size of the selected area and a toast.
/// </summary>
public sealed class LabelPainter
{
    private const string Separator = " × ";

    private const int StopFloorPx = 8;

    private const float StopShare = 0.44f;

    private const int MinWidth = 256, MinHeight = 64;

    private readonly TextRuler _ruler = new();
    private readonly Desktop _desk;
    private readonly Metrics _metrics;
    private readonly CaretBlink _caret;

    private readonly Surface _background = new();
    private BackgroundLook? _drawn;
    private MeasuredFor? _measuredFor;
    private Size _measured;

    public LabelPainter(Desktop desk, Metrics metrics, CaretBlink caret)
    {
        _desk = desk;
        _metrics = metrics;
        _caret = caret;
    }

    private int Pad
    {
        get
        {
            return Theme.LabelPad.Px(_metrics.Scale);
        }
    }

    private int Gap
    {
        get
        {
            return Theme.LabelGap.Px(_metrics.Scale);
        }
    }

    private LabelStyle SizeStyle
    {
        get
        {
            return new(_metrics.Label, Shaded: false, Theme.LabelRadius.Exact(_metrics.Scale));
        }
    }

    public Rect SizeLabelRect(Rect crop)
    {
        Size sz = SizeLabelSize(crop);
        int pw = sz.Width;
        int ph = sz.Height;
        int gap = Gap;
        Rect screen = MonitorAt(new Point(crop.Right - 1, crop.Bottom - 1));
        int x = crop.Right - pw - gap;
        int y = crop.Bottom + gap;
        if (y + ph > screen.Bottom)
        {
            y = crop.Top - ph - gap;
        }
        if (y < screen.Top)
        {
            y = crop.Top + gap;
        }
        return Rect.Within(Rect.At(x, y, pw, ph), screen);
    }

    public Rect ToastRect(Rect crop, string t)
    {
        Size ext = _ruler.Measure(t, _metrics.Label);
        float scale = _metrics.Scale;
        int w = ext.Width + 2 * Theme.ToastPad.Px(scale);
        int h = ext.Height + 2 * Theme.Pad.Px(scale);
        Point c = crop.Center;
        return Rect.Within(Rect.At(c.X - w / 2, c.Y - h / 2, w, h), MonitorAt(c));
    }

    public Rect RecTagRect(Rect crop, string t)
    {
        return RecTag(crop, t).Tag;
    }

    public Rect CountdownTagRect(Rect crop, string t)
    {
        Size sz = LabelSize(t, _metrics.Label);
        return AboveLeftOf(crop, sz.Width, sz.Height);
    }

    public void DrawSizeLabel(DeviceContext dc, Rect crop, TypedSize? typed)
    {
        Rect r = SizeLabelRect(crop);
        if (!BlendBackground(dc, r, SizeStyle))
        {
            return;
        }
        WriteRuns(dc, r, _metrics.Label, SizeRuns(crop.Size, typed));
    }

    public void DrawToast(DeviceContext dc, Rect r, string t)
    {
        if (!BlendBackground(dc, r, ShadedStyle(_metrics.Label, Theme.PanelRadius)))
        {
            return;
        }
        WriteRuns(dc, r, _metrics.Label, [OnPanel(t)]);
    }

    public void DrawToast(AlphaSurface into, Rect r, string t)
    {
        DrawToast(into.DeviceContext, r, t);
        Size ext = _ruler.Measure(t, _metrics.Label);
        int x = r.Left + (r.Width - ext.Width) / 2;
        int y = r.Top + (r.Height - ext.Height) / 2;
        into.Opaque(Rect.At(x, y, ext.Width, ext.Height).Inflated(_metrics.LabelMargin));
    }

    public Rect DrawSizeLabelInLayer(Layer layer, Rect crop, TypedSize? typed)
    {
        Rect r = SizeLabelRect(crop);
        InLayer(layer, r, crop, SizeStyle, SizeRuns(crop.Size, typed));
        return r;
    }

    public Rect DrawRecInLayer(Layer layer, Rect crop, string t)
    {
        (Rect r, Rect stop) = RecTag(crop, t);
        InLayer(layer, r, default, ShadedStyle(_metrics.Label, Theme.RecRadius), [OnPanel(t)], textAt: r with { Left = stop.Right });
        layer.Fill(stop, Palette.StopRef, 255);
        return r;
    }

    public Rect DrawCountdownTagInLayer(Layer layer, Rect crop, string t)
    {
        Rect r = CountdownTagRect(crop, t);
        InLayer(layer, r, default, ShadedStyle(_metrics.Label, Theme.RecRadius), [OnPanel(t)]);
        return r;
    }

    public void DrawCountdownInLayer(Layer layer, Rect r, string t)
    {
        InLayer(layer, r, default, ShadedStyle(_metrics.Digit, Theme.PanelRadius), [OnPanel(t)]);
    }

    public void Free()
    {
        FreeBackground();
        _ruler.Free();
    }

    private LabelStyle ShadedStyle(TextFont font, Length radius)
    {
        return new(font, Shaded: true, radius.Exact(_metrics.Scale));
    }

    private static string SizeLabelText(Rect crop)
    {
        return $"{crop.Width}{Separator}{crop.Height}";
    }

    private Size SizeLabelSize(Rect crop)
    {
        var key = new MeasuredFor(crop.Size, _metrics.Scale);
        if (key == _measuredFor)
        {
            return _measured;
        }
        _measured = LabelSize(SizeLabelText(crop), _metrics.Label);
        _measuredFor = key;
        return _measured;
    }

    private Size LabelSize(string t, TextFont font)
    {
        Size ext = _ruler.Measure(t, font);
        int pad = Pad;
        return new(ext.Width + 2 * pad, ext.Height + pad);
    }

    private (Rect Tag, Rect Stop) RecTag(Rect crop, string t)
    {
        Size sz = LabelSize(t, _metrics.Label);
        int pad = Pad;
        int ph = sz.Height;
        int side = Math.Max(StopFloorPx, (int)(ph * StopShare));
        Rect tag = AboveLeftOf(crop, sz.Width + side + pad, ph);
        return (tag, Rect.At(tag.Left + pad, tag.Top + (ph - side) / 2, side, side));
    }

    private Rect AboveLeftOf(Rect crop, int w, int h)
    {
        int gap = Gap;
        Rect screen = MonitorAt(crop.TopLeft);
        int y = crop.Top - h - gap;
        if (y < screen.Top)
        {
            y = crop.Top + gap;
        }
        return Rect.Within(Rect.At(crop.Left, y, w, h), screen);
    }

    private Rect MonitorAt(Point p)
    {
        return _desk.FromScreen(_desk.MonitorAt(_desk.ToScreen(p)));
    }

    private void InLayer(Layer layer, Rect r, Rect hole, in LabelStyle style, ReadOnlySpan<TextRun> text, Rect textAt = default)
    {
        if (!layer.IsOpen || !Background(r.Size, style))
        {
            return;
        }
        layer.DrawOver(_background, r, hole, false);
        Rect textRect;
        if (textAt.IsEmpty)
        {
            textRect = r;
        }
        else
        {
            textRect = textAt;
        }
        WriteRuns(layer.DeviceContext, textRect, style.Font, text);
        // GDI zeroes the alpha where it draws text, and may still be drawing: let it finish, then
        // put the alpha back.
        DeviceContext.Flush();
        layer.DrawOver(_background, r, hole, true);
    }

    private bool BlendBackground(DeviceContext dc, Rect r, in LabelStyle style)
    {
        if (!Background(r.Size, style))
        {
            return false;
        }
        dc.AlphaBlend(r, _background.DeviceContext, new Rect { Right = r.Width, Bottom = r.Height });
        return true;
    }

    private bool Background(Size size, in LabelStyle style)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return false;
        }
        var look = new BackgroundLook(size, style.Shaded, style.RadiusPx, _metrics.Scale);
        if (_background.IsOpen && look == _drawn)
        {
            return true;
        }
        if (!_background.EnsureAtLeast(size.Width, size.Height, MinWidth, MinHeight))
        {
            FreeBackground();
            return false;
        }

        _drawn = look;
        var target = new Pixmap(_background.Pixels, size.Width, size.Height, _background.Width);
        if (style.Shaded)
        {
            PanelRaster.Gradient(target, _metrics.LabelMargin, style.RadiusPx, Theme.PanelRim.Exact(_metrics.Scale), Palette.Panel);
        }
        else
        {
            PanelRaster.Flat(target, Palette.DarkRef, style.RadiusPx);
        }
        return true;
    }

    private void FreeBackground()
    {
        _background.Free();
        _drawn = null;
    }

    /// <summary>
    /// Gives the runs of the size label: while typing, a typed number shows in the accent color,
    /// and an empty field being typed into blinks.
    /// </summary>
    private TextRun[] SizeRuns(Size shown, TypedSize? typed)
    {
        bool blinkOff = typed is { IsFocusedFieldEmpty: true } && _caret.IsHidden;
        TypedSize fields = typed.GetValueOrDefault();
        TextRun width = SizeField(shown.Width, fields.Width, blinkOff && !fields.HeightFocused);
        TextRun height = SizeField(shown.Height, fields.Height, blinkOff && fields.HeightFocused);
        return [width, new(Separator, Palette.LabelTextRef), height];
    }

    private static TextRun SizeField(int shown, int typed, bool hidden)
    {
        uint colorRef;
        if (typed > 0)
        {
            colorRef = Palette.AccentRef;
        }
        else
        {
            colorRef = Palette.LabelTextRef;
        }
        return new(shown.ToString(), colorRef, hidden);
    }

    private static TextRun OnPanel(string t)
    {
        return new(t, Palette.OnPanelRef);
    }

    private void WriteRuns(DeviceContext dc, Rect r, TextFont font, ReadOnlySpan<TextRun> runs)
    {
        Span<Size> sizes = stackalloc Size[runs.Length];
        int width = 0;
        for (int i = 0; i < runs.Length; i++)
        {
            sizes[i] = _ruler.Measure(runs[i].Text, font);
            width += sizes[i].Width;
        }

        int x = r.Left + (r.Width - width) / 2;
        int y = r.Top + (r.Height - sizes[0].Height) / 2;
        using FontScope selected = dc.SelectFont(font);
        for (int i = 0; i < runs.Length; i++)
        {
            var at = new Rect { Left = x, Top = y, Right = r.Right, Bottom = y + sizes[0].Height };
            if (!runs[i].Hidden)
            {
                dc.DrawTextRun(runs[i].Text, at, runs[i].ColorRef);
            }
            x += sizes[i].Width;
        }
    }

    /// <summary>
    /// Represents the font and background shape used to draw a label; shaded is the toolbar's
    /// gradient look, otherwise the background is flat and dark.
    /// </summary>
    private readonly record struct LabelStyle(TextFont Font, bool Shaded, float RadiusPx);

    /// <summary>
    /// Represents what the reused background bitmap was last drawn for.
    /// </summary>
    private readonly record struct BackgroundLook(Size Size, bool Shaded, float RadiusPx, float Scale);

    /// <summary>
    /// Represents the size and scale that the size label was last measured for.
    /// </summary>
    private readonly record struct MeasuredFor(Size Crop, float Scale);

    /// <summary>
    /// Represents a piece of label text in one color; a hidden piece keeps its room but draws
    /// nothing.
    /// </summary>
    private readonly record struct TextRun(string Text, uint ColorRef, bool Hidden = false);
}
